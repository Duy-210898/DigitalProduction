const fs = require('fs');
const net = require('net');
const Modbus = require('jsmodbus');
const ping = require('ping');
const { updateDeviceConnectionStatus, getSizeDataFromDB, getDistributionDataFromDb, saveActualDataToDB, setOrderIsComplete } = require('./database');
const { notifyClientsToDeleteOrder } = require('./notifications');

let previousRegister6507Value = null; 
let previousRegister1034 = null;
let previousRegister1032 = null; 
let previousRegister6510 = null;
let isDataSentToModbus = false; 

let modbusClients = {};
let counter = 0; 
const successLogPath = './success_log.txt';
const errorLogPath = './error_log.txt';

function logToFile(filePath, message) {
  const timestamp = new Date().toISOString();
  const logMessage = `[${timestamp}] ${message}\n`;
  fs.appendFile(filePath, logMessage, (err) => {
    if (err) {
      console.error(`Failed to write log to ${filePath}: ${err.message}`);
    }
  });
}

async function pingHost(ipAddress) {
  try {
    return new Promise((resolve) => {
      ping.sys.probe(ipAddress, (isAlive) => {
        resolve(isAlive);
      });
    });
  } catch (error) {
    console.error(`Error pinging device at ${ipAddress}: ${error.message}`);
    logToFile(errorLogPath, `Error pinging device at ${ipAddress}: ${error.message}`);
    return false;
  }
}


async function performActionForBit(client, bitIndex) {
  const ipAddress = client.socket.remoteAddress;
  let previousBitIndex = -1;
  const baseRead = 560; 

  // Tính toán địa chỉ đọc dựa trên bitIndex
  const read = baseRead + bitIndex * 20; 
  console.log(`Địa chỉ đọc được tính toán: ${read}`);

  // Lấy partName từ Modbus trước
  const partName = await getPartNameFromModbus(client, read);

  // Kiểm tra xem bitIndex có thay đổi không
  if (previousBitIndex !== bitIndex) {
    console.log(`Thực hiện hành động cho chỉ số bit: ${bitIndex}`);

    // Kiểm tra phạm vi của bitIndex
    if (bitIndex < 0 || bitIndex > 11) {
      return console.warn(`Không có hành động được định nghĩa cho chỉ số bit: ${bitIndex}`);
    }

    // Thực hiện ghi dữ liệu vào Modbus sau khi lấy partName
    await writeSizeDataToModbus(client);

    // Lưu partName vào đối tượng modbusClients
    if (!modbusClients[ipAddress]) {
      modbusClients[ipAddress] = {};
    }
    modbusClients[ipAddress].partName = partName;

    console.log(`Đã lưu partName cho client tại ${ipAddress}: ${partName}`);

    // Cập nhật giá trị bitIndex trước đó
    previousBitIndex = bitIndex;
  } else {
    console.log(`Chỉ số bit ${bitIndex} không thay đổi, không thực hiện hành động.`);
  }
}

const previousData = {}; 

async function getPartNameFromModbus(client, startAddress) {
  try {
    const partNameData = await client.readHoldingRegisters(startAddress, 20);

    let partName = '';

    for (let i = 0; i < partNameData.response._body.values.length; i++) {
      const registerValue = partNameData.response._body.values[i];

      const lowByte = registerValue & 0xFF; 
      const highByte = (registerValue >> 8) & 0xFF; 

      partName += String.fromCharCode(lowByte); 
      if (highByte !== 0) { 
        partName += String.fromCharCode(highByte); 
      }
    }
    console.log(`Part Name getPartName: ${partName}`);

    return partName;
  } catch (err) {
    console.error(`Error reading PartName from Modbus: ${err.message}`);
    logToFile(errorLogPath, `Error reading PartName from Modbus: ${err.message}`);
    throw err; 
  }
}

async function handleDisconnection(ipAddress) {
  const modbusClient = modbusClients[ipAddress];
  if (modbusClient) {
    modbusClient.socket.end();
    delete modbusClients[ipAddress];
    console.log(`Disconnected from device at ${ipAddress}`);
    logToFile(successLogPath, `Disconnected from device at ${ipAddress}`);
    await updateDeviceConnectionStatus(ipAddress, false).catch((err) => {
      console.error(`Error updating device connection status: ${err.message}`);
    });
  } else {
    console.log(`No Modbus client found for ${ipAddress}, skipping disconnection process.`);
  }
}

