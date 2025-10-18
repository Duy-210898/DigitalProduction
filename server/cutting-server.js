const express = require('express');
const { initDatabase, getDeviceList } = require('./database');
const { setupWebSocket, startCheckConnectCronJob } = require('./websocket');
const schedule = require('node-schedule');
const { setIpAddresses, closeAllConnections, connectToDevice } = require('./modbusClient');
const fs = require('fs');
const path = require('path');

const app = express();
const port = 8000;
const PING_INTERVAL = 1000;

// === LOG PATHS ===
const perfLogPath = path.join(__dirname, 'performance-log.txt');
const successLogPath = path.join(__dirname, 'success_log.txt');
const errorLogPath = path.join(__dirname, 'error_log.txt');

// === LOG HELPERS ===
function logPerformance(message) {
  const timestamp = new Date().toISOString();
  fs.appendFileSync(perfLogPath, `[${timestamp}] ${message}\n`);
}

function logSuccess(message) {
  const timestamp = new Date().toISOString();
  fs.appendFileSync(successLogPath, `[${timestamp}] ${message}\n`);
}

function logError(message) {
  const timestamp = new Date().toISOString();
  fs.appendFileSync(errorLogPath, `[${timestamp}] ${message}\n`);
}

// === DAILY LOG ARCHIVE & CLEAR ===
function archiveAndClearLogs() {
  const date = new Date().toISOString().split('T')[0]; // YYYY-MM-DD
  const logDir = path.join(__dirname, 'logs');

  try {
    if (!fs.existsSync(logDir)) fs.mkdirSync(logDir);

    const logs = [
      { src: perfLogPath, dest: path.join(logDir, `performance-log-${date}.txt`) },
      { src: successLogPath, dest: path.join(logDir, `success_log-${date}.txt`) },
      { src: errorLogPath, dest: path.join(logDir, `error_log-${date}.txt`) },
    ];

    logs.forEach(({ src, dest }) => {
      if (fs.existsSync(src)) {
        fs.copyFileSync(src, dest);
        fs.writeFileSync(src, '', 'utf-8'); // clear after archive
      }
    });

    console.log(`[${new Date().toISOString()}] ✅ Logs archived and cleared.`);
  } catch (err) {
    console.error(`[${new Date().toISOString()}] ❌ Failed to archive logs:`, err);
  }
}

// 🕒 Schedule at 00:00 every day
schedule.scheduleJob('0 0 * * *', archiveAndClearLogs);

// === EXPRESS + DATABASE ===
app.use(express.json());
initDatabase();

const server = app.listen(port, () => {
  console.log(`Server running at http://localhost:${port}`);
  connectToDevicesImmediately();
});

setupWebSocket(server, app);
startCheckConnectCronJob();

// === ROUTES ===
app.get('/devices', require('./routes/getDevices'));
app.get('/read/:ip', require('./routes/readData'));
app.post('/write/:ip', require('./routes/writeData'));
app.get('/plants', require('./routes/getPlants'));

// === INITIAL CONNECTION ===
async function connectToDevicesImmediately() {
  try {
    console.log('Starting immediate connection process...');
    const ipAddresses = await getDeviceList(); 
    console.log('IP Addresses:', ipAddresses);
    await handleDeviceConnection(ipAddresses);
    await setIpAddresses(ipAddresses);
    logSuccess('✅ Devices connected successfully on startup.');
  } catch (error) {
    console.error(`Error during immediate device connection: ${error.message}`);
    logError(`❌ Immediate connection failed: ${error.message}`);
  }
}

// === SCHEDULED CONNECTIONS ===
schedule.scheduleJob('30 07 * * *', async function () {
  console.log('Starting connection process at 07:30...');
  const ipAddresses = await getDeviceList();
  await handleDeviceConnection(ipAddresses);
});

schedule.scheduleJob('10 20 * * *', function () {
  closeAllConnections();
  console.log('All connections closed at 20:10.');
  logSuccess('🔌 All connections closed at 20:10.');
});

// === UPDATE IPs PERIODICALLY ===
setInterval(async () => {
  try {
    const ipAddresses = await getDeviceList();
    setIpAddresses(ipAddresses);
  } catch (error) {
    console.error(`Error updating IP addresses: ${error.message}`);
    logError(`Error updating IP addresses: ${error.message}`);
  }
}, PING_INTERVAL);

// === GRACEFUL SHUTDOWN ===
process.on('SIGINT', closeAllConnections);
process.on('SIGTERM', closeAllConnections);

// === DEVICE CONNECTION HANDLER ===
async function handleDeviceConnection(ipAddresses) {
  const startTotal = Date.now();
  const MAX_CONCURRENT = 100;
  let successCount = 0;
  let failCount = 0;

  logPerformance(`🚀 Starting fast connection for ${ipAddresses.length} machines...`);

  for (let i = 0; i < ipAddresses.length; i += MAX_CONCURRENT) {
    const chunk = ipAddresses.slice(i, i + MAX_CONCURRENT);
    const chunkStart = Date.now();

    const settled = await Promise.allSettled(
      chunk.map(async (ip) => {
        const start = Date.now();
        try {
          await connectToDevice(ip, { timeout: 500 });
          const duration = Date.now() - start;
          logPerformance(`✅ Connected to ${ip} in ${duration} ms`);
          successCount++;
        } catch (err) {
          const duration = Date.now() - start;
          logPerformance(`❌ Failed ${ip} in ${duration} ms: ${err.message}`);
          failCount++;
        }
      })
    );

    const chunkDuration = Date.now() - chunkStart;
    logPerformance(`📦 Chunk ${i / MAX_CONCURRENT + 1}: ${chunk.length} devices in ${chunkDuration} ms`);
  }

  const totalDuration = Date.now() - startTotal;
  logPerformance(`📊 Final Summary:`);
  logPerformance(`  🟢 Success: ${successCount}`);
  logPerformance(`  🔴 Failed: ${failCount}`);
  logPerformance(`  ⏱️ Total: ${totalDuration} ms`);
  logPerformance(`------------------------------------------------------------`);
}
