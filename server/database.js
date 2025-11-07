const sql = require('mssql');
const fs = require('fs');
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
  pool: {
    max: 10,
    min: 0,
    idleTimeoutMillis: 30000,
  },
  requestTimeout: 30000,
};

let pool; // Global connection pool

const errorLogPath = './error_log.txt';

function logToFile(filePath, message) {
  const timestamp = new Date().toISOString();
  const logMessage = `[${timestamp}] ${message}\n`;
  fs.appendFile(filePath, logMessage, (err) => {
    if (err) {
      console.error(`Failed to write log to ${filePath}: ${err.message}`);
    }
  });
}
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
  }
}

// Hàm thực thi truy vấn SQL chung
async function executeQuery(query, inputs = [], retries = 3, delay = 1000) {
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const request = (await initDatabase()).request();
      inputs.forEach(input => {
        request.input(input.name, input.type, input.value);
      });
      const result = await request.query(query);
      return result.recordset;
    } catch (err) {
      console.error(`❌ Query failed (attempt ${attempt}): ${err.message}`);
      if (attempt === retries) {
        logToFile(errorLogPath, `❌ Query failed after ${retries} attempts: ${attempt}`);
      }
      await new Promise(res => setTimeout(res, delay));
      delay *= 2; // exponential backoff
    }
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
      console.warn('Invalid OrderID. It must be a positive integer.');
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
  }
}

async function getActualOutputData(startDate, endDate) {
  // const offset = (page - 1) * pageSize;

  const dataQuery = `
        SELECT  
          po.OrderID,
          po.MasterWorkOrder,
          po.SO,
          pr.Model,
          pr.ART,
          latestDO.IsLeather,
          o.OperatorName,
          COALESCE(p.VietnameseName, p.PartName) AS PartName,
          s.Size,
          dl.MachineName,
          pso.SizeID,

          -- Use Sub.SizeQty if exists, otherwise fallback to pso.SizeQty
          ISNULL(SUM(sub.SizeQty), pso.SizeQty) AS SizeQty,

          latestDO.PiecesPerPair,
          latestDO.MaterialLayer,
          latestDO.CuttingDieQty,
          latestDO.ActualCut,
          latestDO.ActualSizeQty,
          latestDO.ActualPieces,
          latestDO.TotalPiecesPerPair,
          latestDO.InventoryQty,
          latestDO.CreatedAt AS Timestamp,
          latestDO.UpdatedAt AS UpdatedAt
      FROM ProductOrder po
      JOIN Product pr ON po.ProductId = pr.ProductId
      JOIN PartSizeOrder pso ON po.OrderID = pso.OrderId
      JOIN Part p ON pso.PartId = p.PartId
      JOIN Size s ON pso.SizeId = s.SizeID
      JOIN Material m ON pso.MaterialID = m.MaterialID
      JOIN DistributionData dd ON dd.PartSizeOrderId = pso.PartSizeOrderId
      LEFT JOIN SubDistribution sub ON sub.DistributionID = dd.DistributionID AND sub.PartSizeOrderId = dd.PartSizeOrderId AND sub.IsDelete = 0
      LEFT JOIN Operator o ON o.OperatorID = ISNULL(sub.OperatorID, dd.OperatorID)
      LEFT JOIN DeviceList dl ON dl.DeviceID = ISNULL(sub.DeviceID, dd.DeviceID)

      -- ✅ Get only latest DeviceOutput row per combination
      OUTER APPLY (
          SELECT TOP 1 *
          FROM DeviceOutput do
          WHERE do.SizeID = pso.SizeID 
            AND do.OrderID = po.OrderID 
            AND do.PartID = p.PartID
            AND do.OperatorID = o.EmployeeID
          ORDER BY do.UpdatedAt DESC
      ) latestDO

      WHERE 
          dd.IsDelete = 0 AND  latestDO.UpdatedAt >= @startDate AND latestDO.UpdatedAt < @endDate
      GROUP BY
          po.OrderID,
          po.MasterWorkOrder,
          po.SO,
          pr.Model,
          pr.ART,
          latestDO.IsLeather,
          o.OperatorName,
          COALESCE(p.VietnameseName, p.PartName),
          s.Size,
          dl.MachineName,
          pso.SizeID,
          pso.SizeQty,
          latestDO.PiecesPerPair,
          latestDO.MaterialLayer,
          latestDO.CuttingDieQty,
          latestDO.ActualCut,
          latestDO.ActualSizeQty,
          latestDO.ActualPieces,
          latestDO.TotalPiecesPerPair,
          latestDO.InventoryQty,
          latestDO.CreatedAt,
          latestDO.UpdatedAt
      ORDER BY 
          latestDO.UpdatedAt ASC;
  `;

  const countQuery = `
    SELECT COUNT(*) AS TotalCount
    FROM DeviceOutput do
    WHERE do.UpdatedAt >= @startDate AND do.UpdatedAt < @endDate;
  `;

  try {
    const pool = await initDatabase();

    // Run data query next
    const dataRequest = pool.request();
    dataRequest.input('startDate', sql.DateTime, startDate);
    dataRequest.input('endDate', sql.DateTime, endDate);
    const dataResult = await dataRequest.query(dataQuery);

    const filteredOutputData = dataResult.recordset
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
        InventoryQty: record.InventoryQty,
        Timestamp: record.Timestamp,
        UpdatedAt: record.UpdatedAt
      }));

    return {
      Data: filteredOutputData
    };
  } catch (error) {
    console.error('Error fetching actual output data from database:', error.message);
  }
}


