const { writeDataToDevice } = require('../modbusClient');

module.exports = async (req, res) => {
  const ip = req.params.ip;
  const value = req.body.value; // Giá trị cần ghi
  try {
    await writeDataToDevice(ip, value);
    res.send('Data written successfully');
  } catch (error) {
    console.error(`Error writing data to ${ip}:`, error);
    res.status(500).send('Internal Server Error');
  }
};
