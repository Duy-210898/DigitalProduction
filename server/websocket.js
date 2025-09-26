const WebSocket = require('ws');
const { connectToDevice, isHostReachable, modbusClients } = require('./modbusClient');
const { getDeviceList , updateDeviceConnectionStatus, getActualOutputData, getAllDeviceData, getDistributionByDevice, getPlantNames, addDeviceToList, getProductionSchedule, getUniquePages, saveDistributionDataToDB, getUserList, getOperatorList, getAllProductionSchedule, getDistributions, getOperatorDistribution, getListOfSOsByYear} = require('./database');
const { setClients } = require('./notifications'); 
const { Time } = require('mssql');
const cron = require("node-cron");
const ping = require("ping");
const nodemailer = require("nodemailer");
const net = require("net");
require("dotenv").config();
const knownCuttingDevices = new Set();
let clients = [];


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
      //  console.log(`[${time}] Received message: ${message}`);
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
   // console.log(`Action: ${action}`);

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
        case 'getListOfSOsByYear':
          await handleGetListOfSOsByYear(ws, request);
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
async function handleGetActualData(ws, request, retryCount = 0) {
  try {
    const { filter } = request;

    // // Default page and pageSize
    // const page = filter?.page ?? 1;
    // const pageSize = filter?.pageSize ?? 100;

    const currentDate = new Date(); // Local time (assumed +07:00)

    // Format YYYY-MM-DD
    const formattedCurrentDate = currentDate.toISOString().split('T')[0];
    
    // Parse input or fallback to today
    const startDate = filter?.startDate ? new Date(filter.startDate) : new Date(formattedCurrentDate);
    const endDate = filter?.endDate ? new Date(filter.endDate) : new Date(formattedCurrentDate);
    
    // Normalize endDate to end of day local time
    endDate.setHours(23, 59, 59, 999);
    
    // Convert to UTC string for SQL if needed (optional)
    const sqlStartDate = startDate.toISOString();  // e.g., 2025-04-19T00:00:00.000Z
    const sqlEndDate = endDate.toISOString();      // e.g., 2025-04-19T16:59:59.999Z (for +07:00)
    
    // Send to SQL query as parameters @startDate and @endDate
    
    // Fetch real-time paginated data
    const realTimeData = await getActualOutputData(sqlStartDate, sqlEndDate);

    if (!realTimeData || realTimeData.Data.length === 0) {
      return ws.send(JSON.stringify({
        action: 'getActualData',
        status: 'error',
        message: 'No real-time data found for given filters'
      }));
    }

    // Send the result back
    ws.send(JSON.stringify({
      action: 'getActualData',
      status: 'success',
      page: realTimeData.Page,
      pageSize: realTimeData.PageSize,
      TotalCount: realTimeData.TotalCount,
      totalPages: realTimeData.TotalPages,
      data: realTimeData.Data
    }));

    // 🛑 Prevent stacking multiple listeners
    ws.removeAllListeners('close');
    ws.on('close', () => {
      console.log('Client disconnected.');
    });

  } catch (error) {
    console.error('Error in handleGetActualData:', error.message);

    if (error.message.includes('timeout') && retryCount < 3) {
      console.log(`Retrying (${retryCount + 1}/3)...`);
      await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1s
      return handleGetActualData(ws, request, retryCount + 1);
    }

    // Send error message to client
    ws.send(JSON.stringify({
      action: 'getActualData',
      status: 'error',
      message: 'Failed to fetch data. Please try again later.'
    }));
  }
}


