const WebSocket = require('ws');
const { connectToDevice, isHostReachable, modbusClients } = require('./modbusClient');
const { getDeviceList, updateDeviceConnectionStatus, getActualOutputData, getAllDeviceData, getDistributionByDevice, getPlantNames, addDeviceToList, getProductionSchedule, getUniquePages, saveDistributionDataToDB, getUserList, getOperatorList, getAllProductionSchedule, getDistributions, getOperatorDistribution } = require('./database');
const { setClients } = require('./notifications'); 
const { Time } = require('mssql');

let clients = [];
//const modusClients = {};

// Thiết lập WebSocket server
function setupWebSocket(server) {
  const wss = new WebSocket.Server({ noServer: true });

  // Xử lý yêu cầu upgrade WebSocket
  server.on('upgrade', (request, socket, head) => {
    wss.handleUpgrade(request, socket, head, (ws) => {
      wss.emit('connection', ws, request);
    });
  });

  // Khi có client kết nối
  wss.on('connection', (ws) => {
    const time = new Date().toLocaleTimeString(); // Lấy giờ:phút:giây
    console.log(`Client connected at ${time}`);
    
    clients.push(ws);
    setClients(clients); // Cập nhật danh sách client khi có kết nối mới

    // Xử lý thông điệp từ client
    ws.on('message', (message) => {
        const time = new Date().toLocaleTimeString();
        console.log(`[${time}] Received message: ${message}`);
        handleClientMessage(ws, message);
    });
    // Khi client ngắt kết nối
    ws.on('close', () => {
      const time = new Date().toLocaleTimeString();
      console.log(`Client disconnected at ${time}`);
      
      clients = clients.filter(client => client !== ws); // Loại bỏ client khỏi danh sách
      setClients(clients); // Cập nhật danh sách client khi có kết nối bị ngắt
    });
  });
}

async function handleClientMessage(ws, message) {
  try {
    const request = JSON.parse(message);
    const { app, action } = request;

    if (!app) {
      console.log('Missing app field');
      ws.send(JSON.stringify({ error: 'Missing app field' }));
      return;
    }

    if (app === "CuttingProject") {
      switch (action) {
        case 'getDevices':
          await handleGetDevices(ws);
          break;
        case 'connectDevice':
          await handleConnectDevice(ws, request);
          break;
        case 'getDistributionOfDevice':
          await handleGetDistributionOfDevice(ws, request);
          break;
        case 'getDistributions':
          await handleGetDistributions(ws, request);
          break;
        case 'getActualData':
          await handleGetActualData(ws, request);
          break;
        case 'disconnectDevice':
          await handleDisconnectDevice(ws, request);
          break;
        case 'getPlants':
          await handleGetPlant(ws);
          break;
        case 'addDevice':
          await handleAddDevice(ws, request);
          break;
        case 'getSchedule':
          await handleGetSchedule(ws, request);
          break;
        case 'getUniquePages':
          await handleGetUniquePages(ws, request);
          break;
        case 'saveDistributionData':
          await handleSaveDistributionData(ws, request);
          break;
        case 'getUsers':
          await handleGetUsers(ws, request);
          break;
        case 'getOperators':
          await handleGetOperators(ws, request);
          break;
        case 'getOperatorDistribution':
          await handleGetOperatorDistribution(ws, request);
          break;
        default:
          console.log('Unknown action for CuttingProject:', action);
          ws.send(JSON.stringify({ error: 'Unknown action' }));
          break;
      }
    } else if (app === "KPIReport") {
      switch (action) {
        case 'generateReport':
          await handleGenerateReport(ws, request);
          break;
        case 'getKPIData':
          await handleGetKPIData(ws, request);
          break;
        case 'sendReport':
          await handleSendReport(ws, request);
          break;
        default:
          console.log('Unknown action for KPIReport:', action);
          ws.send(JSON.stringify({ error: 'Unknown action' }));
          break;
      }
    } else {
      console.log('Unknown app:', app);
      ws.send(JSON.stringify({ error: 'Unknown app' }));
    }
  } catch (error) {
    console.error('Error processing message:', error);
    ws.send(JSON.stringify({ error: 'Invalid message format' }));
  }
}