async function connectToDevice(ipAddress, retries = 0) {
  if (!modbusClients[ipAddress]) {
    modbusClients[ipAddress] = {};
  }

  const modbusClient = modbusClients[ipAddress];

  if (modbusClient.isDisconnected) {
    console.log(`Device at ${ipAddress} is disconnected. Skipping reconnection attempt.`);
    return;
  }

  const isReachable = await pingHost(ipAddress);
  if (!isReachable) {
    console.log(`Device at ${ipAddress} is not reachable. Skipping connection attempt.`);
    logToFile(errorLogPath, `Device at ${ipAddress} is not reachable. Skipping connection attempt.`);
    await updateDeviceConnectionStatus(ipAddress, false).catch((err) => {
      console.error(`Error updating device connection status: ${err.message}`);
    });
    return;
  }

  if (modbusClient && modbusClient.isConnected) {
    return modbusClient;
  }

  const socket = new net.Socket();
  const client = new Modbus.client.TCP(socket, 1);
  const options = { host: ipAddress, port: 502 };

  socket.connect(options, async function () {
    console.log(`Connected to device at ${ipAddress}`);
    logToFile(successLogPath, `Connected to device at ${ipAddress}`);
    
    modbusClients[ipAddress] = { client, socket, isConnected: true, isDisconnected: false };
  
    try {
      await updateDeviceConnectionStatus(ipAddress, true).catch((err) => {
        console.error(`Error updating device connection status: ${err.message}`);
      });
    } catch (err) {
      console.error(`Error updating device connection status for ${ipAddress}: ${err.message}`);
      logToFile(errorLogPath, `Error updating device connection status for ${ipAddress}: ${err.message}`);
    }
  
    startReadingRegisters(client, ipAddress);
  
    try {
      writeToModbusRegister(client).catch((err) => {
        console.error(`Error writing to Modbus register for ${ipAddress}: ${err.message}`);
      });
    } catch (err) {
      console.error(`Error writing to Modbus register for ${ipAddress}: ${err.message}`);
      logToFile(errorLogPath, `Error writing to Modbus register for ${ipAddress}: ${err.message}`);
    }
  
  });
  
  socket.on('error', async function (error) {
    console.error(`Unable to connect to device at ${ipAddress}: ${error.message}`);
    logToFile(errorLogPath, `Unable to connect to device at ${ipAddress}: ${error.message}`);
  
    if (modbusClient) {
      modbusClient.isDisconnected = true;
    }
  
    await handleDisconnection(ipAddress); 
  
    if (retries < 3) {
      console.log(`Retrying connection to ${ipAddress} (${retries + 1}/3)`);
      setTimeout(() => connectToDevice(ipAddress, retries + 1), 10000);
    } else {
      console.log(`Max retries reached for ${ipAddress}. Giving up on connection.`);
      await updateDeviceConnectionStatus(ipAddress, false).catch((err) => {
        console.error(`Error updating device connection status: ${err.message}`);
      });
    }
  });
  
  socket.on('close', async function () {
    console.log(`Connection closed to device at ${ipAddress}`);
    logToFile(successLogPath, `Connection closed to device at ${ipAddress}`);
  
    if (modbusClient) {
      modbusClient.isDisconnected = true;
    }
  
    await handleDisconnection(ipAddress);
  });
  
  return modbusClients[ipAddress];
  }
  

  async function startReadingRegisters(client, ipAddress) {
    setInterval(() => {
      readAndCheckBits(client, ipAddress);
      readAndProcessID(client, ipAddress); 
      readActualData(client, ipAddress)
    }, 1000);
  }
  
  async function readAndCheckBits(client, ipAddress) {
    try {
      const registers = await Promise.all([
        client.readHoldingRegisters(1034, 1),
        client.readHoldingRegisters(1032, 1),
        client.readHoldingRegisters(6510, 1)
      ]);
  
      const register1034 = registers[0].response._body.values[0];
      const register1032 = registers[1].response._body.values[0];
      const register6510 = registers[2].response._body.values[0];
  
      processRegister1034(register1034, client);
      processRegister1032(register1032, client, ipAddress);
      processRegister6510(register6510, client, ipAddress);
  
    } catch (err) {
      console.error(`Error reading or processing Modbus registers: ${err.message}`);
      logToFile(errorLogPath, `Error reading or processing Modbus registers: ${err.message}`);
    }
  }
    
  function processRegister1034(register1034, client) {
    let bitString1034 = '';
    for (let i = 0; i < 16; i++) {
      const bitValue = (register1034 >> i) & 1;
      bitString1034 = bitValue + bitString1034;
    }
  
    if (previousRegister1034 === null || previousRegister1034 !== register1034) {
      console.log(`Thanh ghi chọn PartName: ${bitString1034}`);
      previousRegister1034 = register1034;
      for (let i = 0; i <= 12; i++) {
        const bitValue = (register1034 >> i) & 1;
        if (bitValue === 1) {
          performActionForBit(client, i); 
        }
      }
    }
  }
  
  async function processRegister1032(register1032, client, ipAddress) {
    let bitString1032 = '';
    for (let i = 0; i < 16; i++) {
        const bitValue = (register1032 >> i) & 1;
        bitString1032 = bitValue + bitString1032;
    }

    if (previousRegister1032 === null || previousRegister1032 !== register1032) {
        previousRegister1032 = register1032;
        console.log(`Thanh ghi yêu cầu xóa đơn: ${bitString1032}`);

        const bitValue1 = (register1032 >> 0) & 1;
        if (bitValue1 === 1) {
            try {
                if (client && typeof client.writeSingleRegister === 'function') {
                    // Ensure to get the client associated with the ipAddress
                    const modbusClient = modbusClients[ipAddress];

                    // Kiểm tra kết nối trước khi thực hiện ghi
                    if (modbusClient && modbusClient.isConnected) {
                        try {
                            const OrderID = modbusClients[ipAddress]?.orderID;

                            await setOrderIsComplete(OrderID);

                            // Đọc giá trị hiện tại của thanh ghi
                            const data = await client.readHoldingRegisters(1032, 1);
                            let registerValue = data.response._body.values[0];

                            registerValue |= (1 << 1);

                            await client.writeSingleRegister(1032, registerValue);
                            console.log(`Written ${registerValue} to register 1032`);
                            logToFile(successLogPath, `Written ${registerValue} to register 1032`);
                        } catch (err) {
                            console.error(`Error writing to Modbus register: ${err.message}`);
                            logToFile(errorLogPath, `Error writing to Modbus register: ${err.message}`);
                        }
                    } else {
                        console.error(`Client is not connected.`);
                    }
                } else {
                    console.error('Client does not have writeSingleRegister method or is invalid');
                }
            } catch (error) {
                console.error("Lỗi trong quá trình xử lý yêu cầu xóa đơn:", error);
                logToFile(errorLogPath, `Lỗi trong quá trình xử lý yêu cầu xóa đơn: ${error.message}`);
            }
        }
    }
}

