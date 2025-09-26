const EventEmitter = require('events');

class ModbusPollingManager extends EventEmitter {
  constructor(interval = 200, maxConcurrent = 50) {
    super();
    this.interval = interval;
    this.maxConcurrent = maxConcurrent;
    this.clients = {};   // { ip: { client, socket, isConnected, hasSizeData, ... } }
    this.queue = new Set(); // dùng Set để tránh trùng IP
    this.activeCount = 0;
    this.timer = null;

    // Dynamic tuning
    this.minConcurrent = 10;
    this.maxLimit = 200;
    this.lastAdjust = Date.now();

    // // Timer để auto check connection
    // this.checkTimer = setInterval(() => {
    //   this.autoCheckConnections();
    //   }, 1000);
  }

  registerClient(ipAddress, clientEntry) {
    this.clients[ipAddress] = {
      ...clientEntry,
      isProcessing: false,
      isConnecting: false,
      taskLocks: {},
      hasSizeData: !!(clientEntry.sizeDataInfo && Object.keys(clientEntry.sizeDataInfo).length > 0)
    };

    this.queue.add(ipAddress);

    if (!this.timer) {
      this.startScheduler();
    }
  }

  unregisterClient(ipAddress) {
    const entry = this.clients[ipAddress];
    if (entry?.socket) {
      try {
        entry.socket.removeAllListeners();
        entry.socket.destroy();
      } catch (_) {}
    }
    delete this.clients[ipAddress];
    this.queue.delete(ipAddress);
  }

  startScheduler() {
    const loop = () => {
      if (this.activeCount < this.maxConcurrent && this.queue.size > 0) {
        const ip = this.queue.values().next().value; // lấy IP đầu tiên trong Set
        this.queue.delete(ip);

        if (ip) {
          this.pollOne(ip).finally(() => {
            if (this.clients[ip]) { // chỉ add lại nếu client còn tồn tại
              this.queue.add(ip);
            }
          });
        }
      }

      // ⚡ auto-adjust concurrency every 5s
      if (Date.now() - this.lastAdjust > 5000) {
        this.adjustConcurrency();
        this.lastAdjust = Date.now();
      }

      this.timer = setTimeout(loop, this.interval);
    };
    this.timer = setTimeout(loop, this.interval);
  }

  stopScheduler() {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }

  async pollOne(ip) {
    const entry = this.clients[ip];
    if (!entry || entry.isProcessing) return;
    entry.isProcessing = true;
    this.activeCount++;

    try {
      const { socket, isConnected } = entry;
      if (!isConnected || !socket || socket.destroyed || !socket.writable) {
        await this.safeConnect(ip);
        // tự remove client nếu fail quá nhiều lần
        entry.failCount = (entry.failCount || 0) + 1;
        if (entry.failCount > 50) {
          console.warn(`[${ip}] Removed due to repeated failures.`);
          this.unregisterClient(ip);
          return;
        }
        return;
      }
      entry.failCount = 0;

      const tasks = [
        this.runWithLock(entry, 'checkConnection', () => this.emit('checkConnection', entry, ip)),
        //this.runWithLock(entry, 'writeRegister', () => this.emit('writeRegister', entry, ip)),
        this.runWithLock(entry, 'poll', () => this.emit('poll', entry, ip)),
        this.runWithLock(entry, 'readActual', () => this.emit('readActual', entry, ip))
      ];

      await Promise.all(tasks);
    } finally {
      entry.isProcessing = false;
      this.activeCount--;
    }
  }

  async runWithLock(entry, taskName, fn) {
    if (entry.taskLocks[taskName]) return;
    entry.taskLocks[taskName] = true;
    try {
      await fn();
    } finally {
      entry.taskLocks[taskName] = false;
    }
  }

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

  adjustConcurrency() {
    const memMB = process.memoryUsage().heapUsed / 1024 / 1024;
    const loadFactor = this.queue.size / (Object.keys(this.clients).length || 1);

    if (memMB < 1500 && loadFactor > 0.3) {
      // 🔼 safe to increase concurrency
      this.maxConcurrent = Math.min(this.maxConcurrent + 10, this.maxLimit);
    } else if (memMB > 2500) {
      // 🔽 reduce to prevent OOM
      this.maxConcurrent = Math.max(this.maxConcurrent - 10, this.minConcurrent);
    }
  }
  // autoCheckConnections() {
  //   for (const [ip, entry] of Object.entries(this.clients)) {
  //   this.emit('checkConnection', entry, ip);
  //   }
  // }
}
// ✅ Helper để check HMI connection
function isHMIConnected(entry, ip) {
  if (!entry || entry.isDisconnected || !entry.client || typeof entry.client.readHoldingRegisters !== "function") {
  console.warn(`[${ip}] ❌ HMI disconnected`);
  return false;
  }
  return true;
  }

module.exports = {ModbusPollingManager, isHMIConnected};