// Xử lý yêu cầu lấy thông tin phân phối của thiết bị
async function handleGetDistributions(ws, request) {
  const { filter } = request;
  try {
    // Get the current date in YYYY-MM-DD format
    const currentDate = new Date();
    const formattedCurrentDate = currentDate.toISOString().split('T')[0]; // YYYY-MM-DD
    
    // Ensure startDate and endDate are Date objects
    const startDate = filter?.startDate ? new Date(filter.startDate) : new Date(formattedCurrentDate);
    let endDate = filter?.endDate ? new Date(filter.endDate) : new Date(formattedCurrentDate);
    
    // Set endDate to 23:59:59.999 in local time
    endDate.setHours(23, 59, 59, 999);
    // Add 1 day to the endDate
    endDate.setDate(endDate.getDate());
    
    // Format dates for SQL (YYYY-MM-DD HH:mm:ss.SSS) - LOCAL TIME
    const formatDateForSQL = (date) => {
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ` +
             `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}.${String(date.getMilliseconds()).padStart(3, '0')}`;
    };
    
    // Paging
    const pageNumber = parseInt(filter?.pageNumber || 1);
    const pageSize = parseInt(filter?.pageSize || 100);

    // console.log("Start Date:", formattedStartDate); // 2025-03-04 00:00:00.000
    // console.log("End Date:", formattedEndDate);     // 2025-03-04 23:59:59.999
    // Gọi hàm getDistributionByDevice từ database.js để lấy dữ liệu phân phối
    const { records, totalCount } = await getDistributions(startDate, endDate, pageNumber, pageSize);

    if (!records || records.length === 0) {
      return ws.send(JSON.stringify({
        action: 'getDistributions',
        status: 'success',
        distributionData: [],
        totalCount: 0
      }));
    }

    // Success response
    ws.send(JSON.stringify({
      action: 'getDistributions',
      status: 'success',
      distributionData: records,
      totalCount: totalCount
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
// async function handleGetSchedule(ws, request) {
//   try {
//     let schedule;
    
//     if (request.so) {
//       // Get schedule for a specific SO
//       schedule = await getProductionSchedule(request.so);
//     } else {
//       // Get all schedules if no SO is provided
//       const month = request.month ?? null;
//       const year = request.year ?? null;

//       // Pass month/year into the query function
//       schedule = await getAllProductionSchedule(month, year);
//     }

//     ws.send(JSON.stringify({ action: 'getSchedule', status: 'success', schedule }));
//   } catch (error) {
//     console.error('Error fetching production schedule:', error);
//     ws.send(JSON.stringify({ action: 'getSchedule', status: 'error', message: 'Failed to retrieve production schedule' }));
//   }
// }

async function handleGetListOfSOsByYear(ws, request) {
  try {
    const year = request.year ?? null;

    if (!year) {
      ws.send(JSON.stringify({
        action: 'getListOfSOsByYear',
        status: 'error',
        message: 'Month and year are required'
      }));
      return;
    }

    const soList = await getListOfSOsByYear(year);
    ws.send(JSON.stringify({
      action: 'getListOfSOsByYear',
      status: 'success',
      data: soList
    }));
  } catch (error) {
    console.error('Error in handleGetListOfSOsByYear:', error.message);
    ws.send(JSON.stringify({
      action: 'getListOfSOsByYear',
      status: 'error',
      message: 'Failed to retrieve SO list'
    }));
  }
}

async function handleGetSchedule(ws, request) {
  try {
    let schedule;

    if (request.so) {
      const includeDistributed = request.includeDistributed;
      const soList = Array.isArray(request.so) ? request.so : [request.so];
      schedule = await getProductionSchedule(soList, includeDistributed);
    } else {
      const month = request.month ?? null;
      const year = request.year ?? null;
      const includeDistributed = request.includeDistributed;
      schedule = await getAllProductionSchedule(month, year, includeDistributed);
    }

    ws.send(JSON.stringify({
      action: 'getSchedule',
      status: 'success',
      schedule
    }));
  } catch (error) {
    console.error('Error fetching production schedule:', error);
    ws.send(JSON.stringify({
      action: 'getSchedule',
      status: 'error',
      message: 'Failed to retrieve production schedule'
    }));
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

  // Check data object exists and has the required lists
  if (!data || !Array.isArray(data.Distributions) || data.Distributions.length === 0) {
    return ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: 'Missing or invalid Distributions list'
    }));
  }

  try {
    const results = await saveDistributionDataToDB(data);

    const successCount = results.filter(r => r.DistributionInserted).length;
    const duplicateCount = results.filter(r => r.DistributionDuplicate).length;
    const errorCount = results.filter(r => r.Error).length;

    const messages = [];

    if (successCount > 0)
      messages.push(`✅ ${successCount} distribution(s) saved successfully.`);

    if (duplicateCount > 0)
      messages.push(`⚠️ ${duplicateCount} duplicate(s) skipped.`);

    if (errorCount > 0)
      messages.push(`❌ ${errorCount} error(s) occurred.`);

    ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: errorCount > 0 ? 'partial' : 'success',
      message: messages.join(' ')
    }));

  } catch (error) {
    console.error("❌ Critical error saving distribution data:", error.message);
    ws.send(JSON.stringify({
      action: 'saveDistributionData',
      status: 'error',
      message: `Critical failure while saving data: ${error.message}`
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
  //  console.log(`Proceeding with saving data for device at ${IpAddress}...`);

    // Lưu dữ liệu vào cơ sở dữ liệu
    const result = await saveDistributionDataToDB(dataList);
  //  console.log("Final Status:", result);
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
  const departmentID = request.departmentID || 0; // Default to 0 = get all

  try {
    const usersResponse = await getOperatorList(departmentID);
    ws.send(JSON.stringify({ action: 'getOperators', users: usersResponse }));
  } catch (error) {
    console.error('Error getting users:', error);
    ws.send(JSON.stringify({ action: 'getOperators', status: 'error', message: 'Failed to retrieve operators' }));
  }
}

// Xử lý yêu cầu lấy thông tin operator from HMI
async function handleGetOperatorDistribution(ws, request) {
  const { IpAddress } = request;

  if (!IpAddress) {
    return ws.send(JSON.stringify({ 
      action: 'getOperatorDistribution', 
      status: 'error', 
      message: 'Missing IpAddress parameter' 
    }));
  }

  try {
    const operatorID = modbusClients[IpAddress]?.operatorID;

    if (!operatorID) {
      console.warn(`No operatorID found for IpAddress: ${IpAddress}`);
      return ws.send(JSON.stringify({ 
        action: 'getOperatorDistribution', 
        status: 'error', 
        message: 'No operatorID assigned to this IpAddress' 
      }));
    }

  //  console.log(`Fetching distribution data for operatorID: ${operatorID}`);
    const usersResponse = await getOperatorDistribution(operatorID);

    if (!usersResponse || usersResponse.length === 0) {
      console.warn(`No operator distribution data found for operatorID: ${operatorID}`);
      return ws.send(JSON.stringify({ 
        action: 'getOperatorDistribution', 
        status: 'error', 
        message: 'No distribution data found for this operator' 
      }));
    }

    ws.send(JSON.stringify({ 
      action: 'getOperatorDistribution', 
      status: 'success',
      employee: usersResponse 
    }));

  } catch (error) {
    console.error('Error retrieving operator distribution:', error);
    ws.send(JSON.stringify({
      action: 'getOperatorDistribution', 
      status: 'error', 
      message: 'Failed to retrieve operator distribution data' 
    }));
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
//✅ 4. Schedule cronjob every 1 minute
// setInterval(calculateActiveHours, 60 * 1000);


// ✅ Setup mail transporter (use ENV in production)
const transporter = nodemailer.createTransport({
  host: "smtp.sendgrid.net",
  port: 587,           // 465 for SSL
  secure: false,       // true for port 465
  auth: {
    user: process.env.SENDGRID_USER, // "apikey"
    pass: process.env.SENDGRID_PASS  // API key thực tế
  },
  tls: {
    rejectUnauthorized: false // allow self-signed certificate
  }
});

// 📧 Send mail alert
async function sendMail(ip, machineName , port) {
  const mailOptions = {
    from: "ducnhat171998@gmail.com",
    to: ["ducnhat1708@gmail.com", "Trung-Pham@vn.apachefootwear.com"],
    //"Trung-Pham@vn.apachefootwear.com"
    subject: `Device DOWN: ${ip}:${port}`,
    text: `Device at ${ip}:${port} and Machine Name: ${machineName} is unreachable (Change IP address or Socket off).`,
  };
  try {
    await transporter.sendMail(mailOptions);
    console.log(`📧 Mail sent for ${ip}:${port}`);
  } catch (err) {
    console.error("❌ Mail error:", err.message);
  }
}

// 🔍 Check one device
async function checkDevice(ip, port, matchedDevice, ) {
  const pingResult = await ping.promise.probe(ip, { timeout: 2 });
  const isPingAlive = pingResult.alive;

  let isSocketAlive = false;
  if (isPingAlive) {
    isSocketAlive = await new Promise((resolve) => {
      const socket = new net.Socket();
      socket.setTimeout(2000);
      socket
        .connect(port, ip, () => {
         // socket.destroy();
          resolve(true);
        })
        .on("error", () => {
          //socket.destroy();
          resolve(false);
        })
        .on("timeout", () => {
         // socket.destroy();
          resolve(false);
        });
    });
  }
  //if(ip !== '10.30.4.144') return;
  if (!isSocketAlive || !matchedDevice) {
    await sendMail(ip, machineName, port);
    await updateDeviceConnectionStatus(ip, false);
  } else if (isPingAlive && !isSocketAlive) {
    //console.log(`⚠️ Device ${ip}:${port} ping OK but socket OFF → no mail`);
    await updateDeviceConnectionStatus(ip, false);
  } else {
  //  console.log(`✅ Device ${ip}:${port} OK`);
    await updateDeviceConnectionStatus(ip, true);
  }
}

// 🔁 Cron job to check devices every 30 minute
function startCheckConnectCronJob() 
{ 
  cron.schedule("*/30 * * * *", async () => { 
  // console.log("⏰ Running device check:", new Date().toLocaleString()); 
  try { 
    const devices = await getAllDeviceData(); 
    for (const device of devices) { 
      const ip = device.IpAddress || device.ipAddress; 
      const port = device.Port || 502; 
      // default Modbus 
      const entry = modbusClients[ip];
      if(entry === undefined || entry.isConnected == false) continue;
      let ipAddressResponse = await entry.client.readHoldingRegisters(8009, 4); 
      const values = ipAddressResponse.response._body.values;
      // array of 4 numbers 
      const hmiIp = values.join(".");
     // console.log(`📡 HMI reports its IP as: ${hmiIp}`);
      // check if HMI IP exists in devices
      const matchedDevice = devices.find(d => {
        const deviceIp = d.IpAddress || d.ipAddress; 
        return deviceIp === hmiIp;
      }); 
      if (ip) await checkDevice(ip, port, matchedDevice, device.machineName); 
    } 
  } 
  catch (err) {
     console.error("❌ Cron job error:", err.message); 
    } 
  }); 
}


module.exports = {
  setupWebSocket,
  startCheckConnectCronJob
};