async function processRegister6510(register6510, client, ipAddress) {
    let bitString6510 = '';
    for (let i = 0; i < 16; i++) {
      const bitValue = (register6510 >> i) & 1;
      bitString6510 = bitValue + bitString6510;
    }
  
    if (previousRegister6510 === null || previousRegister6510 !== register6510) {
      previousRegister6510 = register6510;
      console.log(`Thanh ghi gửi lại dữ liệu: ${bitString6510}`);
      
      const bitValue1 = (register6510 >> 0) & 1;
      if (bitValue1 === 1 && !isDataSentToModbus[ipAddress]) {
        const distributionData = await getDistributionDataFromDb(ipAddress);
        console.log(`${ipAddress}`);
        console.log('Distribution Data:', distributionData);
  
        if (distributionData) {
          await saveDistributionDataToModbus(ipAddress, distributionData);
          await writeSizeDataToModbus(client)
          isDataSentToModbus[ipAddress] = true;
        } else {
          console.log('No distribution data found for this IP address.');
        }
      }
    }
  }
  async function readAndProcessID(client, ipAddress) {
    try {
      const data = await client.readHoldingRegisters(6507, 1);
      const register6507 = data.response._body.values[0];
      
      if (register6507 === 0) {
        const distributionData = await getDistributionDataFromDb(ipAddress);
        
        if (distributionData) {
          await delay(1000);
          console.log('Saving distribution data to Modbus:', distributionData);
          await saveDistributionDataToModbus(ipAddress, distributionData);
        } else {
        }
      } else {
        if (!modbusClients[ipAddress]) {
          modbusClients[ipAddress] = {};  
        }
        
        modbusClients[ipAddress].orderID = register6507;
      }
    } catch (error) {
      const errorMsg = `Error reading register 6507 for IP ${ipAddress}: ${error.message}`;
      console.error(errorMsg);
      logToFile(errorLogPath, errorMsg);
    }
  }
  
  function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
        
  async function readActualData(client, ipAddress) {
    try {
      const sizeLabels = [
        "10K", "10.5K", "11K", "11.5K", "12K", "12.5K", "13K", "13.5K",
        "1", "1.5", "2", "2.5", "3", "3.5", "4", "4.5", "5", "5.5",
        "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5",
        "11", "11.5", "12", "12.5", "13", "13.5", "14", "14.5"
      ];
  
      const currentData = {};
  
      // Đọc OrderID từ thanh ghi 6507
      const orderIDData = await client.readHoldingRegisters(6507, 1);
      if (!orderIDData || !orderIDData.response || !orderIDData.response._body) {
        throw new Error('Không thể đọc OrderID từ thanh ghi 6507');
      }
      const OrderID = orderIDData.response._body.values[0];
  
      // Lấy PartName từ đối tượng modbusClients
      const partName = modbusClients[ipAddress]?.partName;
      console.log(`PartName on readActualData: ${partName}`);
  
      const promises = sizeLabels.map((label, i) => {
        const baseAddress = 6003 + i * 14;
        return Promise.all([ // Đọc nhiều thanh ghi
          client.readHoldingRegisters(baseAddress + 2, 1),  // PiecesPerPair
          client.readHoldingRegisters(baseAddress + 4, 1),  // MaterialLayer
          client.readHoldingRegisters(baseAddress + 6, 1),  // CuttingDieQty
          client.readHoldingRegisters(baseAddress + 8, 1),  // ActualCut
          client.readHoldingRegisters(baseAddress + 10, 1), // ActualPieces
          client.readHoldingRegisters(baseAddress + 12, 1)  // ActualSizeQty
        ]).then(async ([piecesPerPairData, materialLayerData, cuttingDieQtyData, actualCutData, actualPiecesData, actualSizeQtyData]) => {
      
          // Kiểm tra và xử lý dữ liệu từ Modbus cho mỗi thanh ghi
          const piecesPerPair = piecesPerPairData?.response?._body?.values[0] ?? 0;
          const materialLayer = materialLayerData?.response?._body?.values[0] ?? 0;
          const cuttingDieQty = cuttingDieQtyData?.response?._body?.values[0] ?? 0;
          const actualCut = actualCutData?.response?._body?.values[0] ?? 0;
          const actualPieces = actualPiecesData?.response?._body?.values[0] ?? 0;
          const actualSizeQty = actualSizeQtyData?.response?._body?.values[0] ?? 0;
  
          // Lưu dữ liệu vào currentData
          currentData[label] = {
            PiecesPerPair: piecesPerPair,
            MaterialLayer: materialLayer,
            CuttingDieQty: cuttingDieQty,
            ActualCut: actualCut,
            ActualPieces: actualPieces,
            ActualSizeQty: actualSizeQty
          };
  
          // Kiểm tra sự thay đổi và chỉ lưu nếu có sự thay đổi
          const prevData = previousData ? previousData[label] : null;
          let hasChanges = !prevData || (
            piecesPerPair !== prevData.PiecesPerPair ||
            materialLayer !== prevData.MaterialLayer ||
            cuttingDieQty !== prevData.CuttingDieQty ||
            actualCut !== prevData.ActualCut ||
            actualPieces !== prevData.ActualPieces ||
            actualSizeQty !== prevData.ActualSizeQty
          );
  
          if (hasChanges) {
            if (piecesPerPair !== 0 || materialLayer !== 0 ||
              cuttingDieQty !== 0 || actualCut !== 0 || actualPieces !== 0 || actualSizeQty !== 0) {
              
              console.log(`${label} =`, currentData[label]);
      
              await saveActualDataToDB({
                OrderID: OrderID,
                SizeData: [{
                  Size: label,
                  PiecesPerPair: piecesPerPair,
                  MaterialLayer: materialLayer,
                  CuttingDieQty: cuttingDieQty,
                  ActualCut: actualCut,
                  ActualPieces: actualPieces,
                  ActualSizeQty: actualSizeQty
                }],
              }, partName); 
            }
      
            previousData[label] = {
              PiecesPerPair: piecesPerPair,
              MaterialLayer: materialLayer,
              CuttingDieQty: cuttingDieQty,
              ActualCut: actualCut,
              ActualPieces: actualPieces,
              ActualSizeQty: actualSizeQty
            };
          }
        });
      });
      
      await Promise.all(promises);
      
    } catch (error) {
      console.error(`Error reading actual data: ${error.message}`);
      logToFile(errorLogPath, `Error reading actual data: ${error.message}`);
    }
  }  
  async function writeToModbusRegister(client) {
  try {
    setInterval(async () => {
      counter++; 
      await client.writeSingleRegister(5999, counter).catch((err) => {
        console.error(`Error writing to Modbus register: ${err.message}`);
        logToFile(errorLogPath, `Error writing to Modbus register: ${err.message}`);
      });
    }, 1000); 
  } catch (error) {
    console.error(`Error writing to Modbus register: ${error.message}`);
    logToFile(errorLogPath, `Error writing to Modbus register: ${error.message}`);
  }
}