async function setOrderIsComplete(OrderID) {
  // Kiểm tra OrderID hợp lệ
  if (!OrderID || OrderID <= 0) {
    console.warn('OrderID không hợp lệ. Nó phải là số nguyên dương.');
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

  //  console.log(`Cập nhật trạng thái 'Complete' thành công cho OrderID: ${OrderID}`);
    return true; // Trả về true nếu cập nhật thành công
  } catch (error) {
    console.error('Lỗi khi cập nhật trạng thái Order thành Complete:', error.message);
  }
}

async function setSubDistributionComplete(SubDistributionID, Status) {
  if (!Number.isInteger(SubDistributionID) || SubDistributionID <= 0) {
    console.warn('SubDistributionID không hợp lệ. Nó phải là số nguyên dương.');
  }

  try {
    const pool = await initDatabase();
    const request = pool.request();

    request.input('SubDistributionID', sql.Int, SubDistributionID);
    request.input('Status', sql.VarChar(50), Status);

    const result = await request.query(`
      UPDATE SubDistribution
      SET Status = @Status
      WHERE SubDistributionID = @SubDistributionID;
    `);

    const affected = result.rowsAffected?.[0] || 0;

    if (affected === 0) {
      console.warn(`Không có bản ghi nào được cập nhật cho SubDistributionID = ${SubDistributionID}`);
      return false;
    }

    console.log(`✅ Cập nhật thành công SubDistributionID = ${SubDistributionID} với Status = '${Status}'`);
    return true;

  } catch (err) {
    console.error('❌ Lỗi khi cập nhật trạng thái SubDistribution:', err.message);
  }
}
async function getSubDistributions(distributionID) {
  try {
    const query = `
      SELECT 
        sd.SubDistributionID,
        sd.DistributionID,
        sd.PartSizeOrderId,
        sd.SizeQty,
        sd.Status,
        dl.IpAddress
      FROM SubDistribution sd
      LEFT JOIN DeviceList dl ON sd.DeviceID = dl.DeviceID
      WHERE sd.DistributionID = @DistributionID
        AND sd.IsDelete = 0;
    `;

    const request = new sql.Request();
    request.input('DistributionID', sql.Int, distributionID);

    const result = await request.query(query);

    return result.recordset;
  } catch (error) {
    console.error(`❌ Error fetching SubDistributions for DistributionID=${distributionID}:`, error.message);
    logToFile(errorLogPath, `Error fetching SubDistributions for DistributionID=${distributionID}: ${error.message}`);
    return [];
  }
}
async function setDistributionIsComplete(DistributionID, Status) {
  // Kiểm tra DistributionID hợp lệ
  if (!DistributionID || DistributionID <= 0) {
    console.warn('DistributionID không hợp lệ. Nó phải là số nguyên dương.');
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
    //  console.log(`Không tìm thấy DistributionID: ${DistributionID} trong bảng DistributionOrders.`);
      return false;
    }

    //console.log(`Cập nhật trạng thái 'Complete' thành công cho DistributionID: ${DistributionID}`);
    return true; // Trả về true nếu cập nhật thành công
  } catch (error) {
    console.error('Lỗi khi cập nhật trạng thái Distribution thành Complete:', error.message);
  }
}
/**
 * Logs cut history to the database.
 * 
 * @param {Object} params - Parameters for logging cut history.
 * @param {number} params.OrderID - Order ID.
 * @param {number} params.PartID - Part ID.
 * @param {number} params.CutQuantity - Cut quantity.
 * @param {number} params.SizeID - Size ID.
 * @param {Date} params.CutDate - Cut date.
 * @param {number} params.EmployeeID - Employee ID.
 */
