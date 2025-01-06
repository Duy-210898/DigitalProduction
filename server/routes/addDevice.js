const { addDeviceToList } = require('../database');

module.exports = async (req, res) => {
  console.log(req.body);

  // Kiểm tra xem có trường device trong req.body không
  if (!req.body.device) {
    return res.status(400).send('Bad Request: Missing device object.');
  }

  const { IpAddress, MachineName, Plant } = req.body.device; 

  // Kiểm tra các trường bắt buộc
  if (!IpAddress || !MachineName || !Plant) {
    return res.status(400).send('Bad Request: Missing required fields.');
  }

  try {
    // Thêm thiết bị vào danh sách
    const result = await addDeviceToList({ ipAddress: IpAddress, machineName: MachineName, plant: Plant });

    // Kiểm tra phản hồi từ hàm thêm thiết bị
    if (result.status === 'success') {
      return res.status(201).send('Device added successfully.');
    } else {
      return res.status(400).send(`Failed to add device: ${result.message}`);
    }
  } catch (error) {
    console.error('Error adding device:', error);
    return res.status(500).send('Internal Server Error');
  }
};