function startMonitoring() {
  setInterval(async () => {
    for (const ipAddress in modbusClients) {
      const client = modbusClients[ipAddress];
      if (client && client.isConnected) {
      }
    }
  }, 5000);  
}

function stringTo16BitArrayLittleEndian(str) {
  const result = [];
  for (let i = 0; i < str.length; i += 2) {
    const low = str.charCodeAt(i);
    const high = i + 1 < str.length ? str.charCodeAt(i + 1) : 0;
    result.push((high << 8) | low); 
  }
  return result;
}

async function writeSizeDataToModbus(client) {
  const sizeToRegisterMapping = {
    "10K": 0, "10.5K": 14, "11K": 28, "11.5K": 42, "12K": 56, "12.5K": 70,
    "13K": 84, "13.5K": 98, "1": 112, "1.5": 126, "2": 140, "2.5": 154,
    "3": 168, "3.5": 182, "4": 196, "4.5": 210, "5": 224, "5.5": 238,
    "6": 252, "6.5": 266, "7": 280, "7.5": 294, "8": 308, "8.5": 322,
    "9": 336, "9.5": 350, "10": 364, "10.5": 378, "11": 392, "11.5": 406,
    "12": 420, "12.5": 434, "13": 448, "13.5": 462, "14": 476, "14.5": 490
  };

  const sizeDataMap = {};

  try {
    const ipAddress = client.socket.remoteAddress;
    console.log(`IP Address: ${ipAddress}`);

    // Đọc dữ liệu OrderID từ Modbus
    const [orderIdData] = await Promise.all([client.readHoldingRegisters(6507, 1)]);
    const partName = modbusClients[ipAddress]?.partName; 
    console.log(`PartName on write: ${partName}`);

    // Kiểm tra OrderID hợp lệ
    const orderID = parseInt(orderIdData.response._body.values[0], 10);
    if (isNaN(orderID) || orderID <= 0) {
      console.error(`Invalid OrderID: ${orderID}`);
      return;
    }
    console.log(`OrderID: ${orderID}`);

    // Kiểm tra partName hợp lệ
    if (!partName || partName.trim() === "") {
      console.error('Invalid PartName, skipping database query.');
      return;
    }

    console.log(`Fetching size data with OrderID: ${orderID} and PartName: ${partName}`);

    // Lấy dữ liệu kích thước từ cơ sở dữ liệu
    const sizeData = await getSizeDataFromDB(ipAddress, orderID, partName);

    if (!sizeData || sizeData.length === 0) {
      console.log('No size data found.');
      return;
    }

    // Chuyển dữ liệu kích thước vào sizeDataMap
    sizeData.forEach(item => {
      sizeDataMap[item.Size] = item;
    });

    // Chuyển sizeDataMap thành mảng để xử lý
    const sizeDataArray = Object.keys(sizeToRegisterMapping).map(size => ({
      Size: size,
      Data: sizeDataMap[size] || {}
    }));

    // Ghi dữ liệu vào các thanh ghi Modbus
    for (const { Size, Data } of sizeDataArray) {
      const registerAddress = sizeToRegisterMapping[Size];
      if (registerAddress !== undefined && Data.SizeQty > 0) {
        try {
          await client.writeSingleRegister(registerAddress, Data.SizeQty || 0);
          await client.writeSingleRegister(registerAddress + 2, 0);
          await client.writeSingleRegister(registerAddress + 4, 0);
          await client.writeSingleRegister(registerAddress + 6, 0);
          await client.writeSingleRegister(registerAddress + 8, 0);
          await client.writeSingleRegister(registerAddress + 10, 0);
          await client.writeSingleRegister(registerAddress + 12, Data.ActualSizeQty || 0);
          console.log(`Successfully written data to registers starting from ${registerAddress} for size ${Size}`);
        } catch (error) {
          console.error(`Error writing size "${Size}" data to registers starting from ${registerAddress}: ${error.message}`);
          logToFile(errorLogPath, `Error writing size "${Size}" data to registers starting from ${registerAddress}: ${error.message}`);
        }
      }
    }
  } catch (err) {
    console.error(`Error saving size data: ${err.message}`);
    logToFile(errorLogPath, `Error saving size data: ${err.message}`);
  }
}


