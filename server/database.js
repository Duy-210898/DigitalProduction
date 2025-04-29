const sql = require('mssql');

// Database Configuration
const dbConfig = {
  user: 'sa',
  password: '12345',
  server: '10.30.0.116',
  database: 'CuttingProjectData',
  options: {
    encrypt: false, // Disable encryption (use `true` for Azure)
    trustServerCertificate: true, // Trust self-signed certificates
  },
};

let pool; // Global connection pool

// Khởi tạo kết nối cơ sở dữ liệu
async function initDatabase() {
  try {
    if (!pool || !pool.connected) {
      pool = await sql.connect(dbConfig);
      console.log('✅ Kết nối cơ sở dữ liệu thành công');
    }
    return pool;
  } catch (err) {
    console.error('❌ Lỗi kết nối cơ sở dữ liệu:', err.message);
    pool = null; // Reset pool to allow reconnection
    throw err;
  }
}

// Hàm thực thi truy vấn SQL chung
async function executeQuery(query, inputs = []) {
  try {
    const request = (await initDatabase()).request();
    inputs.forEach(input => {
      request.input(input.name, input.type, input.value);
    });
    const result = await request.query(query);
    return result.recordset;
  } catch (err) {
    console.error('Lỗi khi truy vấn cơ sở dữ liệu:', err.message);
    throw err;
  }
}
async function getSizeDataFromDB(ipAddress, orderID, partName) {
  const query = `
    SELECT Distinct
      OS.Size,
      OS.SizeQty,
      DOut.PiecesPerPair,
      DOut.MaterialLayer,
      DOut.CuttingDieQTY,
      DOut.ActualCut
    FROM 
      [CuttingData].[dbo].[DistributionOrders] AS DO
    JOIN 
      [CuttingData].[dbo].[OrderMaterials] AS OM ON DO.OrderID = OM.OrderID
    JOIN 
      [CuttingData].[dbo].[OrderSizes] AS OS ON OM.MaterialID = OS.MaterialID
    JOIN 
      [CuttingData].[dbo].[DeviceOutput] AS DOut ON DO.OrderID = DOut.OrderID
    WHERE 
      DO.Status = 'Pending'
      AND DO.OrderID = @OrderID
      AND OM.PartName = @PartName;
  `;

  try {
    const request = new sql.Request();
    
    // Đảm bảo OrderID là số nguyên hợp lệ trước khi truyền vào câu lệnh SQL
    if (isNaN(orderID) || orderID <= 0) {
      throw new Error('Invalid OrderID. It must be a positive integer.');
    }

    request.input('IpAddress', sql.NVarChar, ipAddress);
    request.input('OrderID', sql.Int, orderID);  
    request.input('PartName', sql.NVarChar, partName);

    const result = await request.query(query);

    if (result.recordset.length > 0) {
      return result.recordset;
    } else {
      return null;
    }
  } catch (error) {
    console.error('Error fetching size data from database:', error.message);
    throw error;
  }
}

async function getActualOutputData(startDate, endDate) {
  const query = `
    SELECT 
      po.OrderID,
      po.MasterWorkOrder,
      po.SO,
      pr.Model,
      pr.ART,
      do.IsLeather,
      o.OperatorName,
      p.PartName,
      s.Size,
      dl.MachineName,
      pso.SizeID,
      pso.SizeQty,
      do.PiecesPerPair,
      do.MaterialLayer,
      do.CuttingDieQty,
      do.ActualCut,
      do.ActualSizeQty,
      do.ActualPieces,
      do.TotalPiecesPerPair,
      do.InventoryQty,
      do.CreatedAt AS Timestamp,
      do.UpdatedAt AS UpdatedAt
    FROM ProductOrder po
    JOIN Product pr ON po.ProductId = pr.ProductId
    JOIN PartSizeOrder pso ON po.OrderID = pso.OrderId
    JOIN Part p ON pso.PartId = p.PartId
    JOIN Size s ON pso.SizeId = s.SizeID
    JOIN Material m ON pso.MaterialID = m.MaterialID
    JOIN DeviceOutput do ON do.SizeID = pso.SizeID AND do.OrderID = pso.OrderID AND do.PartID = p.PartID
    JOIN DistributionData dd ON dd.PartSizeOrderId = pso.PartSizeOrderId
    JOIN DeviceList dl ON dl.DeviceID = dd.DeviceID
    JOIN Operator o ON dd.OperatorID = o.OperatorID
    WHERE do.UpdatedAt >= @startDate AND do.UpdatedAt < @endDate
    GROUP BY
      po.OrderID, do.IsLeather, do.TotalPiecesPerPair, 
      po.MasterWorkOrder, po.SO, pr.Model, pr.ART, 
      o.OperatorName, s.Size, pso.SizeID, pso.SizeQty, dl.MachineName,
      do.PiecesPerPair, do.MaterialLayer, do.CuttingDieQty, 
      do.ActualCut, do.ActualSizeQty, do.ActualPieces, do.InventoryQty, do.CreatedAt, do.UpdatedAt, p.PartName
      ORDER BY do.UpdatedAt ASC;
  `;
  
  let transaction;
  try {
    const pool = await initDatabase();
    transaction = new sql.Transaction(pool);
    await transaction.begin();
    
    const request = transaction.request();
    request.input('startDate', sql.DateTime, startDate);
    request.input('endDate', sql.DateTime, endDate);

    const result = await request.query(query);
    
    await transaction.commit();
    
    if (result.recordset.length > 0) {
      const filteredOutputData = result.recordset
        .filter(record => record.SizeQty > 0)
        .map(record => ({
          PartName: record.PartName,
          MachineName: record.MachineName,
          SO: record.SO,
          IsLeather: record.IsLeather,
          OperatorName: record.OperatorName,
          Size: record.Size,
          SizeQty: record.SizeQty,
          PiecesPerPair: record.PiecesPerPair,
          MaterialLayer: record.MaterialLayer,
          CuttingDieQty: record.CuttingDieQty,
          ActualCut: record.ActualCut,
          ActualSizeQty: record.ActualSizeQty,
          ActualPieces: record.ActualPieces,
          TotalPiecesPerPair: record.TotalPiecesPerPair,
          Timestamp: record.Timestamp,
          UpdatedAt: record.UpdatedAt
        }));
      
      return {
        OutputData: filteredOutputData 
      };
    } else {
      return null;
    }
  } catch (error) {
    console.error('Error fetching actual output data from database:', error.message);
    if (transaction) {
      try {
        await transaction.rollback();
        console.log('Transaction rolled back.');
      } catch (rollbackError) {
        console.error('Error during transaction rollback:', rollbackError.message);
      }
    }
    throw error;
  }
}

