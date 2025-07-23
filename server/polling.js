const { parentPort, workerData } = require('worker_threads');
const fs = require('fs');
const path = require('path');
const { connectToDevice, checkAndSaveDistribution, readActualData, timedRead, writeToModbusRegister } = require('./modbusClient');

const ipList = workerData.ipList || [];
const POLL_INTERVAL = workerData.interval || 1000;
const perfLogPath = path.join(__dirname, 'performance-log.txt');
const modbusClients = {};

// Utility: log performance
function logPerformance(message) {
  const timestamp = new Date().toISOString();
  fs.appendFileSync(perfLogPath, `[${timestamp}] ${message}\n`);
}

async function pollDevice(ip) {
  const start = Date.now();
  try {
    // Ensure we have a connected client
    if (!modbusClients[ip] || !modbusClients[ip].client || !modbusClients[ip].isConnected) {
      console.log(`🔄 Reconnecting to device ${ip}`);
      const client = await connectToDevice(ip); // No need to pass modbusClients
      modbusClients[ip] = { client, totalReadTime: 0, isConnected: true };
    }

    // Perform timed reads
    await timedRead(ip, 0, 10, modbusClients);
    await timedRead(ip, 10, 20, modbusClients);

    const entry = modbusClients[ip];
    if (entry?.client) {
      await checkAndSaveDistribution(entry.client, ip, modbusClients);
      if (entry.sizeDataInfo && Object.keys(entry.sizeDataInfo).length > 0) {
        await readActualData(entry.client, ip, modbusClients);
      }
    }

    // Optionally write values back to Modbus
    await writeToModbusRegister(ip, modbusClients);

    const duration = Date.now() - start;
    parentPort.postMessage({ type: 'perf', ip, readTime: duration });

    return { ip, status: 'ok', readTime: duration };
  } catch (err) {
    console.error(`❌ Polling error for ${ip}: ${err.message}`);
    modbusClients[ip] = null; // Reset client to force reconnection
    return { ip, status: 'error', message: err.message };
  }
}

async function startPolling() {
  while (true) {
    const startTime = Date.now();
    const results = await Promise.all(ipList.map(ip => pollDevice(ip)));
    const duration = Date.now() - startTime;

    parentPort.postMessage({ type: 'batchResult', results, duration });
    logPerformance(`📦 Batch read: ${ipList.length} devices in ${duration} ms`);

    await new Promise(resolve => setTimeout(resolve, POLL_INTERVAL));
  }
}

startPolling();

module.exports = { modbusClients };