async function logCutHistoryToDB({ OrderID, PartID, CutQuantity, SizeID, CutDate, EmployeeID }) {
  const pool = await initDatabase();
  const transaction = new sql.Transaction(pool);

  try {
    let actualCut = CutQuantity;

    // Start transaction
    await transaction.begin();

    // Check if Operator (EmployeeID) exists
    const request = new sql.Request(transaction);

    // Check if operator exists
    const employeeCheck = await request
      .input("EmployeeID", sql.Int, EmployeeID)
      .query(`SELECT EmployeeID FROM Operator WHERE EmployeeID = @EmployeeID`);
  
    if (employeeCheck.recordset.length === 0) {
      console.warn(`❌ EmployeeID ${EmployeeID} not found`);
      await transaction.rollback();
      return;
    }

    // Check for past cuts before CutDate
    const pastCutResult = await transaction.request()
      .input("OrderID", sql.Int, OrderID)
      .input("PartID", sql.Int, PartID)
      .input("SizeID", sql.Int, SizeID)
      .input("CutDate", sql.Date, CutDate)
      .input("EmployeeID", sql.Int, EmployeeID)
      .query(`
        SELECT SUM(CutQuantity) AS TotalCutQuantity
        FROM CutHistory
        WHERE 
          OrderID = @OrderID AND 
          PartID = @PartID AND 
          SizeID = @SizeID AND 
          CAST(CutDate AS DATE) < @CutDate AND 
          EmployeeID = @EmployeeID
      `);

    const cutHistoryBefore = pastCutResult.recordset[0].TotalCutQuantity ?? 0;
    actualCut = CutQuantity - cutHistoryBefore;
    //console.log('✅ Log đã lưu vào CutHistory ${actualCut}');
    if (actualCut < 0) {
      console.warn('❌ Invalid CutQuantity: result is negative');
      await transaction.rollback();
      return;
    }

    // Check for same-day entry
    const existingEntry = await transaction.request()
      .input("OrderID", sql.Int, OrderID)
      .input("PartID", sql.Int, PartID)
      .input("SizeID", sql.Int, SizeID)
      .input("CutDate", sql.Date, CutDate)
      .input("EmployeeID", sql.Int, EmployeeID)
      .query(`
        SELECT CutHistoryID, CutQuantity
        FROM CutHistory
        WHERE 
          OrderID = @OrderID AND 
          PartID = @PartID AND 
          SizeID = @SizeID AND 
          CAST(CutDate AS DATE) = @CutDate AND 
          EmployeeID = @EmployeeID
      `);

    if (existingEntry.recordset.length > 0) {
      const { CutHistoryID } = existingEntry.recordset[0];

      // Update CutQuantity
      await transaction.request()
        .input("CutHistoryID", sql.Int, CutHistoryID)
        .input("CutQuantity", sql.Int, actualCut)
        .query(`
          UPDATE CutHistory
          SET CutQuantity = @CutQuantity
          WHERE CutHistoryID = @CutHistoryID
        `);

    } else {
      // Insert new CutHistory record
      await transaction.request()
        .input("OrderID", sql.Int, OrderID)
        .input("PartID", sql.Int, PartID)
        .input("SizeID", sql.Int, SizeID)
        .input("CutQuantity", sql.Int, actualCut)
        .input("CutDate", sql.Date, CutDate)
        .input("EmployeeID", sql.Int, EmployeeID)
        .query(`
          INSERT INTO CutHistory (OrderID, PartID, SizeID, CutQuantity, CutDate, EmployeeID)
          VALUES (@OrderID, @PartID, @SizeID, @CutQuantity, @CutDate, @EmployeeID)
        `);
    }

    // Commit transaction
    await transaction.commit();
   // console.log(`✅ CutHistory saved successfully for EmployeeID ${EmployeeID}`);
  } catch (error) {
    if (transaction.inTransaction) {
      await transaction.rollback();
    }
    console.error('❌ Error logging CutHistory with transaction:', error.message);
  }
}