// Hàm xử lý yêu cầu lấy dữ liệu sản lượng thực tế
async function handleGetActualData(ws, request) {
  try {
    const { filter } = request;

    // Get the current date in YYYY-MM-DD format
    const currentDate = new Date();
    const formattedCurrentDate = currentDate.toISOString().split('T')[0]; // YYYY-MM-DD
    
    // Ensure startDate and endDate are Date objects
    const startDate = filter?.startDate ? new Date(filter.startDate) : new Date(formattedCurrentDate);
    let endDate = filter?.endDate ? new Date(filter.endDate) : new Date(formattedCurrentDate);
    
    // Set endDate to 23:59:59.999 in local time
    endDate.setHours(23, 59, 59, 999);
    // Add 1 day to the endDate
    endDate.setDate(endDate.getDate() + 1);
    
    // Format dates for SQL (YYYY-MM-DD HH:mm:ss.SSS) - LOCAL TIME
    const formatDateForSQL = (date) => {
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ` +
             `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}.${String(date.getMilliseconds()).padStart(3, '0')}`;
    };
    
    const formattedStartDate = formatDateForSQL(startDate);
    const formattedEndDate = formatDateForSQL(endDate);
    
    console.log("Start Date:", formattedStartDate); // 2025-03-04 00:00:00.000
    console.log("End Date:", formattedEndDate);     // 2025-03-04 23:59:59.999
    
    
    // Fetch real-time data once and send response
    const realTimeData = await getActualOutputData(startDate, endDate);    

    if (!realTimeData) {
      return ws.send(JSON.stringify({
        action: 'getActualData',
        status: 'error',
        message: 'No real-time data found'
      }));
    }

    // Send the real-time data once
    ws.send(JSON.stringify({
      action: 'getActualData',
      status: 'success',
      realTime: realTimeData
    }));

    // 🛑 Ensure only one 'close' listener per WebSocket
    const closeHandler = () => {
      ws.removeListener('close', closeHandler); // 🛠 Remove the event listener to prevent memory leaks
      console.log('Client disconnected.');
    };

    ws.removeAllListeners('close'); // 🛑 Prevent multiple listeners from stacking
    ws.on('close', closeHandler); // Attach the single close event

  } catch (error) {
    console.error('Error fetching real-time data:', error);
    ws.send(JSON.stringify({
      action: 'getActualData',
      status: 'error',
      message: `Failed to get real-time data: ${error.message}`
    }));
  }
}


// Xử lý yêu cầu lấy thông tin phân phối của thiết bị
async function handleGetDistributions(ws) {
  try {
    // Gọi hàm getDistributionByDevice từ database.js để lấy dữ liệu phân phối
    const distributionData = await getDistributions();

    if (!distributionData) {
      return ws.send(JSON.stringify({
        action: 'getDistributions',
        status: 'error',
        message: 'No distribution data found'
      }));
    }

    // Gửi phản hồi thành công với dữ liệu phân phối
    ws.send(JSON.stringify({
      action: 'getDistributions',
      status: 'success',
      distributionData: distributionData
    }));

  } catch (error) {
    console.error(`Error getting distribution data for device :  ${error.message}`);
    ws.send(JSON.stringify({
      action: 'getDistributions',
      status: 'error',
      message: `Failed to get distribution data : ${error.message}`
    }));
  }
}
// Xử lý yêu cầu lấy thông tin phân phối của thiết bị
async function handleGetDistributionOfDevice(ws, request) {
  const { ipAddress } = request;

  if (!ipAddress) {
    return ws.send(JSON.stringify({
      action: 'getDistributionOfDevice',
      status: 'error',
      message: 'Missing IpAddress parameter'
    }));
  }

  try {
    // Gọi hàm getDistributionByDevice từ database.js để lấy dữ liệu phân phối
    const distributionData = await getDistributionByDevice(ipAddress);

    if (!distributionData) {
      return ws.send(JSON.stringify({
        action: 'getDistributionOfDevice',
        status: 'error',
        message: `No distribution data found for device at ${ipAddress}`
      }));
    }

    // Gửi phản hồi thành công với dữ liệu phân phối
    ws.send(JSON.stringify({
      action: 'getDistributionOfDevice',
      status: 'success',
      distributionData: distributionData
    }));

  } catch (error) {
    console.error(`Error getting distribution data for device ${ipAddress}:`, error);
    ws.send(JSON.stringify({
      action: 'getDistributionOfDevice',
      status: 'error',
      message: `Failed to get distribution data for device at ${ipAddress}: ${error.message}`
    }));
  }
}
// Xử lý yêu cầu lấy danh sách thiết bị
async function handleGetDevices(ws) {
  try {
    // Lấy dữ liệu thiết bị từ cơ sở dữ liệu hoặc nguồn khác
    const devicesResponse = await getAllDeviceData();

    // Gửi phản hồi thành công với dữ liệu danh sách thiết bị
    ws.send(JSON.stringify({
      action: 'getDevices',
      status: 'success',
      devices: devicesResponse
    }));

  } catch (error) {
    console.error('Error getting device list:', error);

    // Gửi phản hồi lỗi nếu có sự cố khi lấy danh sách thiết bị
    ws.send(JSON.stringify({
      action: 'getDevices',
      status: 'error',
      error: error.message || 'Failed to retrieve device list'
    }));
  }
}

// Xử lý yêu cầu kết nối thiết bị
async function handleConnectDevice(ws, request) {
  const { ipAddress } = request;
  try {
    const reachable = await isHostReachable(ipAddress);
    if (!reachable) {
      return ws.send(JSON.stringify({
        action: 'connectDevice',
        ipAddress,
        status: 'disconnected',
        error: `Device at ${ipAddress} is unreachable.`
      }));
    }

    if (modbusClients[ipAddress] && modbusClients[ipAddress].isConnected) {
      return ws.send(JSON.stringify({
        action: 'connectDevice',
        ipAddress,
        status: 'already_connected'
      }));
    }

    //await connectToDevice(ipAddress);
    //await updateDeviceConnectionStatus(ipAddress, true);
    return ws.send(JSON.stringify({
      action: 'connectDevice',
      ipAddress,
      status: 'connected'
    }));

    // Cập nhật danh sách thiết bị cho tất cả client
   // notifyClients(await getAllDeviceData());
  } catch (error) {
    console.error(`Error connecting to device ${ipAddress}:`, error);
    ws.send(JSON.stringify({
      action: 'connectDevice',
      ipAddress,
      status: 'disconnected',
      error: `Failed to connect to ${ipAddress}: ${error.message}`
    }));
  }
}

// Xử lý yêu cầu ngắt kết nối thiết bị
async function handleDisconnectDevice(ws, request) {
  const { ipAddress } = request;
  try {
    await disconnectFromDevice(ipAddress);
    await setDeviceConnectionStatus(ipAddress, false);
    ws.send(JSON.stringify({
      action: 'disconnectDevice',
      ipAddress,
      status: 'disconnected'
    }));

    notifyClients(await getDeviceList());
  } catch (error) {
    console.error(`Error disconnecting from device ${ipAddress}:`, error);
    ws.send(JSON.stringify({
      action: 'disconnectDevice',
      ipAddress,
      status: 'error',
      error: error.message
    }));
  }
}

async function handleAddDevice(ws, request) {
  const { action, ipAddress, machineName, plant } = request;

  console.log(`Received addDevice request with ipAddress: ${ipAddress}, machineName: ${machineName}, plant: ${plant}`);

  if (!ipAddress || !machineName || !plant) {
    console.log('Error: Missing required fields: ipAddress, machineName, or plant');
    return ws.send(JSON.stringify({ action: 'addDevice', status: 'error', message: 'Missing required fields.' }));
  }

  // Kiểm tra và loại bỏ khoảng trắng thừa
  const trimmedIpAddress = ipAddress.trim();
  const trimmedMachineName = machineName.trim();
  const trimmedPlant = plant.trim();

  console.log(`Trimmed values: ipAddress: ${trimmedIpAddress}, machineName: ${trimmedMachineName}, plant: ${trimmedPlant}`);

  const result = await addDeviceToList({ ipAddress: trimmedIpAddress, machineName: trimmedMachineName, plantName: trimmedPlant });

  ws.send(JSON.stringify({ action: 'addDevice', status: result.status, message: result.message }));

  if (result.status === 'success') {
    await handleGetDevices(ws); 
  }
}

// Xử lý yêu cầu lấy lịch trình sản xuất
async function handleGetSchedule(ws, request) {
  try {
    let schedule;
    
    if (request.so) {
      // Get schedule for a specific SO
      schedule = await getProductionSchedule(request.so);
    } else {
      // Get all schedules if no SO is provided
      schedule = await getAllProductionSchedule();
    }

    ws.send(JSON.stringify({ action: 'getSchedule', status: 'success', schedule }));
  } catch (error) {
    console.error('Error fetching production schedule:', error);
    ws.send(JSON.stringify({ action: 'getSchedule', status: 'error', message: 'Failed to retrieve production schedule' }));
  }
}


// Xử lý yêu cầu lấy các trang duy nhất
async function handleGetUniquePages(ws, request) {
  const { masterWorkOrder } = request;

  if (!masterWorkOrder) {
    return ws.send(JSON.stringify({ action: 'getUniquePages', status: 'error', message: 'Missing masterWorkOrder parameter' }));
  }

  try {
    const pages = await getUniquePages(masterWorkOrder);
    if (!pages || pages.length === 0) {
      return ws.send(JSON.stringify({
        action: 'getUniquePages',
        status: 'error',
        message: 'No pages data found'
      }));
    }

    ws.send(JSON.stringify({
      action: 'getUniquePages',
      status: 'success',
      pages
    }));
  } catch (error) {
    console.error('Error fetching pages:', error.message);
    ws.send(JSON.stringify({
      action: 'getUniquePages',
      status: 'error',
      message: `Failed to retrieve pages: ${error.message}`
    }));
  }
}

// Function to insert data into the database
async function handleSaveDistributionData(ws, request) {
  const { data } = request;

  if (!data) {
    return ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: 'Missing required data'
    }));
  }

  try {

    // Lưu dữ liệu vào cơ sở dữ liệu
    const result = await saveDistributionDataToDB(data);
    if (!result || result.length === 0) {
      console.log("⚠️ No results returned from the function.");
      ws.send(JSON.stringify({
        action: 'saveDistributionData',
        status: 'error',
        message: `Failed to save distribution data: ${error.message}`
      }));
    } else if (result[0].DistributionDuplicate) {
        console.log("🚨 Entry is a duplicate. Insertion skipped.");
         ws.send(JSON.stringify({
          action: 'saveDistributionData',
          status: 'error',
          message: 'Entry is a duplicate. Insertion skipped!!'
        }));
    } else if (result[0].DistributionInserted) {
         // Gửi phản hồi thành công
        ws.send(JSON.stringify({
          action: 'saveDistributionData',
          status: 'success',
          message: 'Distribution data saved and sent to Modbus successfully'
        }));
    } else {
        console.log("❌ Error: Entry was not inserted.");
        ws.send(JSON.stringify({
          action: 'saveDistributionData',
          status: 'error',
          message: `Failed to save distribution data`
        }));
    }
  } catch (error) {
    console.error('Error saving distribution data:', error);
    ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: `Failed to save distribution data: ${error.message}`
    }));
  }
}
// Xử lý yêu cầu lưu trữ dữ liệu phân phối vào Modbus
async function handleSaveDistributionDatas(ws, request) {
  const { data } = request;
  const { IpAddress } = data;

  if (!data || !IpAddress) {
    return ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: 'Missing required data or IpAddress'
    }));
  }

  try {
    // Bỏ qua việc kiểm tra kết nối thiết bị
    console.log(`Proceeding with saving data for device at ${IpAddress}...`);

    // Lưu dữ liệu vào cơ sở dữ liệu
    const result = await saveDistributionDataToDB(dataList);
    console.log("Final Status:", result);
    // Gửi phản hồi thành công
    ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'success',
      message: 'Distribution data saved and sent to Modbus successfully'
    }));
  } catch (error) {
    console.error('Error saving distribution data:', error);
    ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: `Failed to save distribution data: ${error.message}`
    }));
  }
}

// Xử lý yêu cầu lấy thông tin plants
async function handleGetPlant(ws) {
  try {
    const plantsResponse = await getPlantNames();
    ws.send(JSON.stringify({ action: 'getPlant', plants: plantsResponse }));
  } catch (error) {
    console.error('Error getting plant names:', error);
    ws.send(JSON.stringify({ error: 'Failed to retrieve plant names' }));
  }
}

// Xử lý yêu cầu lấy thông tin users
async function handleGetUsers(ws) {
  try {
    const usersResponse = await getUserList();
    ws.send(JSON.stringify({ action: 'getUsers', users: usersResponse }));
  } catch (error) {
    console.error('Error getting users:', error);
    ws.send(JSON.stringify({ error: 'Failed to retrieve users' }));
  }
}

// Xử lý yêu cầu lấy thông tin users
async function handleGetOperators(ws, request) {
  const { departmentID } = request;
  if (!departmentID) {
    return ws.send(JSON.stringify({ action: 'getOperators', status: 'error', message: 'Missing departmentID parameter' }));
  }

  try {
    const usersResponse = await getOperatorList(departmentID);
    ws.send(JSON.stringify({ action: 'getOperators', users: usersResponse }));
  } catch (error) {
    console.error('Error getting users:', error);
    ws.send(JSON.stringify({ error: 'Failed to retrieve operators' }));
  }
}
// Xử lý yêu cầu lấy thông tin operator from HMI
async function handleGetOperatorDistribution(ws, request) {
  const { IpAddress } = request;
  if (!IpAddress) {
    return ws.send(JSON.stringify({ action: 'getOperatorDistribution', status: 'error', message: 'Missing IpAddress parameter' }));
  }
  try {
    const operatorID = modbusClients[IpAddress]?.operatorID;
    console.log(`getOperatorID on ipAddress:${IpAddress} ${operatorID}`);
    const usersResponse = await getOperatorDistribution(operatorID);
    ws.send(JSON.stringify({ action: 'getOperatorDistribution', employee: usersResponse }));
  } catch (error) {
    console.error('Error getting getOperatorDistribution:', error);
    ws.send(JSON.stringify({ error: 'Failed to retrieve operatorID' }));
  }
}
// Hàm gửi thông báo cho tất cả client
function notifyClients(devicesResponse) {
  clients.forEach(client => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(JSON.stringify({ action: 'updateDevices', deviceStatuses: devicesResponse }));
    }
  });
}

module.exports = {
  setupWebSocket,
};
