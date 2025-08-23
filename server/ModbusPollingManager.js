const EventEmitter = require('events');
const ping = require('ping'); // dùng để ping host

// ================== POLLING MANAGER ==================
class ModbusPollingManager extends EventEmitter {
  constructor(interval = 1000, maxConcurrent = 2) {
    super();
    this.interval = interval;
    this.maxConcurrent = maxConcurrent;
    this.clients = {};   // { ip: { client, socket, isConnected, sizeDataInfo, ... } }
    this.queue = [];     // danh sách IP để polling
    this.activeCount = 0;
    this.timer = null;
  }

  registerClient(ipAddress, clientEntry) {
    this.clients[ipAddress] = {
      ...clientEntry,
      isProcessing: false,
      isConnecting: false,
      taskLocks: {}
    };
    if (!this.queue.includes(ipAddress)) {
      this.queue.push(ipAddress);
    }
    if (!this.timer) {
      this.startScheduler();
    }
  }

  unregisterClient(ipAddress) {
    delete this.clients[ipAddress];
    this.queue = this.queue.filter(ip => ip !== ipAddress);
  }

  startScheduler() {
    this.timer = setInterval(() => {
      if (this.activeCount >= this.maxConcurrent) return;
      const ip = this.queue.shift();
      if (!ip) return;
      this.pollOne(ip).finally(() => {
        this.queue.push(ip);
      });
    }, this.interval);
  }

  stopScheduler() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  async pollOne(ipAddress) {
    const entry = this.clients[ipAddress];
    if (!entry || entry.isProcessing) return;
    entry.isProcessing = true;
    this.activeCount++;

    try {
      const { client, socket, isConnected } = entry;

      // Connection check
      if (!client || !isConnected || !socket || socket.destroyed || !socket.writable) {
        await this.safeConnect(ipAddress);
        return;
      }

      // Task 1: Poll distribution
      await this.runWithLock(entry, 'poll', async () => {
        if (await ping.promise.probe(ipAddress)) {
          this.emit('poll', client, entry, ipAddress);
        }
      });

      // Task 2: Read actual data if sizeDataInfo exists
      if (entry.sizeDataInfo && Object.keys(entry.sizeDataInfo).length > 0) {
        await this.runWithLock(entry, 'readActual', async () => {
          if (await ping.promise.probe(ipAddress)) {
            this.emit('readActual', client, entry, ipAddress);
          }
        });
      }

      // Task 3: Write register
      await this.runWithLock(entry, 'writeRegister', async () => {
        if (await ping.promise.probe(ipAddress)) {
          this.emit('writeRegister', client, entry, ipAddress);
        }
      });

    } catch (err) {
      console.error(`[${ipAddress}] Polling error: ${err.message}`);
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
}

module.exports = ModbusPollingManager;
