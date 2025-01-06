const WebSocket = require('ws');
const { connectToDevice } = require('./modbusClient');
const { getDeviceList, updateDeviceConnectionStatus, getActualOutputData, getAllDeviceData, getDistributionByDevice, getPlantNames, addDeviceToList, getProductionSchedule, getUniquePages, saveDistributionDataToDB } = require('./database');
const { setClients } = require('./notifications'); 

let clients = [];
const modbusClients = {};

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
    console.log('Client connected');
    clients.push(ws);
    setClients(clients); // Cập nhật danh sách client khi có kết nối mới

    // Xử lý thông điệp từ client
    ws.on('message', (message) => {
      console.log(`Received message: ${message}`);
      handleClientMessage(ws, message);
    });

    // Khi client ngắt kết nối
    ws.on('close', () => {
      console.log('Client disconnected');
      clients = clients.filter(client => client !== ws); // Loại bỏ client khỏi danh sách
      setClients(clients); // Cập nhật danh sách client khi có kết nối bị ngắt
    });
  });
}

// Hàm xử lý yêu cầu từ client
async function handleClientMessage(ws, message) {
  try {
    const request = JSON.parse(message);
    const { action } = request;

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
      default:
        console.log('Unknown action:', action);
        ws.send(JSON.stringify({ error: 'Unknown action' }));
        break;
    }
  } catch (error) {
    console.error('Error processing message:', error);
    ws.send(JSON.stringify({ error: 'Invalid message format' }));
  }
}
// Hàm xử lý yêu cầu lấy dữ liệu sản lượng thực tế
async function handleGetActualData(ws, request) {
  const { orderId, masterWorkOrder } = request;

  let intervalId;

  try {
    // Function to fetch and send real-time data
    const sendRealTimeData = async () => {
      try {
        const realTimeData = await getActualOutputData(orderId, masterWorkOrder);

        if (realTimeData === null) {
          ws.send(JSON.stringify({
            action: 'getActualData',
            status: 'error',
            message: 'No real-time data found'
          }));
        } else {
          ws.send(JSON.stringify({
            action: 'getActualData',
            status: 'success',
            realTime: realTimeData
          }));
        }
      } catch (error) {
        console.error('Error fetching real-time data:', error);
        ws.send(JSON.stringify({
          action: 'getActualData',
          status: 'error',
          message: `Failed to get real-time data: ${error.message}`
        }));
      }
    };

    intervalId = setInterval(sendRealTimeData, 2000);

  } catch (error) {
    console.error('Error handling the real-time data request:', error);
    ws.send(JSON.stringify({
      action: 'getActualData',
      status: 'error',
      message: `Failed to handle request: ${error.message}`
    }));
  }

  ws.on('close', () => {
    clearInterval(intervalId); 
    console.log('Client disconnected. Stopping data requests.');
  });
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

    // Thông báo cho tất cả các client với danh sách thiết bị mới
    notifyClients(devicesResponse);

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

    await connectToDevice(ipAddress);
    await updateDeviceConnectionStatus(ipAddress, true);
    ws.send(JSON.stringify({
      action: 'connectDevice',
      ipAddress,
      status: 'connected'
    }));

    // Cập nhật danh sách thiết bị cho tất cả client
    notifyClients(await getAllDeviceData());
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
  const { masterWorkOrder, page } = request;

  if (!masterWorkOrder) {
    return ws.send(JSON.stringify({ action: 'getSchedule', status: 'error', message: 'Missing masterWorkOrder parameter' }));
  }

  try {
    const schedule = await getProductionSchedule(masterWorkOrder, page);
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

// Xử lý yêu cầu lưu trữ dữ liệu phân phối vào Modbus
async function handleSaveDistributionData(ws, request) {
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
    await saveDistributionDataToDB(data);

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
