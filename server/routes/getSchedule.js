const { getProductionSchedule } = require('../database');

module.exports = async (req, res) => {
  try {
    const masterWorkOrder = req.query.masterWorkOrder;

    if (!masterWorkOrder) {
      return res.status(400).send('Missing masterWorkOrder parameter');
    }

    const schedule = await getProductionSchedule(masterWorkOrder);

    res.json(schedule);
  } catch (error) {
    console.error('Error fetching production schedule:', error);
    res.status(500).send('Internal Server Error');
  }
};