async function saveDistributionDataToModbus(ipAddress, data) {
  const modbusClient = await connectToDevice(ipAddress);
  const client = modbusClient.client;

  if (!client || !client.writeSingleRegister) {
    throw new Error(`Modbus client not properly initialized for device at ${ipAddress}`);
  }

  try {
    if (!modbusClient.socket || !modbusClient.socket.writable) {
      console.log(`Reconnecting to device at ${ipAddress}`);
      await connectToDevice(ipAddress);
    }

    // Ghi thông tin đơn hàng (Model, ART, SO, MasterWorkOrder)
    const orderInfo = [data.Model, data.ART, data.SO, data.MasterWorkOrder];
    const orderInfoAddresses = [
      { start: 505, maxRegisters: 25 },
      { start: 530, maxRegisters: 10 },
      { start: 540, maxRegisters: 10 },
      { start: 550, maxRegisters: 10 }
    ];

    const orderInfoPromises = orderInfo.map(async (info, index) => {
      let registerData = stringTo16BitArrayLittleEndian(info);
      const maxRegisters = orderInfoAddresses[index].maxRegisters;
      registerData = registerData.slice(0, maxRegisters * 2);
      const startRegister = orderInfoAddresses[index].start;
      for (let j = 0; j < registerData.length; j++) {
        await client.writeSingleRegister(startRegister + j, registerData[j]);
      }
    });

    await Promise.all(orderInfoPromises);

    const partNameStartRegister = 560;
    const partNamePromises = data.MaterialData.slice(0, 12).map(async (material, i) => {
      const partName = material.PartName;
      const startRegister = partNameStartRegister + (i * 20); 
      let registerData = stringTo16BitArrayLittleEndian(partName).slice(0, 10 * 2); 
      for (let j = 0; j < registerData.length; j++) {
        await client.writeSingleRegister(startRegister + j, registerData[j]);
      }
    });

    await Promise.all(partNamePromises);

    const materialStartRegister = 1046;
    const materialNamePromises = data.MaterialData.slice(0, 12).map(async (material, i) => {
      const materialName = material.MaterialsName;
      const startRegister = materialStartRegister + (i * 210); 
      let registerData = stringTo16BitArrayLittleEndian(materialName).slice(0, 210 * 2);
      registerData = registerData.map(value => Math.min(value, 65535)); 
      for (let j = 0; j < registerData.length; j++) {
        await client.writeSingleRegister(startRegister + j, registerData[j]);
      }
    });

    await Promise.all(materialNamePromises);

    await client.writeSingleRegister(6507, data.OrderID);

    console.log('Data successfully saved to Modbus');
  } catch (error) {
    console.error(`Error saving distribution data to Modbus: ${error.message}`);
    throw new Error(`Failed to save distribution data: ${error.message}`);
  }
}

