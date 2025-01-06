const express = require('express');
const WebSocket = require('ws');
const ping = require('ping');
const { initDatabase, getDeviceList } = require('./database');
const { setupWebSocket } = require('./websocket');
const schedule = require('node-schedule');
const { setIpAddresses, closeAllConnections, connectToDevice } = require('./modbusClient');

const PING_INTERVAL = 10000;
const app = express();
const port = 8000;

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
  for (const ipAddress of ipAddresses) {
    try {
      await connectToDevice(ipAddress);
    } catch (error) {
      console.error(`Error connecting to device at ${ipAddress}: ${error.message}`);
    }
  }
}

