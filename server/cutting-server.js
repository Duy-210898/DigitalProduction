const express = require('express');
const WebSocket = require('ws');
const ping = require('ping');
const { initDatabase, getDeviceList } = require('./database');
const { setupWebSocket } = require('./websocket');
const schedule = require('node-schedule');
const { setIpAddresses, closeAllConnections, connectToDevice } = require('./modbusClient');

const PING_INTERVAL = 1000;
const app = express();
const port = 8000;

const fs = require('fs');
const path = require('path');
const perfLogPath = path.join(__dirname, 'performance-log.txt');

function logPerformance(message) {
  const timestamp = new Date().toISOString();
  fs.appendFileSync(perfLogPath, `[${timestamp}] ${message}\n`);
}

app.use(express.json());

initDatabase();

const server = app.listen(port, () => {
  console.log(`Server is running at http://localhost:${port}`);
  connectToDevicesImmediately();
});

setupWebSocket(server, app);

app.get('/devices', require('./routes/getDevices'));
app.get('/read/:ip', require('./routes/readData'));
app.post('/write/:ip', require('./routes/writeData'));
app.get('/plants', require('./routes/getPlants'));

async function connectToDevicesImmediately() {
  try {
    console.log('Starting immediate connection process...');
    const ipAddresses = await getDeviceList(); 
    console.log('IP Addresses:', ipAddresses);
    await handleDeviceConnection(ipAddresses);
    await setIpAddresses(ipAddresses);
  } catch (error) {
    console.error(`Error during immediate device connection: ${error.message}`);
  }
}

schedule.scheduleJob('30 07 * * *', async function () {
  console.log('Starting connection process at 07:30...');
  const ipAddresses = await getDeviceList();
  await handleDeviceConnection(ipAddresses);
});

schedule.scheduleJob('10 20 * * *', function () {
  closeAllConnections();
  console.log('All connections closed at 20:10.');
});

setInterval(async () => {
  try {
    const ipAddresses = await getDeviceList();
    setIpAddresses(ipAddresses);
  } catch (error) {
    console.error(`Error updating IP addresses: ${error.message}`);
  }
}, PING_INTERVAL);

process.on('SIGINT', closeAllConnections);
process.on('SIGTERM', closeAllConnections);

async function handleDeviceConnection(ipAddresses) {
  const startTotal = Date.now();
  const BATCH_SIZE = 20;

  logPerformance(`🚀 Starting batched connection for ${ipAddresses.length} machines...`);

  let successCount = 0;
  let failCount = 0;

  for (let i = 0; i < ipAddresses.length; i += BATCH_SIZE) {
    const batch = ipAddresses.slice(i, i + BATCH_SIZE);

    const batchStart = Date.now();

      await Promise.allSettled(
      batch.map(async ip => {
        const start = Date.now();
        try {
          await connectToDevice(ip);
          const duration = Date.now() - start;
          logPerformance(`✅ Connected to ${ip} in ${duration} ms`);
          successCount++;
        } catch (error) {
          const duration = Date.now() - start;
          logPerformance(`❌ Failed to connect ${ip} in ${duration} ms: ${error.message}`);
          failCount++;
        }
      })
    );

    const batchDuration = Date.now() - batchStart;
    logPerformance(`📦 Batch ${i / BATCH_SIZE + 1}: ${batch.length} devices connected in ${batchDuration} ms`);
  }

  const totalDuration = Date.now() - startTotal;
  logPerformance(`📊 Final Summary:`);
  logPerformance(`  🟢 Success: ${successCount}`);
  logPerformance(`  🔴 Failed: ${failCount}`);
  logPerformance(`  ⏱️ Total time for ${ipAddresses.length} machines: ${totalDuration} ms`);
  logPerformance(`------------------------------------------------------------`);
}