async function closeAllConnections() {
  for (const ipAddress in modbusClients) {
    const modbusClient = modbusClients[ipAddress];
    if (modbusClient) {
      await updateDeviceConnectionStatus(ipAddress, false).catch((err) => {
        console.error(`Error updating device connection status: ${err.message}`);
      });
      modbusClient.socket.end();
      console.log(`Closed connection to device at ${ipAddress}`);
      logToFile(successLogPath, `Closed connection to device at ${ipAddress}`);
    }
  }
}

async function setIpAddresses(ipAddresses) {
  try {
    for (const ipAddress of ipAddresses) {
      await connectToDevice(ipAddress).catch((err) => {
        console.error(`Error connecting to device at ${ipAddress}: ${err.message}`);
        logToFile(errorLogPath, `Error connecting to device at ${ipAddress}: ${err.message}`);
      });
    }
  } catch (error) {
    console.error(`Error setting IP addresses: ${error.message}`);
    logToFile(errorLogPath, `Error setting IP addresses: ${error.message}`);
  }
}
startMonitoring();
module.exports = {
  connectToDevice,
  startMonitoring,
  writeSizeDataToModbus,
  processRegister1032,
  saveDistributionDataToModbus,
  closeAllConnections,
  setIpAddresses,
  startReadingRegisters,
};