async function setOrderIsComplete(OrderID) {
  // Kiểm tra OrderID hợp lệ
  if (!OrderID || OrderID <= 0) {
    throw new Error('OrderID không hợp lệ. Nó phải là số nguyên dương.');
  }

  try {
    const updateQuery = `
      UPDATE DistributionOrders
      SET Status = 'Complete'
      WHERE OrderID = @OrderID;
    `;

    const request = (await initDatabase()).request();
    request.input('OrderID', sql.Int, OrderID);

    // Thực hiện truy vấn cập nhật
    const result = await request.query(updateQuery);

    // Kiểm tra số lượng bản ghi bị ảnh hưởng (nếu không tìm thấy OrderID nào thì sẽ không có bản ghi nào bị thay đổi)
    if (result.rowsAffected[0] === 0) {
      console.log(`Không tìm thấy OrderID: ${OrderID} trong bảng DistributionOrders.`);
      return false;
    }

    console.log(`Cập nhật trạng thái 'Complete' thành công cho OrderID: ${OrderID}`);
    return true; // Trả về true nếu cập nhật thành công
  } catch (error) {
    console.error('Lỗi khi cập nhật trạng thái Order thành Complete:', error.message);
    throw error;
  }
}


async function setDistributionIsComplete(DistributionID, Status) {
  // Kiểm tra DistributionID hợp lệ
  if (!DistributionID || DistributionID <= 0) {
    throw new Error('DistributionID không hợp lệ. Nó phải là số nguyên dương.');
  }

  try {
    const updateQuery = `
      UPDATE DistributionData
      SET Status = @Status
      WHERE DistributionID = @DistributionID;
    `;

    const request = (await initDatabase()).request();
    request.input('DistributionID', sql.Int, DistributionID);
    request.input('Status', sql.VarChar, Status);

    // Thực hiện truy vấn cập nhật
    const result = await request.query(updateQuery);

    // Kiểm tra số lượng bản ghi bị ảnh hưởng
    if (result.rowsAffected[0] === 0) {
      console.log(`Không tìm thấy DistributionID: ${DistributionID} trong bảng DistributionOrders.`);
      return false;
    }

    console.log(`Cập nhật trạng thái 'Complete' thành công cho DistributionID: ${DistributionID}`);
    return true; // Trả về true nếu cập nhật thành công
  } catch (error) {
    console.error('Lỗi khi cập nhật trạng thái Distribution thành Complete:', error.message);
    throw error;
  }
}



