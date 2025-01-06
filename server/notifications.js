const WebSocket = require('ws');

let clients = [];

// Hàm gửi thông báo tới tất cả client khi yêu cầu xóa đơn
function notifyClientsToDeleteOrder(message) {
  return new Promise((resolve, reject) => {
    let responded = false;

    // Thiết lập sự kiện để xử lý phản hồi từ client
    clients.forEach(client => {
      if (client.readyState === WebSocket.OPEN) {
        const notification = JSON.stringify({
          action: 'delete_order',
          message: message
        });

        client.send(notification); // Gửi thông báo đến client

        // Lắng nghe phản hồi từ client
        client.on('message', (response) => {
          try {
            const data = JSON.parse(response);
            if (data.action === 'confirm_delete_order') {
              responded = true;
              resolve(data.userChoice); // Trả về userChoice
            }
          } catch (err) {
            reject(new Error('Invalid response format.'));
          }
        });
      }
    });

    // Thiết lập timeout nếu không có phản hồi từ client trong một khoảng thời gian nhất định
    setTimeout(() => {
      if (!responded) {
        reject(new Error('No response from client.'));
      }
    }, 300000);
  });
}

// Hàm cập nhật danh sách client đang kết nối
function setClients(newClients) {
  clients = newClients;
}

module.exports = {
  notifyClientsToDeleteOrder,
  setClients,
};