async function saveActualDataToDB(data) {
  const { OrderID, OperatorID, SizeData, IsLeather } = data;

  // Kiểm tra OrderID trước khi tiếp tục
  if (!OrderID || OrderID === 0) {
    console.warn('OrderID không hợp lệ');
  }

  try {
    for (const size of SizeData) {
      if (!size.SizeID || typeof size.SizeID !== 'number') {
        console.warn(`Kích thước không hợp lệ: ${size.SizeID}`);
      }

      const inputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'OperatorID', type: sql.Int, value: OperatorID },
        { name: 'SizeID', type: sql.Int, value: size.SizeID },
        { name: 'PartID', type: sql.Int, value: size.PartID },
        { name: 'IsLeather', type: sql.Int, value: IsLeather }
      ];

      // Check if entry exists
      const checkDeviceOutputQuery = `
        SELECT COUNT(*) AS count FROM DeviceOutput 
        WHERE OrderID = @OrderID AND SizeID = @SizeID AND PartID = @PartID AND IsLeather = @IsLeather AND OperatorID = @OperatorID
      `;

      // Insert Query
      const DeviceOutputQuery = `
        INSERT INTO DeviceOutput (
            OrderID, OperatorID, SizeID, PartID, PiecesPerPair, MaterialLayer, CuttingDieQty, 
            ActualCut, ActualPieces, ActualSizeQty, TotalPiecesPerPair, IsLeather, CreatedAt, UpdatedAt
        ) VALUES (
            @OrderID, @OperatorID, @SizeID, @PartID, @PiecesPerPair, @MaterialLayer, @CuttingDieQty, 
            @ActualCut, @ActualPieces, @ActualSizeQty, @TotalPiecesPerPair, @IsLeather, GETDATE(), GETDATE() 
        );
      `;

      // Inputs for Insert
      const DeviceOutputInputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'OperatorID', type: sql.Int, value: OperatorID },
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
          AND OperatorID = @OperatorID
          AND SizeID = @SizeID
          AND PartID = @PartID
          AND IsLeather = @IsLeather
      `;

      // Inputs for Update
      const updateDeviceOutputInputs = [
        { name: 'OrderID', type: sql.Int, value: OrderID },
        { name: 'OperatorID', type: sql.Int, value: OperatorID },
        { name: 'SizeID', type: sql.Int, value: size.SizeID },
        { name: 'PartID', type: sql.Int, value: size.PartID },
        { name: 'IsLeather', type: sql.Int, value: IsLeather },
        { name: 'PiecesPerPair', type: sql.Int, value: size.PiecesPerPair },
        { name: 'MaterialLayer', type: sql.Int, value: size.MaterialLayer },
        { name: 'CuttingDieQty', type: sql.Int, value: size.CuttingDieQty },
        { name: 'ActualCut', type: sql.Int, value: size.ActualCut  || 0},
        { name: 'ActualPieces', type: sql.Int, value: size.ActualPieces  || 0},
        { name: 'ActualSizeQty', type: sql.Int, value: size.ActualSizeQty || 0 },
        { name: 'TotalPiecesPerPair', type: sql.Int, value: size.TotalPiecesPerPair }
      ];

      // Execute check query
      const result = await executeQuery(checkDeviceOutputQuery, inputs);

      if (result[0].count === 0) {
        // If not exists, INSERT
        await executeQuery(DeviceOutputQuery, DeviceOutputInputs);
       //console.log(`✅ Inserted DeviceOutput for OrderID: ${OrderID}, SizeID: ${size.SizeID}, PartID: ${size.PartID}`);
      } else {
        // If exists, UPDATE
        await executeQuery(updateDeviceOutputQuery, updateDeviceOutputInputs);
     //   console.log(`✅ Updated DeviceOutput for OrderID: ${OrderID}, SizeID: ${size.SizeID}, PartID: ${size.PartID}`);
        
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
         // console.log(`🟢 Updated DistributionData UpdatedAt for PartSizeOrderID=${partSizeOrderId}`);
        } else {
          console.warn(`⚠️ No PartSizeOrder found for OrderID=${OrderID}, SizeID=${size.SizeID}, PartID=${size.PartID}`);
        }
      }
    }
  } catch (error) {
    console.error('Lỗi khi cập nhật Actual data:', error.message);
  }
}

async function saveDistributionDataToDB(distributionList) {
  let pool;
  const results = [];

  try {
    pool = await sql.connect(dbConfig);

    for (const data of distributionList.Distributions) {
      const status = {
        PartID: data.PartID,
        Model: data.Model,
        DeviceID: data.DeviceID,
        ProductID: data.ProductID,
        DistributionInserted: false,
        DistributionDuplicate: false,
        Error: null,
      };

      try {
        const exists = await recordExists("DistributionData", {
          PartSizeOrderID: { type: sql.Int, value: data.PartSizeOrderID }
        }, pool);

        if (exists) {
          status.DistributionDuplicate = true;
          results.push(status);
          continue;
        }

        // Insert into DistributionData
        const request = pool.request();
        const deviceID = parseInt(data.DeviceID, 10);
        request.input('DeviceID', sql.Int, isNaN(deviceID) || deviceID === 0 ? null : deviceID);
        const operatorID = parseInt(data.OperatorID, 10);
        request.input('OperatorID', sql.Int, isNaN(operatorID) || operatorID === 0 ? null : operatorID);
        request.input('InventoryQty', sql.Int, data.InventoryQty);
        request.input('PartSizeOrderID', sql.Int, data.PartSizeOrderID);
        request.input('Status', sql.VarChar, data.Status || 'Pending');
        request.input('CreatedAt', sql.DateTime, data.CreatedAt || new Date());
        request.input('UpdatedAt', sql.DateTime, data.UpdatedAt || new Date());
        request.input('IsLeather', sql.Bit, data.IsLeather);
        request.input('IsDelete', sql.Bit, data.IsDelete || 0);
        request.input('UserID', sql.Int, data.UserID);

        const insertResult = await request.query(`
          INSERT INTO DistributionData (
            DeviceID, PartSizeOrderID, OperatorID, InventoryQty, Status,
            CreatedAt, UpdatedAt, IsLeather, IsDelete, UserID
          ) OUTPUT INSERTED.DistributionID
          VALUES (
            @DeviceID, @PartSizeOrderID, @OperatorID, @InventoryQty, @Status,
            @CreatedAt, @UpdatedAt, @IsLeather, @IsDelete, @UserID
          );
        `);

        const insertedDistributionId = insertResult.recordset[0].DistributionID;
        status.DistributionInserted = true;

        // Insert or update DefaultInfo
        const defaultInfoExists = await recordExists("DefaultInfo", {
          PartID: { type: sql.Int, value: data.PartID },
          Model: { type: sql.VarChar, value: data.Model },
          ProductID: { type: sql.Int, value: data.ProductID }
        }, pool);

        const infoRequest = pool.request();
        infoRequest.input('ProductID', sql.Int, data.ProductID);
        infoRequest.input('PartID', sql.Int, data.PartID);
        infoRequest.input('Model', sql.VarChar, data.Model);
        infoRequest.input('PiecesPerPair', sql.Int, data.PiecesPerPair || 0);
        infoRequest.input('CuttingDieQty', sql.Int, data.CuttingDieQty || 0);
        infoRequest.input('MaterialLayer', sql.Int, data.MaterialLayer || 0);
        infoRequest.input('TotalPiecesPerPair', sql.Int, data.TotalPiecesPerPair || 0);

        if (!defaultInfoExists) {
          await infoRequest.query(`
            INSERT INTO DefaultInfo (
              ProductID, PartID, Model, PiecesPerPair, CuttingDieQty, MaterialLayer, TotalPiecesPerPair
            ) VALUES (
              @ProductID, @PartID, @Model, @PiecesPerPair, @CuttingDieQty, @MaterialLayer, @TotalPiecesPerPair
            );
          `);
        } else {
          await infoRequest.query(`
            UPDATE DefaultInfo SET
              PiecesPerPair = @PiecesPerPair,
              CuttingDieQty = @CuttingDieQty,
              MaterialLayer = @MaterialLayer,
              TotalPiecesPerPair = @TotalPiecesPerPair
            WHERE ProductID = @ProductID AND PartID = @PartID AND Model = @Model;
          `);
        }

        // Insert nested SubDistributions (if any)
        if (Array.isArray(distributionList.SubDistributions)) {
          const relatedSubs = distributionList.SubDistributions
        
          for (const sub of relatedSubs) {
            const subRequest = pool.request();
            subRequest.input('DistributionID', sql.Int, insertedDistributionId);
            subRequest.input('UserID', sql.Int, data.UserID);
            subRequest.input('SizeQty', sql.Int, sub.SizeQty);
            const deviceID = parseInt(sub.DeviceID, 10);
            subRequest.input('DeviceID', sql.Int, isNaN(deviceID) || deviceID === 0 ? null : deviceID);
            const operatorID = parseInt(sub.OperatorID, 10);
            subRequest.input('OperatorID', sql.Int, isNaN(operatorID) || operatorID === 0 ? null : operatorID);
            subRequest.input('PartSizeOrderId', sql.Int, data.PartSizeOrderID);
            subRequest.input('InventoryQty', sql.Int, sub.InventoryQty);
            subRequest.input('Status', sql.VarChar, data.Status || 'Pending');
            subRequest.input('CreatedAt', sql.DateTime, data.CreatedAt || new Date());
            subRequest.input('UpdatedAt', sql.DateTime, data.UpdatedAt || new Date());
            subRequest.input('IsLeather', sql.Bit, data.IsLeather);
            subRequest.input('IsDelete', sql.Bit, data.IsDelete || 0);
            subRequest.input('Note', sql.VarChar, sub.Note || null);

            await subRequest.query(`
              INSERT INTO SubDistribution (
                DistributionID, UserID, SizeQty, DeviceID, PartSizeOrderId, OperatorID, InventoryQty,
                Status, CreatedAt, UpdatedAt, IsLeather, IsDelete, Note
              ) VALUES (
                @DistributionID, @UserID, @SizeQty, @DeviceID, @PartSizeOrderId, @OperatorID, @InventoryQty,
                @Status, @CreatedAt, @UpdatedAt, @IsLeather, @IsDelete, @Note
              );
            `);
          }
        }
      } catch (err) {
        console.error(`❌ Error on DeviceID ${data.DeviceID}: ${err.message}`);
        status.Error = err.message;
      }

      results.push(status);
    }

    return results;

  } catch (err) {
    console.error('❌ Critical database error:', err.message);
    return { success: false, error: err.message };
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
          o.EmployeeID AS OperatorID,
          p.ART,
          pa.PartID,
          ISNULL(pa.VietnameseName, pa.PartName) AS PartName,
          m.MaterialCode,
          m.MaterialID,
          m.MaterialName,
          se.SizeID,
          se.Size,

          --SizeQty from SubDistribution, fallback to PartSizeOrder
          ISNULL(sd.SizeQty, ps.SizeQty) AS SizeQty,

          ISNULL(sd.InventoryQty, dd.InventoryQty) AS InventoryQty,
          di.PiecesPerPair,
          di.CuttingDieQty,
          di.MaterialLayer,
          di.TotalPiecesPerPair,
          do.ActualCut,
          do.ActualPieces,
          do.ActualSizeQty,
          ISNULL(sd.Status, dd.Status) AS Status,
          dd.CreatedAt

      FROM 
          DistributionData AS dd
      JOIN 
          PartSizeOrder AS ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
      JOIN 
          Part AS pa ON pa.PartID = ps.PartID
      JOIN 
          Size AS se ON se.SizeID = ps.SizeID
      JOIN 
          Material AS m ON m.MaterialID = ps.MaterialID
      JOIN 
          ProductOrder AS pr ON ps.OrderId = pr.OrderID
      JOIN 
          Product AS p ON pr.ProductId = p.ProductId
      LEFT JOIN 
          DefaultInfo AS di ON di.ProductID = p.ProductId AND di.PartID = pa.PartId AND di.Model = p.Model

      -- SubDistribution join
      LEFT JOIN 
          SubDistribution AS sd ON sd.DistributionID = dd.DistributionID
      LEFT JOIN 
          DeviceList AS d ON d.DeviceID = ISNULL(sd.DeviceID, dd.DeviceID)
      -- Operator from sub or fallback
      LEFT JOIN 
          Operator AS o ON o.OperatorID = ISNULL(sd.OperatorID, dd.OperatorID)
      LEFT JOIN 
          DeviceOutput AS do ON do.SizeID = se.SizeID AND do.OrderID = pr.OrderID AND do.PartID = pa.PartID AND do.OperatorID = o.EmployeeID

      WHERE 
          dd.IsDelete = 0  
          AND (d.IpAddress = @IpAddress)
          AND ISNULL(sd.Status, dd.Status) IN ('Complete', 'Pending') 
          AND EXISTS (
              SELECT 1
              FROM DistributionData AS sub_dd
              WHERE sub_dd.Status = 'Pending'
                AND FORMAT(dd.CreatedAt, 'dd/MM/yyyy HH:mm:ss') = FORMAT(sub_dd.CreatedAt, 'dd/MM/yyyy HH:mm:ss')
          )
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
      //console.log(soGroups);

      const orderID = parseInt(row.OrderID, 10);

     // Keep `SizeData` unchanged (original sum per SizeID)
     const sizeDataMap = new Map();

     result.recordset.forEach(item => {
       const key = `${item.SizeID}-${item.Size}-${item.PartName}-${item.PartID}`;
     
       if (!sizeDataMap.has(key)) {
         sizeDataMap.set(key, {
           SizeID: item.SizeID,
           Size: item.Size,
           PartName: item.PartName,
           PartID: item.PartID,
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
        OperatorID: row.OperatorID,
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
  }
}

async function getDistributionIDFromSizeID(ipAddress, orderId, isLeather, sizeID, partID) {
  try {
    const query = `
      SELECT DISTINCT  
        dd.DistributionID, 
        ps.PartSizeOrderId,
        se.SizeID
      FROM DistributionData dd
      LEFT JOIN DeviceList d ON dd.DeviceID = d.DeviceID
      JOIN PartSizeOrder ps ON dd.PartSizeOrderId = ps.PartSizeOrderId
      JOIN Part pa ON pa.PartID = ps.PartID
      JOIN Size se ON se.SizeID = ps.SizeID
      JOIN ProductOrder pr ON pr.OrderId = ps.OrderID
      WHERE 
        dd.IsDelete = 0
        AND (d.IpAddress = @IpAddress OR dd.DeviceID IS NULL)
        AND dd.Status = 'Pending'
        AND ps.OrderId = @OrderId
        AND dd.IsLeather = @IsLeather
        AND se.SizeID = @SizeID
        AND pa.PartID = @PartID
    `;

    const request = new sql.Request();
    request.input('IpAddress', sql.VarChar, ipAddress);
    request.input('OrderId', sql.Int, orderId);
    request.input('IsLeather', sql.Int, isLeather);
    request.input('SizeID', sql.Int, sizeID);
    request.input('PartID', sql.Int, partID);

    const result = await request.query(query);

    if (result.recordset.length > 0) {
      const distributionIDData = result.recordset.map(item => ({
        DistributionID: item.DistributionID,
        SizeID: item.SizeID
      }));

      return { DistributionID: distributionIDData };
    } else {
      return null;
    }
  } catch (error) {
    console.error(`❌ Error fetching distribution data: ${error.message}`);
    logToFile(errorLogPath, `Error fetching distribution data from DB: ${error.message}`);
  }
}

async function getAllDeviceData() {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request()
      .query('SELECT a.DeviceID, a.IpAddress, a.MachineName, a.ConnectionStatus, a.IsActive, b.PlantName, d.DepartmentName FROM DeviceList a JOIN Plant b ON a.PlantID = b.PlantID JOIN Department d ON d.DepartmentID= a.DepartmentID WHERE a.IsActive = 1');

    return result.recordset;  
  } catch (error) {
    console.error('Error fetching device data from database:', error.message);
  }
}

// Fetch data from SQL Server
async function getDistributions(startDate, endDate) {
  try {
    const pool = await sql.connect(dbConfig);

    const request = pool.request();
    request.input('startDate', sql.DateTime, startDate);
    request.input('endDate', sql.DateTime, endDate);

    // 1. Get paginated data
    const dataQuery = `
        SELECT 
            dd.DistributionID,
            dd.DeviceID,
            pr.SO,
            d.IpAddress,
            d.MachineName,
            o.OperatorName,
            u.EmployeeName,
            pa.PartName,
            pa.VietnameseName,
            se.Size,
            ps.Unit,
            ps.SizeQty,
            m.MaterialName,
            dd.InventoryQty,
            dd.Status,
            dd.CreatedAt,
            dd.UpdatedAt,
            dd.IsLeather,
            dd.IsDelete,
            dd.Note,
            ISNULL(do.ActualSizeQty, 0) AS ActualSizeQty
        FROM DistributionData dd
        OUTER APPLY (
            SELECT TOP 1 *
            FROM SubDistribution sd
            WHERE sd.DistributionID = dd.DistributionID
            ORDER BY sd.UpdatedAt DESC
        ) sd
        LEFT JOIN DeviceList d ON dd.DeviceID = d.DeviceID
        LEFT JOIN PartSizeOrder ps ON dd.PartSizeOrderId = ps.PartSizeOrderId
        LEFT JOIN Part pa ON ps.PartID = pa.PartID
        LEFT JOIN Size se ON ps.SizeID = se.SizeID
        LEFT JOIN Material m ON ps.MaterialID = m.MaterialID
        LEFT JOIN Operator o ON o.OperatorID = dd.OperatorID
        LEFT JOIN Users u ON dd.UserID = u.UserID
        LEFT JOIN ProductOrder pr ON ps.OrderID = pr.OrderID
        OUTER APPLY (
            SELECT TOP 1 do.ActualSizeQty
            FROM DeviceOutput do
            WHERE do.SizeID = ps.SizeID
              AND do.PartID = ps.PartID
              AND do.OrderID = ps.OrderID
            ORDER BY do.UpdatedAt DESC
        ) do
        WHERE dd.IsDelete = 0 AND 
          dd.UpdatedAt >= @startDate AND dd.UpdatedAt < @endDate
        ORDER BY dd.UpdatedAt ASC;
    `;

    // 2. Get total count
    const countQuery = `
      SELECT COUNT(*) AS TotalCount
      FROM DistributionData dd
      JOIN PartSizeOrder ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
      WHERE dd.IsDelete = 0 AND dd.UpdatedAt >= @startDate AND dd.UpdatedAt < @endDate;
    `;

    const [dataResult, countResult] = await Promise.all([
      request.query(dataQuery),
      pool.request()
        .input('startDate', sql.DateTime, startDate)
        .input('endDate', sql.DateTime, endDate)
        .query(countQuery)
    ]);

    return {
      records: dataResult.recordset,
      totalCount: countResult.recordset[0].TotalCount
    };

  } catch (err) {
    console.error('Database query failed: ' + err.message);
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
  }
}// Lấy danh sách thiết bị
async function getDeviceList() {
  try {
    const pool = await sql.connect(dbConfig);
    const result = await pool.request().query('SELECT IpAddress FROM DeviceList WHERE IsActive = 1');
    return result.recordset.map(record => record.IpAddress);
  } catch (error) {
    console.error('Error fetching device list from database:', error.message);
    return [];
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
//  console.log(`Adding device with ipAddress: ${ipAddress}, machineName: ${machineName}, plantName: ${plantName}`);

  try {
    // Kiểm tra các tham số
    if (!ipAddress || !machineName || !plantName) {
      console.warn('Missing required fields: ipAddress, machineName, or plantName');
    }

    // Câu truy vấn lấy PlantID dựa trên tên Plant
    const plantQuery = `
      SELECT PlantID
      FROM Plant
      WHERE LTRIM(RTRIM(Plant)) = @PlantName
    `;

    const plantInputs = [{ name: 'PlantName', type: sql.NVarChar, value: plantName.trim() }];
   // console.log(`Executing plant query with plantName: '${plantName.trim()}'`);

    const plantResult = await executeQuery(plantQuery, plantInputs);
   // console.log(`Query result: ${JSON.stringify(plantResult)}`);

    // Kiểm tra nếu không có PlantID
    if (!plantResult || plantResult.length === 0) {
   //   console.log(`No Plant found with name: ${plantName}`);
      return { status: 'error', message: `No Plant found with name: ${plantName}` };  
    }

    const plantID = plantResult[0].PlantID;
   // console.log(`Found PlantID: ${plantID}`);

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
   // console.log(`Device ${machineName} added successfully with PlantID ${plantID}`);

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

async function getListOfSOsByYear(year) {
  try {
    if (!pool) await initDatabase();

    const result = await pool.request()
      .input('Year', sql.Int, year)
      .query(`
        SELECT DISTINCT po.SO, po.CreatedAt
        FROM Product p
        JOIN ProductOrder po ON p.ProductId = po.ProductId
        JOIN PartSizeOrder pso ON po.OrderID = pso.OrderID
        JOIN Part pa ON pso.PartId = pa.PartId
        JOIN Material m ON pso.MaterialID = m.MaterialID
        JOIN Size s ON pso.SizeId = s.SizeID
        WHERE YEAR(po.CreatedAt) = @Year
      `);

    return result.recordset.map(row => ({ SO: row.SO, CreatedAt: row.CreatedAt }));
  } catch (err) {
    console.error('Error getting SO list by year:', err.message);
    return [];
  }
}

async function getProductionSchedule(soList, includeDistributed) {
  try {
    if (!pool) await initDatabase(); 

    // Create dynamic parameters for each SO
    const soParams = soList.map((_, index) => `@SO${index}`).join(', ');
    const request = pool.request();

    // Add each SO to the request
    soList.forEach((so, index) => {
      request.input(`SO${index}`, sql.NVarChar, so);
    });

    // clause
    let conditionClause = '', joinDeviceOutput = '', selectDeviceOutput = '';
    if (includeDistributed) {
      conditionClause = ` AND d.PartSizeOrderId IS NULL`;
    } else {
      selectDeviceOutput =  ` ,do.ActualCut, do.CuttingDieQty, do.PiecesPerPair AS PeicesPerPair, do.MaterialLayer, do.TotalPiecesPerPair`;
      joinDeviceOutput = `  LEFT JOIN DeviceOutput do ON do.SizeID = s.SizeId AND do.PartId = pa.PartId AND do.OrderID = po.OrderId`;
    }
    const query = `
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
          po.UpdatedAt,
          d.Status,
          d.InventoryQty ${selectDeviceOutput}
      FROM Product p
      JOIN ProductOrder po ON p.ProductId = po.ProductId
      JOIN PartSizeOrder pso ON po.OrderID = pso.OrderID
      JOIN Part pa ON pso.PartId = pa.PartId
      JOIN Material m ON pso.MaterialID = m.MaterialID
      JOIN Size s ON pso.SizeId = s.SizeID
      LEFT JOIN DistributionData d ON pso.PartSizeOrderId = d.PartSizeOrderId
      ${joinDeviceOutput}
      WHERE po.SO IN (${soParams})  ${conditionClause}
    `;

    const result = await request.query(query);
    return result.recordset;
  } catch (err) {
    console.error('Lỗi khi truy cập cơ sở dữ liệu:', err.message);
    return [];
  }
}

async function getAllProductionSchedule(month = null, year = null, includeDistributed) {
  try {
    if (!pool) await initDatabase(); 

    const targetDate = new Date();
    targetDate.setMonth(targetDate.getMonth());
    const targetMonth = month ?? targetDate.getMonth() + 1; // JS month is 0-based
    const targetYear = year ?? targetDate.getFullYear();

    let whereClause = `
      MONTH(po.CreatedAt) = ${targetMonth}
      AND YEAR(po.CreatedAt) = ${targetYear}
    `;

    if (includeDistributed) {
      whereClause += ` AND d.PartSizeOrderId IS NULL AND d.Status IS NULL`;
    }
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
      LEFT JOIN DistributionData d ON pso.PartSizeOrderId = d.PartSizeOrderId
      WHERE ${whereClause}
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
  }
}

// Lấy danh sách users departmentID
async function getOperatorList(departmentID) {
  try {
    const pool = await sql.connect(dbConfig);
    const request = pool.request();

    let query = 'SELECT * FROM [CuttingProjectData].[dbo].[Operator]';

    if (departmentID && departmentID > 0) {
      request.input('DepartmentID', sql.Int, departmentID);
      query += ' WHERE DepartmentID = @DepartmentID';
    }

    const result = await request.query(query);
    return result.recordset;
  } catch (error) {
    console.error('Error fetching user list from database:', error.message);
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
// ✅ 3. CRONJOB function
// async function calculateActiveHours() {
//   try {
//     await sql.connect(dbConfig);

//     const result = await sql.query`
//       SELECT d.DeviceID,
//              MAX(c.CutTime) AS LastCutTime,
//              ISNULL(da.ActiveHours, 0) AS ActiveHoursToday
//       FROM DeviceList d
//       LEFT JOIN CutHistory c ON d.DeviceID = c.DeviceID
//       LEFT JOIN DeviceDailyActivity da 
//              ON d.DeviceID = da.DeviceID 
//             AND da.ActivityDate = CAST(GETDATE() AS DATE)
//       GROUP BY d.DeviceID, da.ActiveHours;
//     `;