async function saveActualDataToDB(data) {
  const { OrderID, SizeData, IsLeather } = data;

  // Kiểm tra OrderID trước khi tiếp tục
  if (!OrderID || OrderID === 0) {
    throw new Error('OrderID không hợp lệ');
  }

  try {
    for (const size of SizeData) {
      if (!size.SizeID || typeof size.SizeID !== 'number') {
        throw new Error(`Kích thước không hợp lệ: ${size.SizeID}`);
      }

      const inputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'SizeID', type: sql.Int, value: size.SizeID },
        { name: 'PartID', type: sql.Int, value: size.PartID },
        { name: 'IsLeather', type: sql.Int, value: IsLeather }
      ];

      // Check if entry exists
      const checkDeviceOutputQuery = `
        SELECT COUNT(*) AS count FROM DeviceOutput 
        WHERE OrderID = @OrderID AND SizeID = @SizeID AND PartID = @PartID AND IsLeather = @IsLeather
      `;

      // Insert Query
      const DeviceOutputQuery = `
        INSERT INTO DeviceOutput (
            OrderID, SizeID, PartID, PiecesPerPair, MaterialLayer, CuttingDieQty, 
            ActualCut, ActualPieces, ActualSizeQty, TotalPiecesPerPair, IsLeather, CreatedAt, UpdatedAt
        ) VALUES (
            @OrderID, @SizeID, @PartID, @PiecesPerPair, @MaterialLayer, @CuttingDieQty, 
            @ActualCut, @ActualPieces, @ActualSizeQty, @TotalPiecesPerPair, @IsLeather, GETDATE(), GETDATE() 
        );
      `;

      // Inputs for Insert
      const DeviceOutputInputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'SizeID', type: sql.Int, value: size.SizeID },
        { name: 'PartID', type: sql.Int, value: size.PartID },
        { name: 'IsLeather', type: sql.Int, value: IsLeather },
        { name: 'PiecesPerPair', type: sql.Int, value: size.PiecesPerPair ?? 0 },
        { name: 'MaterialLayer', type: sql.Int, value: size.MaterialLayer ?? 0 },
        { name: 'CuttingDieQty', type: sql.Int, value: size.CuttingDieQty ?? 0 },
        { name: 'ActualCut', type: sql.Int, value: size.ActualCut ?? 0 },
        { name: 'ActualPieces', type: sql.Int, value: size.ActualPieces ?? 0 },
        { name: 'ActualSizeQty', type: sql.Int, value: size.ActualSizeQty ?? 0 },
        { name: 'TotalPiecesPerPair', type: sql.Int, value: size.TotalPiecesPerPair ?? 0 }
      ];

      // Update Query
      const updateDeviceOutputQuery = `
        UPDATE DeviceOutput
        SET
          PiecesPerPair = @PiecesPerPair,
          MaterialLayer = @MaterialLayer,
          CuttingDieQty = @CuttingDieQty,
          ActualCut = @ActualCut,
          ActualPieces = @ActualPieces,
          ActualSizeQty = @ActualSizeQty,
          TotalPiecesPerPair = @TotalPiecesPerPair,
          IsLeather = @IsLeather,
          UpdatedAt = GETDATE()
        WHERE
          OrderID = @OrderID
          AND SizeID = @SizeID
          AND PartID = @PartID
          AND IsLeather = @IsLeather
      `;

      // Inputs for Update
      const updateDeviceOutputInputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'SizeID', type: sql.Int, value: size.SizeID },
        { name: 'PartID', type: sql.Int, value: size.PartID },
        { name: 'IsLeather', type: sql.Int, value: IsLeather },
        { name: 'PiecesPerPair', type: sql.Int, value: size.PiecesPerPair },
        { name: 'MaterialLayer', type: sql.Int, value: size.MaterialLayer },
        { name: 'CuttingDieQty', type: sql.Int, value: size.CuttingDieQty },
        { name: 'ActualCut', type: sql.Int, value: size.ActualCut },
        { name: 'ActualPieces', type: sql.Int, value: size.ActualPieces },
        { name: 'ActualSizeQty', type: sql.Int, value: size.ActualSizeQty },
        { name: 'TotalPiecesPerPair', type: sql.Int, value: size.TotalPiecesPerPair }
      ];

      // Log input parameters for debugging
      DeviceOutputInputs.forEach(param => {
        console.log(`${param.name}: ${param.value} (Type: ${param.type?.name || param.type})`);
      });

      // Execute check query
      const result = await executeQuery(checkDeviceOutputQuery, inputs);

      if (result[0].count === 0) {
        // If not exists, INSERT
        await executeQuery(DeviceOutputQuery, DeviceOutputInputs);
        console.log(`✅ Inserted DeviceOutput for OrderID: ${OrderID}, SizeID: ${size.SizeID}, PartID: ${size.PartID}`);
      } else {
        // If exists, UPDATE
        await executeQuery(updateDeviceOutputQuery, updateDeviceOutputInputs);
        console.log(`✅ Updated DeviceOutput for OrderID: ${OrderID}, SizeID: ${size.SizeID}, PartID: ${size.PartID}`);
        
        const findPartSizeOrderIdQuery = `
          SELECT PartSizeOrderId 
          FROM PartSizeOrder 
          WHERE PartId = @PartID AND SizeId = @SizeID AND OrderId = @OrderID
        `;

        const partSizeOrderInputs = [
          { name: 'PartID', type: sql.Int, value: size.PartID },
          { name: 'SizeID', type: sql.Int, value: size.SizeID },
          { name: 'OrderID', type: sql.Int, value: OrderID }
        ];

        const partSizeResult = await executeQuery(findPartSizeOrderIdQuery, partSizeOrderInputs);

        if (partSizeResult.length > 0) {
          const partSizeOrderId = partSizeResult[0].PartSizeOrderId;

          const updateDistributionQuery = `
            UPDATE DistributionData
            SET UpdatedAt = GETDATE()
            WHERE PartSizeOrderId = @PartSizeOrderId
          `;

          const updateDistributionInputs = [
            { name: 'PartSizeOrderId', type: sql.Int, value: partSizeOrderId }
          ];

          await executeQuery(updateDistributionQuery, updateDistributionInputs);
          console.log(`🟢 Updated DistributionData UpdatedAt for PartSizeOrderID=${partSizeOrderId}`);
        } else {
          console.warn(`⚠️ No PartSizeOrder found for OrderID=${OrderID}, SizeID=${size.SizeID}, PartID=${size.PartID}`);
        }
      }
    }
  } catch (error) {
    console.error('Lỗi khi cập nhật Actual data:', error.message);
    throw error;
  }
}
// Function to save distribution data and default info
async function saveDistributionDataToDB(dataList) {
  let pool;
  let results = []; // Store status of each data entry

  try {
    pool = await sql.connect(dbConfig);

    for (const data of dataList) {
      let status = {
        PartID: data.PartID,
        Model: data.Model,
        DeviceID: data.DeviceID,
        ProductID: data.ProductID,
        DistributionInserted: false,
        DistributionDuplicate: false,
        Error: null,
      };
    
      try {
        const distributionConditions = {
          PartSizeOrderID: { type: sql.Int, value: data.PartSizeOrderID }
        };
    
        // 🚫 Check for duplicate DistributionData
        const distributionExists = await recordExists("DistributionData", distributionConditions, pool);
    
        if (distributionExists) {
          console.log(`⚠️ Skipped duplicate DistributionData for DeviceID ${data.DeviceID}`);
          status.DistributionDuplicate = true;
          results.push(status);
          continue; // ⛔️ Skip entire item, including DefaultInfo
        }
    
        // ✅ Insert DistributionData
        const distributionQuery = `
          INSERT INTO DistributionData (
            DeviceID, PartSizeOrderID, OperatorID, InventoryQty, Status, CreatedAt , UpdatedAt, IsLeather, IsDelete, UserID
          ) VALUES (
            @DeviceID, @PartSizeOrderID, @OperatorID, @InventoryQty, @Status, @CreatedAt, @UpdatedAt, @IsLeather, @IsDelete, @UserID
          );
        `;
    
        let request = pool.request();
        request.input('DeviceID', sql.Int, data.DeviceID);
        request.input('PartSizeOrderID', sql.Int, data.PartSizeOrderID);
        request.input('OperatorID', sql.Int, data.OperatorID);
        request.input('InventoryQty', sql.Int, data.InventoryQty);
        request.input('Status', sql.NVarChar, data.Status || 'Pending');
        request.input('CreatedAt', sql.DateTime, data.CreatedAt || new Date());
        request.input('UpdatedAt', sql.DateTime, data.UpdatedAt || new Date());
        request.input('IsLeather', sql.Bit, data.IsLeather);
        request.input('IsDelete', sql.Bit, data.IsDelete || 0);
        request.input('UserID', sql.Int, data.UserID);
    
        const result = await request.query(distributionQuery);
        if (result.rowsAffected && result.rowsAffected[0] > 0) {
          console.log(`✅ Inserted new DistributionData: DeviceID ${data.DeviceID}`);
          status.DistributionInserted = true;
        }
    
        // 🧩 Only do DefaultInfo if DistributionData was inserted
        const defaultInfoConditions = {
          PartID: { type: sql.Int, value: data.PartID },
          Model: { type: sql.VarChar, value: data.Model },
          ProductID: { type: sql.Int, value: data.ProductID }
        };
        const defaultInfoExists = await recordExists("DefaultInfo", defaultInfoConditions, pool);
    
        request = pool.request();
        request.input('ProductID', sql.Int, data.ProductID);
        request.input('PartID', sql.Int, data.PartID);
        request.input('Model', sql.VarChar, data.Model);
        request.input('PiecesPerPair', sql.Int, data.PiecesPerPair || 0);
        request.input('CuttingDieQty', sql.Int, data.CuttingDieQty || 0);
        request.input('MaterialLayer', sql.Int, data.MaterialLayer || 0);
        request.input('TotalPiecesPerPair', sql.Int, data.TotalPiecesPerPair || 0);
    
        if (!defaultInfoExists) {
          const defaultInfoQuery = `
            INSERT INTO DefaultInfo (
              ProductID, PartID, Model, PiecesPerPair, CuttingDieQty, MaterialLayer, TotalPiecesPerPair
            ) VALUES (
              @ProductID, @PartID, @Model, @PiecesPerPair, @CuttingDieQty, @MaterialLayer, @TotalPiecesPerPair
            );
          `;
          await request.query(defaultInfoQuery);
          console.log(`✅ Inserted new DefaultInfo: ProductID ${data.ProductID}`);
        } else {
          const updateInfoQuery = `
            UPDATE DefaultInfo 
            SET PiecesPerPair = @PiecesPerPair, 
                CuttingDieQty = @CuttingDieQty, 
                MaterialLayer = @MaterialLayer, 
                TotalPiecesPerPair = @TotalPiecesPerPair
            WHERE ProductID = @ProductID AND PartID = @PartID AND Model = @Model;
          `;
          await request.query(updateInfoQuery);
          console.log(`🔄 Updated DefaultInfo: ProductID ${data.ProductID}`);
        }
    
      } catch (err) {
        console.error(`❌ Error processing DeviceID ${data.DeviceID}: ${err.message}`);
        status.Error = err.message;
      }
    
      results.push(status);
    }    

    console.log('✅ All unique data processed successfully.');
    return results; // Return status for all entries

  } catch (error) {
    console.error('❌ Critical error saving data:', error.message);
    return { success: false, error: error.message };
  }
}

