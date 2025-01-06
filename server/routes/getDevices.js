const { getDeviceList } = require('../database');

module.exports = async (req, res) => {
  try {
    const devices = await getDeviceList();
    res.json(devices);
  } catch (error) {
    console.error('Error fetching devices:', error);
    res.status(500).send('Internal Server Error');
  }
};
