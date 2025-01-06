// routes/getPlants.js
const { getPlantNames } = require('../database');

module.exports = async (req, res) => {
  try {
    const plants = await getPlantNames();
    res.json(plants);
  } catch (error) {
    console.error('Error fetching plant names:', error);
    res.status(500).send('Internal Server Error');
  }
};