//     const now = new Date();

//     for (const row of result.recordset) {
//       const { DeviceID, LastCutTime, ActiveHoursToday } = row;
//       if (!LastCutTime) continue;

//       const diff = (now - LastCutTime) / 1000; // seconds
//       if (diff <= 60) {
//         const newActiveHours = ActiveHoursToday + 1 / 60;

//         await sql.query`
//           MERGE DeviceDailyActivity AS target
//           USING (SELECT ${DeviceID} AS DeviceID, CAST(GETDATE() AS DATE) AS ActivityDate) AS src
//           ON target.DeviceID = src.DeviceID AND target.ActivityDate = src.ActivityDate
//           WHEN MATCHED THEN
//             UPDATE SET ActiveHours = ${newActiveHours}
//           WHEN NOT MATCHED THEN
//             INSERT (DeviceID, ActivityDate, ActiveHours)
//             VALUES (${DeviceID}, CAST(GETDATE() AS DATE), ${newActiveHours});
//         `;

//         const efficiency = (newActiveHours / 8) * 100;

//         const broadcastMsg = JSON.stringify({
//           action: "activeHoursUpdated",
//           deviceId: DeviceID,
//           activeHours: newActiveHours,
//           efficiency,
//           updatedAt: now
//         });

//         wss.clients.forEach(client => {
//           if (client.readyState === WebSocket.OPEN) {
//             client.send(broadcastMsg);
//           }
//         });

//         console.log(`✅ Device ${DeviceID} cutting, total active: ${newActiveHours.toFixed(2)}h`);
//       }
//     }
//   } catch (err) {
//     console.error("❌ Cronjob error:", err);
//   }
// }


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
  setSubDistributionComplete,
  getUserList,
  getOperatorList,
  getDistributions,
  getOperatorDistribution,
  getSizeAndDistributionDataFromDb,
  getDistributionIDFromSizeID,
  getDistributionCompleteFromDb,
  getListOfSOsByYear,
  logCutHistoryToDB,
  getSubDistributions,
 // calculateActiveHours
};
