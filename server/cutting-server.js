const express = require('express');
const { initDatabase, getDeviceList } = require('./database');
const { setupWebSocket , startCheckConnectCronJob } = require('./websocket');
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
startCheckConnectCronJob();

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

// (async () => {
//   try {
//     const ipAddresses = await getDeviceList();
//     await setIpAddresses(ipAddresses);
//   } catch (error) {
//     console.error(`Error updating IP addresses: ${error.message}`);
//   }
// })();
process.on('SIGINT', closeAllConnections);
process.on('SIGTERM', closeAllConnections);

async function handleDeviceConnection(ipAddresses) {
  const startTotal = Date.now();
  const MAX_CONCURRENT = 100; // bump this up for more parallelism
  const results = [];
  let successCount = 0;
  let failCount = 0;

  logPerformance(`🚀 Starting fast connection for ${ipAddresses.length} machines...`);

  // Break into chunks of MAX_CONCURRENT
  for (let i = 0; i < ipAddresses.length; i += MAX_CONCURRENT) {
    const chunk = ipAddresses.slice(i, i + MAX_CONCURRENT);

    const chunkStart = Date.now();

    // Run all connections in parallel
    const settled = await Promise.allSettled(
      chunk.map(async (ip) => {
        const start = Date.now();
        try {
          await connectToDevice(ip, { timeout: 500 }); // ⬅️ set lower timeout
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

    results.push(...settled);
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