async function getDistributionDataFromDb(ipAddress) {
  try {
    const query = `
        SELECT  
          pr.OrderID,
          pr.MasterWorkOrder,
          pr.SO,
          d.IpAddress,
          dd.IsLeather,
          p.Model,
          o.OperatorName AS UserName,
          p.ART,
          pa.PartID,
          pa.PartName,
          m.MaterialCode,
          m.MaterialID,
          m.MaterialName,
          se.SizeID,
          se.Size,
          ps.SizeQty,
          dd.InventoryQty,
          di.PiecesPerPair,
          di.CuttingDieQty,
          di.MaterialLayer,
          di.TotalPiecesPerPair,
          do.ActualCut,
	        do.ActualPieces,
	        do.ActualSizeQty,
          dd.Status,
          dd.CreatedAt
      FROM 
          DistributionData AS dd
      JOIN 
          DeviceList AS d ON dd.DeviceID = d.DeviceID 
      JOIN 
          PartSizeOrder AS ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
      JOIN 
          Part AS pa ON pa.PartID = ps.PartID
      JOIN 
          Size AS se ON se.SizeID = ps.SizeID
      JOIN 
          Material AS m ON m.MaterialID = ps.MaterialID
      JOIN 
          Operator AS o ON dd.OperatorID = o.OperatorID  
      JOIN 
          ProductOrder AS pr ON ps.OrderId = pr.OrderID
      JOIN 
          Product AS p ON pr.ProductId = p.ProductId
      LEFT JOIN 
          DeviceOutput AS do ON do.SizeID = se.SizeID
                      AND do.OrderID = pr.OrderID AND do.PartID = pa.PartID
      LEFT JOIN 
          DefaultInfo AS di ON di.ProductID = p.ProductId  AND di.PartID = pa.PartId AND di.Model = p.Model
      WHERE 
          dd.IsDelete = 0  
          AND d.IpAddress = @IpAddress
          AND dd.Status IN ('Complete', 'Pending') 
          AND EXISTS (
              SELECT *
              FROM DistributionData AS sub_dd
              WHERE sub_dd.Status = 'Pending' AND FORMAT(dd.CreatedAt, 'dd/MM/yyyy HH:mm:ss') = FORMAT(sub_dd.CreatedAt, 'dd/MM/yyyy HH:mm:ss'))
      ORDER BY 
          dd.CreatedAt ASC;
    `;

    const request = new sql.Request();
    request.input('IpAddress', sql.VarChar, ipAddress);
    
    const result = await request.query(query);
    
    if (result.recordset.length > 0) {
      
      const row = result.recordset[0];

      // Group records by timestamp
      const groupedByTimestamp = result.recordset.reduce((acc, item) => {
        const createdAtTimestamp = new Date(item.CreatedAt).toISOString().replace("T", " ").slice(0, 19); // Format as YYYY-MM-DD HH:MM:SS

      if (!acc[createdAtTimestamp]) {
          acc[createdAtTimestamp] = [];
      }
      acc[createdAtTimestamp].push(item);
      return acc;
      }, {});

      // Convert object to an array of grouped records
      const soGroups = Object.keys(groupedByTimestamp).map(timestamp => ({
          Timestamp: timestamp,
          Data: groupedByTimestamp[timestamp]
      }));
      console.log(soGroups);

      const orderID = parseInt(row.OrderID, 10);

     // Keep `SizeData` unchanged (original sum per SizeID)
     const sizeDataMap = new Map();

     result.recordset.forEach(item => {
       const key = `${item.SizeID}-${item.Size}-${item.PartName}`;
     
       if (!sizeDataMap.has(key)) {
         sizeDataMap.set(key, {
           SizeID: item.SizeID,
           Size: item.Size,
           PartName: item.PartName,
           SizeQty: 0,
           InventoryQty: item.InventoryQty
         });
       }
     
       sizeDataMap.get(key).SizeQty += item.SizeQty;
     });
     
     const sizeDataSosGroups = Array.from(sizeDataMap.values());     
        
      // Process MaterialData
      const materialData = result.recordset
        .map(r => ({
          PartID: r.PartID,
          PartName: r.PartName,
          MaterialID: r.MaterialID,
          MaterialCode: r.MaterialCode,
          MaterialsName: r.MaterialName
        }))
        .filter((value, index, self) =>
          self.findIndex(t => 
            t.PartID === value.PartID &&
            t.PartName === value.PartName &&
            t.MaterialID === value.MaterialID
          ) === index
        );

        // Lấy thông tin SizeData
      const sizeData = result.recordset
      .map(item => ({
        OrderID: item.OrderID,
        SizeID: item.SizeID,
        Size: item.Size,
        SizeQty: item.SizeQty,
        InventoryQty: item.InventoryQty
      }))
      .filter((value, index, self) =>
        index === self.findIndex(
          t => t.SizeID === value.SizeID && t.Size === value.Size && t.SizeQty === value.SizeQty && t.InventoryQty === value.InventoryQty
        )
      );
      // Process DefaultValue
      const defaultValue = result.recordset
        .map(item => ({
          PiecesPerPair: item.PiecesPerPair,
          CuttingDieQty: item.CuttingDieQty,
          MaterialLayer: item.MaterialLayer,
          TotalPiecesPerPair: item.TotalPiecesPerPair
        }))
        .filter((value, index, self) =>
          index === self.findIndex(t => 
            t.PiecesPerPair === value.PiecesPerPair &&
            t.CuttingDieQty === value.CuttingDieQty &&
            t.MaterialLayer === value.MaterialLayer &&
            t.TotalPiecesPerPair === value.TotalPiecesPerPair
          )
        );

      // Return the formatted data
      return {
        OrderID: orderID,
        OrderIDWithSOs: soGroups,
        SO: row.SO,
        MasterWorkOrder: row.MasterWorkOrder,
        Leather: row.IsLeather ? 2 : 1,
        Model: row.Model,
        ART: row.ART,
        SizeDataDB: sizeData,
        SizeData: sizeDataSosGroups,
        MaterialData: materialData,
        DefaultValue: defaultValue
      };
    } else {
      return null; 
    }
  } catch (error) {
    console.error(`Error fetching distribution data from DB: ${error.message}`);
    logToFile(errorLogPath, `Error fetching distribution data from DB: ${error.message}`);
    throw error;
  }
}

