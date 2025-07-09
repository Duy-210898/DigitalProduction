// const express = require('express');
// const WebSocket = require('ws');
// const ping = require('ping');
// const { initDatabase, getDeviceList } = require('./database');
// const { setupWebSocket } = require('./websocket');
// const schedule = require('node-schedule');
// const { setIpAddresses, closeAllConnections, connectToDevice } = require('./modbusClient');

// const PING_INTERVAL = 10000;
// const app = express();
// const port = 8000;

// app.use(express.json());

// initDatabase();

// const server = app.listen(port, () => {
//   console.log(`Server is running at http://localhost:${port}`);
//   connectToDevicesImmediately();
// });

// setupWebSocket(server, app);

// app.get('/devices', require('./routes/getDevices'));
// app.get('/read/:ip', require('./routes/readData'));
// app.post('/write/:ip', require('./routes/writeData'));
// app.get('/plants', require('./routes/getPlants'));

// async function connectToDevicesImmediately() {
//   try {
//     console.log('Starting immediate connection process...');
//     const ipAddresses = await getDeviceList(); 
//     console.log('IP Addresses:', ipAddresses);
//     await setIpAddresses(ipAddresses);
//   } catch (error) {
//     console.error(`Error during immediate device connection: ${error.message}`);
//   }
// }

// schedule.scheduleJob('30 07 * * *', async function () {
//   console.log('Starting connection process at 07:30...');
//   const ipAddresses = await getDeviceList();
//   await handleDeviceConnection(ipAddresses);
// });

// schedule.scheduleJob('10 20 * * *', function () {
//   closeAllConnections();
//   console.log('All connections closed at 20:10.');
// });

// setInterval(async () => {
//   try {
//     const ipAddresses = await getDeviceList();
//     setIpAddresses(ipAddresses);
//   } catch (error) {
//     console.error(`Error updating IP addresses: ${error.message}`);
//   }
// }, PING_INTERVAL);

// process.on('SIGINT', closeAllConnections);
// process.on('SIGTERM', closeAllConnections);

// async function handleDeviceConnection(ipAddresses) {
//   for (const ipAddress of ipAddresses) {
//     try {
//       await connectToDevice(ipAddress);
//     } catch (error) {
//       console.error(`Error connecting to device at ${ipAddress}: ${error.message}`);
//     }
//   }
// }


const WebSocket = require('ws');

const TOTAL_CLIENTS = 170;
const SERVER_URL = 'ws://localhost:8000';
const clients = new Map();

function createMockPayload(id) {
  return {
    hmiId: id,
    status: 'OK',
    temperature: Math.floor(Math.random() * 50) + 20,
    timestamp: new Date().toISOString()
  };
}

function startClient(id, retryDelay = 1000) {
  let ws;
  let sendInterval;

  const connect = () => {
    ws = new WebSocket(SERVER_URL);

    ws.on('open', () => {
      console.log(`✅ HMI-${id} connected`);

      sendInterval = setInterval(() => {
        if (ws.readyState === WebSocket.OPEN) {
          try {
            ws.send(JSON.stringify(createMockPayload(id)));
          } catch (e) {
            console.error(`🚫 HMI-${id} send error: ${e.message}`);
          }
        }
      }, 1000);
    });

    ws.on('message', (msg) => {
      console.log(`📩 HMI-${id} received: ${msg}`);
    });

    ws.on('close', () => {
      console.log(`🔌 HMI-${id} disconnected`);
      clearInterval(sendInterval);
      // Reconnect with exponential backoff (max 10s)
      const nextDelay = Math.min(retryDelay * 2, 10000);
      setTimeout(() => startClient(id, nextDelay), nextDelay);
    });

    ws.on('error', (err) => {
      console.error(`❌ HMI-${id} error: ${err.message}`);
    });

    clients.set(id, ws);
  };

  connect();
}

async function startAllClients() {
  for (let i = 1; i <= TOTAL_CLIENTS; i++) {
    startClient(i);
    await new Promise((res) => setTimeout(res, 100)); // slower startup
  }
}

startAllClients();

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('🛑 Shutting down clients...');
  clients.forEach((ws, id) => {
    if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
      ws.close();
    }
  });
  process.exit();
});
