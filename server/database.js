const sql = require('mssql');

// Cấu hình cơ sở dữ liệu
const dbConfig = {
  user: 'sa',
  password: '12345',
  server: '10.30.0.116',
  database: 'CuttingData',
  options: {
    encrypt: false,
    trustServerCertificate: true,
  },
};

let pool;

// Khởi tạo kết nối cơ sở dữ liệu
async function initDatabase() {
  if (pool) return pool; // Nếu đã có kết nối, trả về kết nối hiện tại
  try {
    pool = await sql.connect(dbConfig);
    console.log('Kết nối cơ sở dữ liệu thành công');
    return pool;
  } catch (err) {
    console.error('Lỗi kết nối cơ sở dữ liệu:', err.message);
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

// Hàm lấy dữ liệu OutputData từ cơ sở dữ liệu
async function getActualOutputData(orderID, masterWorkOrder) {
  const query = `
    SELECT 
      om.[OrderID],
      om.[PartName],
      om.[MaterialsName],
      do.[MasterWorkOrder],
      do.[SO],
      do.[Model],
      do.[ART],
      do.[UserName],
      os.[Size],
      os.[SizeQty],
      do2.[PiecesPerPair],
      do2.[MaterialLayer],
      do2.[CuttingDieQty],
      do2.[ActualCut],
      do2.[ActualSizeQty],
      do2.[ActualPieces]
    FROM 
      [CuttingData].[dbo].[OrderMaterials] om
    JOIN 
      [CuttingData].[dbo].[DistributionOrders] do ON om.[OrderID] = do.[OrderID]
    JOIN 
      [CuttingData].[dbo].[OrderSizes] os ON om.[MaterialID] = os.[MaterialID]
    JOIN 
      [CuttingData].[dbo].[DeviceOutput] do2 ON do.[OrderID] = do2.[OrderID] AND os.[SizeID] = do2.[SizeID] AND om.[MaterialID] = do2.[MaterialID]
    WHERE 
      do.[OrderID] = @OrderID
      AND do.[MasterWorkOrder] = @MasterWorkOrder;
  `;

  try {
    const request = (await initDatabase()).request();
    
    // Đảm bảo OrderID là số nguyên hợp lệ trước khi truyền vào câu lệnh SQL
    if (isNaN(orderID) || orderID <= 0) {
      throw new Error('Invalid OrderID. It must be a positive integer.');
    }

    request.input('OrderID', sql.Int, orderID);
    request.input('MasterWorkOrder', sql.NVarChar, masterWorkOrder);

    const result = await request.query(query);

    if (result.recordset.length > 0) {
      const firstRecord = result.recordset[0];
      
      const filteredOutputData = result.recordset.filter(record => record.SizeQty > 0).map(record => ({
        PartName: record.PartName, 
        MaterialsName: record.MaterialsName,
        Size: record.Size,
        SizeQty: record.SizeQty,
        PiecesPerPair: record.PiecesPerPair,
        MaterialLayer: record.MaterialLayer,
        CuttingDieQty: record.CuttingDieQty,
        ActualCut: record.ActualCut,
        ActualSizeQty: record.ActualSizeQty,
        ActualPieces: record.ActualPieces
      }));
      
      return {
        OrderID: firstRecord.OrderID,
        MasterWorkOrder: firstRecord.MasterWorkOrder,
        SO: firstRecord.SO,
        Model: firstRecord.Model,
        UserName: firstRecord.UserName,
        OutputData: filteredOutputData 
      };
    } else {
      return null;
    }
  } catch (error) {
    console.error('Error fetching actual output data from database:', error.message);
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

async function saveActualDataToDB(data, PartName) {
  const { OrderID, SizeData } = data;

  // Kiểm tra OrderID và PartName trước khi tiếp tục
  if (!OrderID || OrderID === 0) {
    throw new Error('OrderID không hợp lệ');
  }

  if (!PartName || typeof PartName !== 'string' || PartName.trim() === '') {
    throw new Error('PartName không hợp lệ');
  }

  try {
    // Truy vấn MaterialID từ bảng OrderMaterials dựa trên PartName và OrderID
    const materialIDQuery = `
      SELECT MaterialID
      FROM OrderMaterials
      WHERE PartName = @PartName AND OrderID = @OrderID;
    `;

    const materialIDResult = await executeQuery(materialIDQuery, [
      { name: 'PartName', type: sql.NVarChar, value: PartName },
      { name: 'OrderID', type: sql.Int, value: OrderID }
    ]);

    // Kiểm tra xem materialIDResult có dữ liệu không
    if (!materialIDResult || materialIDResult.length === 0) {
      throw new Error(`Không tìm thấy MaterialID cho PartName: ${PartName}`);
    }

    const MaterialID = materialIDResult[0].MaterialID;

    for (const size of SizeData) {
      if (!size.Size || typeof size.Size !== 'string') {
        throw new Error(`Kích thước không hợp lệ: ${size.Size}`);
      }

      if (!size.MaterialLayer || typeof size.MaterialLayer !== 'number') {
        size.MaterialLayer = 0;
      }

      const sizeIDQuery = `
        SELECT SizeID
        FROM OrderSizes
        WHERE Size = @Size AND MaterialID = @MaterialID;
      `;

      const sizeIDResult = await executeQuery(sizeIDQuery, [
        { name: 'Size', type: sql.NVarChar, value: size.Size },
        { name: 'MaterialID', type: sql.Int, value: MaterialID }
      ]);

      // Kiểm tra xem sizeIDResult có dữ liệu không
      if (!sizeIDResult || sizeIDResult.length === 0) {
        console.log(`Không tìm thấy SizeID cho Size: ${size.Size} và MaterialID: ${MaterialID}. Bỏ qua cập nhật.`);
        continue;
      }

      for (const sizeIDRecord of sizeIDResult) {
        const SizeID = sizeIDRecord.SizeID;

        // Ghi nhật ký SizeID đang xử lý
        console.log(`Đang cập nhật SizeID: ${SizeID} cho Size: ${size.Size}`);

        const updateDeviceOutputQuery = `
          UPDATE DeviceOutput
          SET
            PiecesPerPair = @PiecesPerPair,
            MaterialLayer = @MaterialLayer,
            CuttingDieQty = @CuttingDieQty,
            ActualCut = @ActualCut,
            ActualPieces = @ActualPieces,
            ActualSizeQty = @ActualSizeQty,
            UpdatedAt = GETDATE()
          WHERE
            OrderID = @OrderID
            AND SizeID = @SizeID
            AND MaterialID = @MaterialID;
        `;

        const updateDeviceOutputInputs = [
          { name: 'OrderID', type: sql.Int, value: OrderID },
          { name: 'SizeID', type: sql.Int, value: SizeID },
          { name: 'MaterialID', type: sql.Int, value: MaterialID },
          { name: 'PiecesPerPair', type: sql.Int, value: size.PiecesPerPair },
          { name: 'MaterialLayer', type: sql.Int, value: size.MaterialLayer },
          { name: 'CuttingDieQty', type: sql.Int, value: size.CuttingDieQty },
          { name: 'ActualCut', type: sql.Int, value: size.ActualCut },
          { name: 'ActualPieces', type: sql.Int, value: size.ActualPieces },
          { name: 'ActualSizeQty', type: sql.Int, value: size.ActualSizeQty }
        ];

        await executeQuery(updateDeviceOutputQuery, updateDeviceOutputInputs);
      }
    }

    console.log('Dữ liệu Actual đã được cập nhật thành công');
  } catch (error) {
    console.error('Lỗi khi cập nhật Actual data:', error.message);
    throw error;
  }
}

async function saveDistributionDataToDB(data) {
  const orderQuery = `
    INSERT INTO DistributionOrders (MasterWorkOrder, SO, Model, ART, UserName, IpAddress, CreatedAt, Status)
    VALUES (@MasterWorkOrder, @SO, @Model, @ART, @UserName, @IpAddress, GETDATE(), DEFAULT);
    SELECT SCOPE_IDENTITY() AS OrderID;
  `;
  const orderInputs = [
    { name: 'MasterWorkOrder', type: sql.NVarChar, value: data.MasterWorkOrder },
    { name: 'SO', type: sql.NVarChar, value: data.SO },
    { name: 'Model', type: sql.NVarChar, value: data.Model },
    { name: 'ART', type: sql.NVarChar, value: data.ART },
    { name: 'UserName', type: sql.NVarChar, value: data.User },
    { name: 'IpAddress', type: sql.VarChar, value: data.IpAddress },
  ];

  try {
    // Chèn vào bảng DistributionOrders
    const orderResult = await executeQuery(orderQuery, orderInputs);
    const orderID = orderResult[0].OrderID;

    // Bước 1: Chèn tất cả các PartName vào bảng OrderMaterials và lấy MaterialID
    const materialIdMap = {};
    for (const material of data.MaterialData) {
      const materialQuery = `
        INSERT INTO OrderMaterials (OrderID, PartName, MaterialsName)
        VALUES (@OrderID, @PartName, @MaterialsName);
        SELECT SCOPE_IDENTITY() AS MaterialID;
      `;
      const materialInputs = [
        { name: 'OrderID', type: sql.Int, value: orderID },
        { name: 'PartName', type: sql.NVarChar, value: material.PartName },
        { name: 'MaterialsName', type: sql.NVarChar, value: material.MaterialsName },
      ];
      const materialResult = await executeQuery(materialQuery, materialInputs);
      const materialID = materialResult[0].MaterialID;
      materialIdMap[material.PartName] = materialID;
    }

    // Bước 2: Chèn tất cả các Size vào bảng OrderSizes cho từng MaterialID
    for (const material of data.MaterialData) {
      const materialID = materialIdMap[material.PartName];
      if (!materialID) {
        console.log(`Không tìm thấy MaterialID cho PartName: ${material.PartName}`);
        continue;
      }

      // Lặp qua tất cả các Size từ SizeData và chèn vào OrderSizes
      for (const size of data.SizeData) {
        const sizeQuery = `
          INSERT INTO OrderSizes (MaterialID, Size, SizeQty)
          VALUES (@MaterialID, @Size, @SizeQty);
          SELECT SCOPE_IDENTITY() AS SizeID;
        `;
        const sizeInputs = [
          { name: 'MaterialID', type: sql.Int, value: materialID },
          { name: 'Size', type: sql.NVarChar, value: size.Size },
          { name: 'SizeQty', type: sql.Int, value: size.SizeQty },
        ];

        const sizeResult = await executeQuery(sizeQuery, sizeInputs);
        const sizeID = sizeResult[0].SizeID;
      }
    }

    // Bước 3: Chèn dữ liệu vào bảng DeviceOutput với SizeID và MaterialID đã có
    for (const material of data.MaterialData) {
      const materialID = materialIdMap[material.PartName];
      if (!materialID) {
        console.log(`Không tìm thấy MaterialID cho PartName: ${material.PartName}`);
        continue;
      }

      // Lặp qua tất cả các Size từ SizeData và chèn vào DeviceOutput
      for (const size of data.SizeData) {
        const sizeQuery = `
          SELECT SizeID FROM OrderSizes WHERE MaterialID = @MaterialID AND Size = @Size;
        `;
        const sizeInputs = [
          { name: 'MaterialID', type: sql.Int, value: materialID },
          { name: 'Size', type: sql.NVarChar, value: size.Size },
        ];

        const sizeResult = await executeQuery(sizeQuery, sizeInputs);
        const sizeID = sizeResult[0]?.SizeID;

        if (!sizeID) {
          console.log(`Không tìm thấy SizeID cho Size: ${size.Size} của MaterialID: ${materialID}`);
          continue;
        }

        const deviceOutputQuery = `
          INSERT INTO DeviceOutput (OrderID, SizeID, MaterialID, CreatedAt)
          VALUES (@OrderID, @SizeID, @MaterialID, GETDATE());
        `;
        const deviceOutputInputs = [
          { name: 'OrderID', type: sql.Int, value: orderID },
          { name: 'SizeID', type: sql.Int, value: sizeID },
          { name: 'MaterialID', type: sql.Int, value: materialID },
        ];
        await executeQuery(deviceOutputQuery, deviceOutputInputs);
      }
    }

    console.log('Dữ liệu phân phối và thông tin thiết bị đã được lưu thành công');
  } catch (error) {
    console.error('Lỗi khi lưu dữ liệu phân phối:', error.message);
    throw error;
  }
}

async function getDistributionDataFromDb(ipAddress) {
  try {
    const query = `
      SELECT 
        DO.OrderID,
        DO.MasterWorkOrder,
        DO.SO,
        DO.Model,
        DO.UserName,
        DO.ART,
        OM.PartName,
        OM.MaterialsName,
        DO.IpAddress,
        DO.CreatedAt
      FROM 
        [CuttingData].[dbo].[DistributionOrders] AS DO

      JOIN 
        [CuttingData].[dbo].[OrderMaterials] AS OM ON DO.OrderID = OM.OrderID
     
      JOIN 
        [CuttingData].[dbo].[OrderSizes] AS OS ON OS.MaterialID = OM.MaterialID

      WHERE 
        DO.IpAddress = @IpAddress AND DO.Status = 'Pending'
      ORDER BY 
        DO.CreatedAt ASC  
    `;
    
    const request = new sql.Request();
    request.input('IpAddress', sql.VarChar, ipAddress);
    
    const result = await request.query(query);
    
    if (result.recordset.length > 0) {
      const row = result.recordset[0];
      
      const orderID = parseInt(row.OrderID, 10);

      // Lấy thông tin MaterialData (PartName và MaterialsName) mà không thay đổi gì
      const materialData = result.recordset
        .map(r => ({
          PartName: r.PartName,
          MaterialsName: r.MaterialsName  // Giữ nguyên MaterialsName mà không thay đổi
        }))
        .filter((value, index, self) => self.findIndex(t => t.PartName === value.PartName && t.MaterialsName === value.MaterialsName) === index);  // Loại bỏ trùng lặp PartName và MaterialsName

      // Trả về dữ liệu theo cấu trúc yêu cầu
      return {
        OrderID: orderID,
        MasterWorkOrder: row.MasterWorkOrder,
        SO: row.SO,
        Model: row.Model,
        ART: row.ART,
        MaterialData: materialData
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
      .query('SELECT * FROM DeviceList JOIN Plant ON DeviceList.PlantID = Plant.PlantID WHERE DeviceList.IsActive = 1');

    return result.recordset;  
  } catch (error) {
    console.error('Error fetching device data from database:', error.message);
    throw error;
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
// Lấy phân phối đơn từ Master Work Order
async function getProductionSchedule(masterWorkOrder, page) {
  try {
    if (!pool) await initDatabase(); 
    const result = await pool.request()
      .input('MasterWorkOrder', sql.NVarChar, masterWorkOrder)
      .input('Page', sql.NVarChar, page)
      .query('SELECT * FROM [CuttingData] WHERE MasterWorkOrder = @MasterWorkOrder AND Page = @Page');
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
  getPlantNames,
  getSizeDataFromDB,
  getDistributionDataFromDb,
  getActualOutputData,
  setOrderIsComplete,
};