async function getSizeAndDistributionDataFromDb(ipAddress, orderId, isLeather) {
  try {
    const query =
          `SELECT TOP 6
              dd.DistributionID, 
              se.SizeID
          FROM 
              DistributionData AS dd
          JOIN 
              DeviceList AS d ON dd.DeviceID = d.DeviceID 
          JOIN 
              PartSizeOrder AS ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
          JOIN 
              Part AS pa ON pa.PartID = ps.PartID
          JOIN 
              Size AS se ON se.SizeID = ps.SizeID
          JOIN 
              ProductOrder AS pr ON ps.OrderId = pr.OrderID
          WHERE 
              dd.Status = 'Pending'
              AND dd.IsDelete = 0  
              AND d.IpAddress = @ipAddress
              AND ps.OrderId = @OrderId
              AND dd.IsLeather = @IsLeather
          GROUP BY 
              dd.DistributionID, se.SizeID;
    `;
    
    const request = new sql.Request();
    request.input('IpAddress', sql.VarChar, ipAddress);
    request.input('OrderId', sql.Int, orderId);
    request.input('IsLeather', sql.Int, isLeather);
    
    const result = await request.query(query);
    
    if (result.recordset.length > 0) {
      
      // Lấy thông tin SizeData
      const sizeData = result.recordset
      .map(item => ({
        SizeID: item.SizeID,
      }))
      .filter((value, index, self) =>
        index === self.findIndex(
          t => t.SizeID === value.SizeID
        )
      );
       // Lấy thông tin distributionIDData 
       const distributionID = result.recordset
       .map(item => ({
         DistributionID: item.DistributionID
       }))
       .filter((value, index, self) =>
         index === self.findIndex(
           t => t.DistributionID === value.DistributionID
         )
       );
  
      // Trả về dữ liệu theo cấu trúc yêu cầu
      return {
        SizeData: sizeData,
        DistributionID: distributionID
      };
    } else {
      return null; 
    }
  } catch (error) {
    console.error(`Error fetching distribution data from DB: ${error.message}`);
    logToFile(errorLogPath, `Error fetching distribution data from DB: ${error.message}`);
    throw error;
  }
}

