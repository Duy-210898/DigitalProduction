const { readDataFromDevice } = require('../modbusClient');

module.exports = async (req, res) => {
  const ip = req.params.ip;
  try {
    const data = await readDataFromDevice(ip);
    res.json(data);
  } catch (error) {
    console.error(`Error reading data from ${ip}:`, error);
    res.status(500).send('Internal Server Error');
  }
};
