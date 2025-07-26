const EventEmitter = require('events');

class ModbusPollingManager extends EventEmitter {
  constructor(interval = 1000) {
    super();
    this.interval = interval;
    this.clients = {};        // { ipAddress: { client, socket, ... } }
    this.timers = {};         // Lưu timer của từng IP
  }

  /**
   * Đăng ký một client modbus
   * @param {string} ipAddress 
   * @param {object} clientEntry { client, socket, isConnected, sizeDataInfo }
   */
  registerClient(ipAddress, clientEntry) {
    this.clients[ipAddress] = { ...clientEntry, isProcessing: false, isConnecting: false };
    this.startPolling(ipAddress);
  }

  /**
   * Bắt đầu polling cho một IP
   * @param {string} ipAddress 
   */
  startPolling(ipAddress) {
    if (this.timers[ipAddress]) return; // tránh start trùng
  
    this.timers[ipAddress] = setInterval(async () => {
      const entry = this.clients[ipAddress];
      if (!entry) {
       // console.warn(`[${ipAddress}] No entry found in clients.`);
        return;
      }
  
      if (entry.isProcessing) return;
      entry.isProcessing = true;
  
      try {
        const { client, socket, isConnected } = entry;
  
        // Nếu client chưa khởi tạo hoặc chưa kết nối
        if (!client || !isConnected) {
          console.warn(`[${ipAddress}] Modbus client not connected — attempting initial connect...`);
          await this.safeConnect(ipAddress);
          return;
        }
  
        // Kiểm tra socket
        if (!socket || socket.destroyed || !socket.writable) {
          console.warn(`[${ipAddress}] Socket is closed or invalid — reconnecting...`);
          delete this.clients[ipAddress];
          await this.safeConnect(ipAddress);
          return;
        }
  
        // Gửi sự kiện đọc/ghi để bên ngoài xử lý
        this.emit('poll', client, ipAddress);
  
        // Đọc actual nếu có sizeDataInfo hợp lệ
        const sizeInfo = entry.sizeDataInfo;
        if (
          sizeInfo &&
          typeof sizeInfo === 'object' &&
          !Array.isArray(sizeInfo) &&
          Object.keys(sizeInfo).length > 0
        ) {
          this.emit('readActual', client, ipAddress);
        }
      } catch (err) {
        console.error(`[${ipAddress}] Error in polling loop: ${err.message}`);
      } finally {
        entry.isProcessing = false;
      }
    }, this.interval);
  }
  
  /**
   * Dừng polling cho 1 IP
   */
  stopPolling(ipAddress) {
    if (this.timers[ipAddress]) {
      clearInterval(this.timers[ipAddress]);
      delete this.timers[ipAddress];
    }
  }

  /**
   * Dừng toàn bộ
   */
  stopAll() {
    for (let ip in this.timers) {
      clearInterval(this.timers[ip]);
    }
    this.timers = {};
  }

  /**
   * Safe connect logic
   */
  async safeConnect(ipAddress) {
    const entry = this.clients[ipAddress];
    if (!entry || entry.isConnecting) return;
    entry.isConnecting = true;
    try {
      this.emit('reconnect', ipAddress);
    } finally {
      entry.isConnecting = false;
    }
  }
}

module.exports = ModbusPollingManager;