async function getDistributionCompleteFromDb(ipAddress, orderId, isLeather, note) {
  try {
      const query = `
           SELECT  
                dd.DistributionID
            FROM 
                DistributionData AS dd
            JOIN 
                DeviceList AS d ON dd.DeviceID = d.DeviceID 
            JOIN 
                PartSizeOrder AS ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
            WHERE 
			        dd.Status = 'Stop'
              AND dd.IsDelete = 0  
              AND d.IpAddress = @IpAddress
              AND ps.OrderId = @OrderId
              AND dd.IsLeather = @IsLeather
              AND dd.Note IS NOT NULL
              AND TRY_CAST(dd.Note AS FLOAT) <> 0
            GROUP BY 
                dd.DistributionID;
      `;

      const request = new sql.Request();
      request.input('IpAddress', sql.VarChar, ipAddress);
      request.input('OrderId', sql.Int, orderId);
      request.input('IsLeather', sql.Int, isLeather);
      request.input('Note', sql.Int, note);

      const result = await request.query(query);

      if (result.recordset.length > 0) {
          // ✅ Return only DistributionID and IsComplete
          return result.recordset.map(item => ({
              DistributionID: item.DistributionID
          }));
      } else {
          return null;
      }
  } catch (error) {
      console.error(`❌ Error fetching distribution data from DB: ${error.message}`);
      logToFile(errorLogPath, `❌ Error fetching distribution data from DB: ${error.message}`);
      throw error;
  }
}


async function getDistributionIDFromSizeID(ipAddress, orderId, isLeather, sizeID, partID) {
  try {
    const query = `
      	  SELECT  
              dd.DistributionID, 
              ps.PartSizeOrderId,
              se.SizeID
          FROM 
              DistributionData AS dd
          JOIN 
              DeviceList AS d ON dd.DeviceID = d.DeviceID
          JOIN 
              PartSizeOrder AS ps ON dd.PartSizeOrderId = ps.PartSizeOrderId
          JOIN 
              Part AS pa ON pa.PartID = ps.PartID
          JOIN 
              Size AS se ON se.SizeID = ps.SizeID
          JOIN 
              ProductOrder AS pr ON pr.OrderId = ps.OrderID
          WHERE 
              dd.IsDelete = 0  
              AND d.IpAddress = @IpAddress
              AND dd.Status = 'Pending'
              AND ps.OrderId = @OrderId
              AND dd.IsLeather = @IsLeather
              AND se.SizeID = @SizeID
              AND pa.PartID = @PartID
          GROUP BY 
              dd.DistributionID, ps.PartSizeOrderId, se.SizeID;
    `;
    
    const request = new sql.Request();
    request.input('IpAddress', sql.VarChar, ipAddress);
    request.input('OrderId', sql.Int, orderId);
    request.input('IsLeather', sql.Int, isLeather);
    request.input('SizeID', sql.Int, sizeID);
    request.input('PartID', sql.Int, partID);
    
    const result = await request.query(query);
    
    if (result.recordset.length > 0) {
      const distributionIDData = result.recordset
        .map(item => ({
          DistributionID: item.DistributionID,
          SizeID: item.PartSizeID
        }))
        .filter((value, index, self) =>
          index === self.findIndex(
            t => t.DistributionID === value.DistributionID &&
                 t.SizeID === value.SizeID
          )
        );

      return {
        DistributionID: distributionIDData
      };
    } else {
      return null; 
    }
  } catch (error) {
    console.error(`Error fetching distribution data from DB: ${error.message}`);
    logToFile(errorLogPath, `Error fetching distribution data from DB: ${error.message}`);
    throw error;
  }
}
async function getAllDeviceData() {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request()
      .query('SELECT a.DeviceID, a.IpAddress, a.MachineName, a.ConnectionStatus, a.IsActive, b.PlantName, d.DepartmentName FROM DeviceList a JOIN Plant b ON a.PlantID = b.PlantID JOIN Department d ON d.DepartmentID= a.DepartmentID');

    return result.recordset;  
  } catch (error) {
    console.error('Error fetching device data from database:', error.message);
    throw error;
  }
}


// Fetch data from SQL Server
async function getDistributions(startDate, endDate) {
  try {
      // Kết nối tới cơ sở dữ liệu với cấu hình dbConfig
      const pool = await sql.connect(dbConfig);
      // SQL Query
      const query = `
          SELECT 
              dd.DistributionID,
              dd.DeviceID,
              pr.SO,
              d.IpAddress,
              d.MachineName,
              o.OperatorName,
              u.EmployeeName,
              pa.PartName,
              se.Size,
              ps.Unit,
              ps.UnitUsage,
              ps.SizeQty,
              m.MaterialName,
              dd.InventoryQty,
              dd.Status,
              dd.CreatedAt,
              dd.UpdatedAt,
              dd.IsLeather,
              dd.IsDelete,
              dd.Note
          FROM 
              DistributionData dd
          JOIN 
              DeviceList d ON dd.DeviceID = d.DeviceID 
          JOIN 
              PartSizeOrder ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
          JOIN 
              Part pa ON pa.PartID = ps.PartID
          JOIN 
              Size se ON se.SizeID = ps.SizeID
          JOIN 
              Material m ON m.MaterialID = ps.MaterialID
          JOIN 
              Operator o ON dd.OperatorID = o.OperatorID  
          JOIN 
              Users u ON dd.UserID = u.UserID
          JOIN 
              ProductOrder pr ON pr.OrderId = ps.OrderID
          WHERE 
              dd.IsDelete = 0 AND 
              dd.UpdatedAt >= @startDate AND dd.UpdatedAt < @endDate
          ORDER BY 
              dd.UpdatedAt ASC;
      `;
      
      // Execute the query with parameters
      const request = pool.request();
      request.input('startDate', sql.DateTime, startDate);
      request.input('endDate', sql.DateTime, endDate);
      
      // Get the result
      const result = await request.query(query);
      
      // Return the result rows
      return result.recordset; 
  } catch (err) {
      throw new Error('Database query failed: ' + err.message);
  }
}

async function getDistributionByDevice(ipAddress) {
  try {
    // Kết nối tới cơ sở dữ liệu với cấu hình dbConfig
    const pool = await sql.connect(dbConfig);

    // Thực hiện truy vấn và đảm bảo OrderID là số nguyên
    const result = await pool.request()
      .input('IpAddress', sql.NVarChar, ipAddress) 
      .input('Status', sql.NVarChar, 'Pending')
      .query('SELECT OrderID, MasterWorkOrder, SO, Model, ART, Status, CreatedAt FROM [DistributionOrders] WHERE IpAddress = @IpAddress AND Status = @Status');
      
    // Kiểm tra và trả về kết quả nếu có dữ liệu, nếu không trả về null
    return result.recordset.length > 0 ? result.recordset.map(record => ({
      OrderID: parseInt(record.OrderID, 10),
      MasterWorkOrder: record.MasterWorkOrder,
      SO: record.SO,
      Model: record.Model,
      ART: record.ART,
      Status: record.Status,
      CreatedAt: record.CreatedAt
    })) : null;
    
  } catch (error) {
    console.error('Error fetching device data from database:', error.message);
    throw error;
  }
}// Lấy danh sách thiết bị
async function getDeviceList() {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request().query('SELECT IpAddress FROM DeviceList WHERE IsActive = 1');
    return result.recordset.map(record => record.IpAddress);
  } catch (error) {
    console.error('Error fetching device list from database:', error.message);
    throw error;
  }
}
// Lấy danh sách trang từ Master Work Order
async function getUniquePages(masterWorkOrder) {
  try {
    if (!pool) await initDatabase(); 
    const result = await pool.request()
      .input('MasterWorkOrder', sql.NVarChar, masterWorkOrder)
      .query('SELECT DISTINCT Page FROM [CuttingData] WHERE MasterWorkOrder = @MasterWorkOrder');

    return result.recordset.map(row => parseInt(row.Page, 10));
  } catch (err) {
    console.error('Lỗi khi truy cập cơ sở dữ liệu:', err.message);
    return [];
  }
}

async function addDeviceToList({ ipAddress, machineName, plantName }) {
  console.log(`Adding device with ipAddress: ${ipAddress}, machineName: ${machineName}, plantName: ${plantName}`);

  try {
    // Kiểm tra các tham số
    if (!ipAddress || !machineName || !plantName) {
      throw new Error('Missing required fields: ipAddress, machineName, or plantName');
    }

    // Câu truy vấn lấy PlantID dựa trên tên Plant
    const plantQuery = `
      SELECT PlantID
      FROM Plant
      WHERE LTRIM(RTRIM(Plant)) = @PlantName
    `;

    const plantInputs = [{ name: 'PlantName', type: sql.NVarChar, value: plantName.trim() }];
    console.log(`Executing plant query with plantName: '${plantName.trim()}'`);

    const plantResult = await executeQuery(plantQuery, plantInputs);
    console.log(`Query result: ${JSON.stringify(plantResult)}`);

    // Kiểm tra nếu không có PlantID
    if (!plantResult || plantResult.length === 0) {
      console.log(`No Plant found with name: ${plantName}`);
      return { status: 'error', message: `No Plant found with name: ${plantName}` };  
    }

    const plantID = plantResult[0].PlantID;
    console.log(`Found PlantID: ${plantID}`);

    const query = `
      INSERT INTO DeviceList (IpAddress, MachineName, PlantID, CreatedAt, IsActive, ConnectionStatus)
      VALUES (@IpAddress, @MachineName, @PlantID, DEFAULT, @IsActive, DEFAULT)
    `;

    const inputs = [
      { name: 'IpAddress', type: sql.NVarChar, value: ipAddress },
      { name: 'MachineName', type: sql.NVarChar, value: machineName },
      { name: 'PlantID', type: sql.Int, value: plantID },  
      { name: 'IsActive', type: sql.Bit, value: true },
    ];

    // Thực thi câu truy vấn insert
    await executeQuery(query, inputs);
    console.log(`Device ${machineName} added successfully with PlantID ${plantID}`);

    return { status: 'success', message: `Device ${machineName} added successfully.` };
  } catch (error) {
    console.error(`Error adding device: ${error.message}`);
    return { status: 'error', message: `Error adding device: ${error.message}` }; 
  }
}

// Cập nhật trạng thái kết nối của thiết bị
async function updateDeviceConnectionStatus(ipAddress, status) {
  const query = `
    UPDATE DeviceList
    SET ConnectionStatus = @status
    WHERE IpAddress = @ipAddress
  `;
  const inputs = [
    { name: 'ipAddress', type: sql.VarChar, value: ipAddress },
    { name: 'status', type: sql.Bit, value: status ? true : false },
  ];
  return await executeQuery(query, inputs);
}
// Lấy phân phối đơn từ SO
async function getProductionSchedule(so) {
  try {
    if (!pool) await initDatabase(); 
    const result = await pool.request()
      .input('SO', sql.NVarChar, so)
      .query(`
        SELECT 
            po.OrderID,
            po.Factory,
            po.SO,
            po.PO,
            po.MasterWorkOrder,
            po.LastNo,
            po.Process,
            s.Size,
            s.SizeID,
            p.ART,
            p.Model,
            pso.SizeQty,
            pso.Unit AS PartSizeUnit,
            pso.UnitUsage,
            m.MaterialID,
            m.MaterialCode,
            m.MaterialName,
            m.Unit AS MaterialUnit,
            pa.PartId,
            pa.PartName,
            pa.VietnameseName,
            pa.PartCode,
            po.CreatedAt,
            po.UpdatedAt
        FROM Product p
        JOIN ProductOrder po ON p.ProductId = po.ProductId
        JOIN PartSizeOrder pso ON po.OrderID = pso.OrderID
        JOIN Part pa ON pso.PartId = pa.PartId
        JOIN Material m ON pso.MaterialID = m.MaterialID
        JOIN Size s ON pso.SizeId = s.SizeID
        WHERE po.SO = @SO
      `);

    return result.recordset;
  } catch (err) {
    console.error('Lỗi khi truy cập cơ sở dữ liệu:', err.message);
    return [];
  }
}
async function getAllProductionSchedule() {
  try {
    if (!pool) await initDatabase(); 
    const result = await pool.request()
      .query(`
        SELECT 
            po.OrderID,
            po.Factory,
            po.SO,
            po.PO,
            po.MasterWorkOrder,
            po.LastNo,
            po.Process,
            s.Size,
            s.SizeID,
            p.ART,
            p.Model,
            pso.SizeQty,
            pso.Unit AS PartSizeUnit,
            pso.UnitUsage,
            m.MaterialID,
            m.MaterialCode,
            m.MaterialName,
            m.Unit AS MaterialUnit,
            pa.PartId,
            pa.PartName,
            pa.VietnameseName,
            pa.PartCode,
            po.CreatedAt,
            po.UpdatedAt
        FROM Product p
        JOIN ProductOrder po ON p.ProductId = po.ProductId
        JOIN PartSizeOrder pso ON po.OrderID = pso.OrderID
        JOIN Part pa ON pso.PartId = pa.PartId
        JOIN Material m ON pso.MaterialID = m.MaterialID
        JOIN Size s ON pso.SizeId = s.SizeID
      `);

    return result.recordset;
  } catch (err) {
    console.error('Lỗi khi truy cập cơ sở dữ liệu:', err.message);
    return [];
  }
}


// Đóng kết nối cơ sở dữ liệu
async function closeDatabase() {
  try {
    await pool.close();
    console.log('Kết nối cơ sở dữ liệu đã đóng');
  } catch (err) {
    console.error('Lỗi khi đóng kết nối cơ sở dữ liệu:', err.message);
  }
}
// Lấy danh sách tên nhà máy
async function getPlantNames() {
  try {
    if (!pool) await initDatabase(); // Kiểm tra và khởi tạo kết nối
    const result = await pool.request().query('SELECT Plant FROM Plant');
    return result.recordset; // Trả về danh sách các PlantName
  } catch (err) {
    console.error('Lỗi khi truy cập cơ sở dữ liệu:', err.message);
    return [];
  }
}
// Lấy danh sách users
async function getUserList() {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request().query('SELECT U.UserID, U.Username,  U.EmployeeName, U.EmployeeID, D.DepartmentName, D.DepartmentID, P.PositionName, P.PositionID, U.IsActive, U.CreatedAt, U.UpdatedAt FROM Users U LEFT JOIN Department D ON U.DepartmentID = D.DepartmentID LEFT JOIN Position P ON U.PositionID = P.PositionID;');
    return result.recordset;
  } catch (error) {
    console.error('Error fetching user list from database:', error.message);
    throw error;
  }
}

// Lấy danh sách users departmentID
async function getOperatorList(departmentID) {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request()
    .input('DepartmentID', sql.Int, departmentID)
    .query('SELECT * FROM [CuttingProjectData].[dbo].[Operator] WHERE  Operator.DepartmentID = @DepartmentID');
    return result.recordset;
  } catch (error) {
    console.error('Error fetching user list from database:', error.message);
    throw error;
  }
}
// Lấy danh sách users EmployeeID
async function getOperatorDistribution(employeeID) {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request()
      .input('EmployeeID', sql.Int, employeeID)
      .query('SELECT * FROM [CuttingProjectData].[dbo].[Operator] WHERE Operator.EmployeeID = @EmployeeID AND IsActive = 1');
    return result.recordset;
  } catch (error) {
    console.error('Error fetching user list from database:', error.message);
    throw error;
  }
}


// Function to check if a record already exists
async function recordExists(tableName, conditions, pool) {
  let query = `SELECT COUNT(*) AS count FROM ${tableName} WHERE `;
  let conditionClauses = [];
  let request = new sql.Request(pool);

  // Dynamically generate conditions for the WHERE clause
  Object.keys(conditions).forEach((key, index) => {
    conditionClauses.push(`${key} = @${key}`);
    request.input(key, conditions[key].type, conditions[key].value);
  });

  query += conditionClauses.join(" AND ");

  const result = await request.query(query);
  return result.recordset[0].count > 0; // Return true if record exists
}


module.exports = {
  initDatabase,
  closeDatabase,
  getDeviceList,
  updateDeviceConnectionStatus,
  getAllDeviceData,
  getDistributionByDevice,
  addDeviceToList,
  saveActualDataToDB,
  saveDistributionDataToDB,
  getUniquePages,
  getProductionSchedule,
  getAllProductionSchedule,
  getPlantNames,
  getSizeDataFromDB,
  getDistributionDataFromDb,
  getActualOutputData,
  setOrderIsComplete,
  setDistributionIsComplete,
  getUserList,
  getOperatorList,
  getDistributions,
  getOperatorDistribution,
  getSizeAndDistributionDataFromDb,
  getDistributionIDFromSizeID,
  getDistributionCompleteFromDb
};