const fs = require('fs');
const net = require('net');
const Modbus = require('jsmodbus');
const ping = require('ping');
const { updateDeviceConnectionStatus, getSizeDataFromDB, getDistributionDataFromDb, saveActualDataToDB, setOrderIsComplete, setDistributionIsComplete, getSizeAndDistributionDataFromDb, getDistributionIDFromSizeID, getDistributionCompleteFromDb} = require('./database');
const { notifyClientsToDeleteOrder } = require('./notifications');

let previousRegister6507Value = null;
let previousRegister1034 = null;
let previousRegister1032 = null;
let previousRegister6510 = null;
let isDataSentToModbus = false;
let countCompleteSize = 0;

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
    if (modbusClient.socket) {
      modbusClient.socket.end();
    } 
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
      modbusClients[ipAddress] = {}; // Ensure the object exists
  }

  let modbusClient = modbusClients[ipAddress];

  if (modbusClient.isDisconnected) {
      console.log(`🚫 Device at ${ipAddress} is marked as disconnected. Skipping reconnection.`);
      return;
  }

  const isReachable = await pingHost(ipAddress);
  if (!isReachable) {
      console.error(`🔴 Device at ${ipAddress} is not reachable.`);
      logToFile(errorLogPath, `Device at ${ipAddress} is not reachable.`);
      await updateDeviceConnectionStatus(ipAddress, false);
      return;
  }

  if (modbusClient && modbusClient.isConnected) {
      return modbusClient; // Already connected, no need to reconnect
  }

  const socket = new net.Socket();
  const client = new Modbus.client.TCP(socket, 1);
  const options = { host: ipAddress, port: 502 };

  return new Promise((resolve, reject) => {
      socket.connect(options, async function () {
          console.log(`✅ Connected to device at ${ipAddress}`);
          logToFile(successLogPath, `Connected to device at ${ipAddress}`);

          modbusClients[ipAddress] = { client, socket, isConnected: true, isDisconnected: false };

          try {
              await updateDeviceConnectionStatus(ipAddress, true);
          } catch (err) {
              console.error(`⚠️ Error updating device status: ${err.message}`);
              logToFile(errorLogPath, `Error updating device connection status: ${err.message}`);
          }

          startReadingRegisters(client, ipAddress);
          writeToModbusRegister(client).catch((err) => {
              console.error(`⚠️ Error writing to Modbus register: ${err.message}`);
              logToFile(errorLogPath, `Error writing to Modbus register: ${err.message}`);
          });

          resolve(client);
      });

      socket.on('error', async function (error) {
          console.error(`❌ Unable to connect to device at ${ipAddress}: ${error.message}`);
          logToFile(errorLogPath, `Unable to connect to device at ${ipAddress}: ${error.message}`);

          // Properly clean up and reset the client
          if (modbusClients[ipAddress]) {
              modbusClients[ipAddress].isDisconnected = true;
              modbusClients[ipAddress].isConnected = false;
              delete modbusClients[ipAddress].client;
              delete modbusClients[ipAddress].socket;
          }

          await handleDisconnection(ipAddress);

          if (retries < 3) {
              console.log(`🔄 Retrying connection to ${ipAddress} (${retries + 1}/3)`);
              setTimeout(() => connectToDevice(ipAddress, retries + 1).then(resolve).catch(reject), 10000);
          } else {
              console.log(`❌ Max retries reached for ${ipAddress}. Giving up.`);
              await updateDeviceConnectionStatus(ipAddress, false);
              reject(new Error("Max retries reached"));
          }
      });

      socket.on('close', async function () {
          console.log(`⚠️ Connection closed to device at ${ipAddress}`);
          logToFile(successLogPath, `Connection closed to device at ${ipAddress}`);

          if (modbusClients[ipAddress]) {
              modbusClients[ipAddress].isDisconnected = true;
              modbusClients[ipAddress].isConnected = false;
              delete modbusClients[ipAddress].client;
              delete modbusClients[ipAddress].socket;
          }

          await handleDisconnection(ipAddress);
      });
  });
}


async function startReadingRegisters(client, ipAddress) {
  setInterval(() => {

    //readAndCheckBits(client, ipAddress);
    readOperatorID(client, ipAddress);
    checkAndSaveDistribution(client, ipAddress);
    if (modbusClients[ipAddress].sizeDataInfo != null) {
      readActualData(client, ipAddress);
    }
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

async function checkAndSaveDistribution(client, ipAddress) {
  try {
    // Read register 1000
    let response = await client.readHoldingRegisters(1000, 1);
    let orderID = response.response._body.values[0]; // Extract the actual value
    console.log(`Register 1000 value: ${orderID}`);

    if (orderID === 0) {
      console.log(`Register 1000 is 0. Fetching distribution data for IP: ${ipAddress}`);

      // Fetch distribution data from DB
      const distributionData = await getDistributionDataFromDb(ipAddress);

      if (distributionData) {
        // Write order ID to register 1000
        await client.writeSingleRegister(1000, distributionData.OrderID);
        await delay(1000);

        console.log('Saving distribution data to Modbus...');
        await saveDistributionDataToModbus(ipAddress, distributionData);
      } else {
        console.warn(`No distribution data found for IP ${ipAddress}. Skipping save.`);
      }
    } 
    else {
    console.log(`Register 1000 already has value ${orderID} for IP ${ipAddress}`);

    // Store orderID in modbusClients
    if (!modbusClients[ipAddress]) {
      modbusClients[ipAddress] = {};
    }
    modbusClients[ipAddress].orderID = orderID;

    // check complete order
    let responseLeather = await client.readHoldingRegisters(1001, 1); // Reusing the variable name 'response'
    let isLeather = responseLeather.response._body.values[0];
    console.log(`Register 1001 value: ${isLeather}`);
  
    // interrupt HMI set distributionData again
    if (modbusClients[ipAddress].sizeDataInfo == null) {
        const distributionData = await getDistributionDataFromDb(ipAddress);
        if (distributionData != null) {
          await saveDistributionDataToModbus(ipAddress, distributionData);
        }
    }
    let sizeAddress;
    let chooseSizeAddress;
    let distribution;
    deleteDistributionFromRegister(client);
    if (isLeather !== 0) {
      if (isLeather === 1) {
          chooseSizeAddress = 3101;
          sizeAddress = 3102;
          distribution = await getSizeAndDistributionDataFromDb(ipAddress, orderID, 0);

        } else {
          chooseSizeAddress = 3103;
          sizeAddress = 3104;
          distribution = await getSizeAndDistributionDataFromDb(ipAddress, orderID, 1);
      }
        if (distribution !== null) {

          let results = [];
        
          try {
                // Check choose size 
                const responseChooseSize = await client.readHoldingRegisters(chooseSizeAddress, 1);
                const registerChooseSizeValue = responseChooseSize.response._body.values[0];
  
                let binaryChooseSizsValue = registerChooseSizeValue.toString(2).padStart(16, '0');
        
                console.log(`Output of size at register address ${chooseSizeAddress} = ${registerChooseSizeValue}`);
                console.log(`Binary representation: ${binaryChooseSizsValue}`);
                
                const index = findSetBitIndex(binaryChooseSizsValue);
                if (index !== -1) {
                // check reason complete size order
                let responseReaseonCompleteSize = await client.readHoldingRegisters(3100, 1);
                let reasonComplete = responseReaseonCompleteSize.response._body.values[0];
                console.log("ReasonComplete: " + reasonComplete);
                console.log(`[IndexSize] ReasonComplete ${reasonComplete} The index of the set bit is: ${index}`);  

                if(reasonComplete > 0) {
                  console.log("[SizeID]: " +  distribution.SizeData[index].SizeID);
                  let  distributionIDFromSize = await getDistributionIDFromSizeID(ipAddress, orderID, isLeather === 1 ? 0 : 1, distribution.SizeData[index].SizeID);
                  if (distributionIDFromSize != null) {
                  let message, note;
                  switch (reasonComplete) {
                    case 1:
                        note = 1;
                        message = "Not enough materials";
                        break;
                    case 2:
                        note = 2;
                        message = "Change of plan";
                        break;
                    case 3:
                        note = 3;
                        message = "Forgot to choose size";
                        break;
                    default:
                        message = "Unknown reason";
                        break;
                  }
                  for (const item of distributionIDFromSize.DistributionID) {
                    try {
                        if (note == 0) {
                          //time to set reason
                          await delay(1000);
                        }
                        await setDistributionIsComplete(item.DistributionID, note);
                        console.log(`Updated DistributionID: ${item.DistributionID} note ${note}`);
                    } catch (error) {
                        console.error(`Error updating DistributionID: ${item.DistributionID}`, error);
                    }
              }
                console.log("Reason: " + message);
            }
          }          
        }
        let distributionComplete = await getDistributionCompleteFromDb(ipAddress, orderID, isLeather === 1 ? 0 : 1);

        if (distribution?.SizeData) {
            if (distributionComplete?.length && distribution.SizeData.length === distributionComplete.length) {
                console.log(`Complete due to reason: Size ${distribution.SizeData.length} and Completed ${distributionComplete.length}`);
            } else {
                try {
                    // Continue reading size
                    const response = await client.readHoldingRegisters(sizeAddress, 1);
                    const registerValue = response.response._body.values[0]; // Assuming the response contains an array

                    // Convert the registerValue to a binary string (16-bit)
                    let binaryValue = registerValue.toString(2).padStart(16, '0');
                    console.log(`Output of size at register address ${sizeAddress} = ${registerValue}`);
                    console.log(`Binary representation: ${binaryValue}`);

                    // Count '1' bits in the binary string
                    let countCompleteSize = binaryValue.split('').filter(bit => bit === '1').length;
                    console.log("[Size] Count size of '1' bits:", countCompleteSize);

                    results.push({ address: sizeAddress, value: registerValue });

                    // If all sizes are complete, update DistributionIDs
                    if (countCompleteSize === distribution.SizeData.length && Array.isArray(distribution.DistributionID)) {
                        for (const item of distribution.DistributionID) {
                            try {
                                await setDistributionIsComplete(item.DistributionID, 0);
                                console.log(`Updated DistributionID: ${item.DistributionID}, countCompleteSize: ${countCompleteSize}, distribution.SizeData: ${distribution.SizeData.length}`);

                                // Reset sizeID and delete from register
                                modbusClients[ipAddress].sizeDataInfo.sizeID = [];
                                deleteDistributionFromRegister(client);
                            } catch (error) {
                                console.error(`Error updating DistributionID: ${item.DistributionID}`, error);
                            }
                        }
                    }

                    console.log("Register Values:", results);
                } catch (error) {
                    console.error("Error reading size register:", error);
                }
            }
        } else {
            console.log("No valid size data found.");
        }

        } 
        catch (error) {
            console.error(`Error reading register ${sizeAddress}:`, error.message);
        }
      }
    }
  }} catch (error) {
    const errorMsg = `❌ Error reading register 1000 for IP ${ipAddress}: ${error.message}`;
    console.error(errorMsg);
    logToFile(errorLogPath, errorMsg);
  }
}

// Find bit index
function findSetBitIndex(n) {
  let index = 0;
  while (n > 0) {
    if (n & 1) {
      return index;
    }
    index++;
    n >>= 1;
  }
  return -1; // In case no bits are set.
}
/**
 * Delete distribution by modifying a register based on a specific condition.
 * @param {Object} client - The Modbus client instance.
 * @returns {Promise<void>} - Resolves when the operation is completed.
 */
async function deleteDistributionFromRegister(client) {
  let results = [];
  let deleteDistribution = 3000;
  const sizeIndex = 1;
  try {
    const response = await client.readHoldingRegisters(deleteDistribution, 1);
    const registerValue = response.response._body.values[0];

    // Convert the registerValue to a binary string with leading zeros
    let binaryValueDelete = registerValue.toString(2).padStart(16, '0');  // assuming 16-bit register, adjust if needed

    // Log the current value and binary representation
    console.log(`deleteDistribution ${deleteDistribution} = ${registerValue}`);
    console.log(`Binary deleteDistribution: ${binaryValueDelete}`);

    // Check if the specified bit is set at sizeIndex
    if (isBitSetString(binaryValueDelete, sizeIndex)) {
        // Prepare the value to write (000000000000100 is 4 in decimal)
        const valueToWrite = 4; // This corresponds to 000000000000100 in binary

        // Write the value 1 to register 3001
        await client.writeSingleRegister(deleteDistribution, valueToWrite);
        console.log(`Written value ${valueToWrite} to register 3001`);
        checkCompleteSize = 0;
    }

    results.push({ address: deleteDistribution, value: registerValue });
  } catch (error) {
    console.error(`Error reading register ${deleteDistribution}:`, error.message);
  }
}

  // Check bit at position `n` (1-based index from right)
function isBitSetString(binaryStr, bitPosition) {
    return binaryStr[binaryStr.length - bitPosition] === '1';
  }
async function readOperatorID(client, ipAddress) {
  try {
    const data = await client.readHoldingRegisters(3105, 2);
    const lowRegister = data.response._body.values[0];  // Low register (16 bits)
    const highRegister = data.response._body.values[1]; // High register (16 bits)

    // Combine the two 16-bit
    const operatorID = (highRegister << 16) | lowRegister;
    modbusClients[ipAddress].operatorID = operatorID;
    console.info(`OperatorID: ${operatorID} IP: ${ipAddress}`);
  } catch (error) {
    const errorMsg = `Error reading register 3105 for IP ${ipAddress}: ${error.message}`;
    console.error(errorMsg);
    logToFile(errorLogPath, errorMsg);
  }
}
function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
async function readActualData(client, ipAddress) {
  try {
    const currentData = {};
  //   let sizeDataInfo = {
  //     sizeID: [900],
  //     isLeather: 2,
  //     sizeCount: 1
  // };
    
    // Read OrderID from register 1000
    const orderIDData = await client.readHoldingRegisters(1000, 1);
    if (!orderIDData || !orderIDData.response || !orderIDData.response._body || orderIDData.response._body.values[0] == 0) {
      console.log('Không thể đọc OrderID từ thanh ghi 1000');
      return;
    }

    const baseAddress = modbusClients[ipAddress].sizeDataInfo.isLeather == 1 ? 886 : 966;
    const baseActul = modbusClients[ipAddress].sizeDataInfo.isLeather == 1 ? 794 : 922;
    const OrderID = orderIDData.response._body.values[0];

    // Use map instead of forEach to properly handle promises
    const promises = modbusClients[ipAddress].sizeDataInfo.sizeID.map(async (sizeAdressID, index) => {
      try {
        // Function to safely read registers and handle errors
        const safeRead = async (address) => {
          try {
            const response = await client.readHoldingRegisters(address, 1);
            return response?.response?._body?.values[0] ?? 0; // Return value or default 0
          } catch (err) {
            console.error(`Error reading register ${address}:`, err.message);
            return null; // Return null instead of breaking execution
          }
        };
    
        // Read Modbus registers safely
        const [
          sizeID, piecesPerPair, materialLayer, cuttingDieQty, 
          actualCut, actualPieces, actualSizeQty, totalPieces
        ] = await Promise.all([
          safeRead(sizeAdressID),  // Read sizeID safely
          modbusClients[ipAddress].sizeDataInfo.isLeather == 1 ? safeRead(baseAddress) : Promise.resolve(null), // picesPer
          modbusClients[ipAddress].sizeDataInfo.isLeather == 1 ? safeRead(baseAddress + 4) :  Promise.resolve(null), //material
          modbusClients[ipAddress].sizeDataInfo.isLeather == 1 ? safeRead(baseAddress + 8) :  Promise.resolve(null), //cutting
          safeRead(baseActul + (16 * index)), //actul Cut
          safeRead(baseActul + 4 + (16 * index)), //actualpieces
          safeRead(baseActul + 8 + (16 * index)), //actualSizeQty
          modbusClients[ipAddress]. sizeDataInfo.isLeather == 2 ? safeRead(baseAddress) : Promise.resolve(null) // totalPieces
        ]);
    
        // If sizeID is null (error occurred), skip processing this sizeID
        if (sizeID === null) {
          console.warn(`Skipping sizeID ${sizeID} due to read failure`);
          return;
        }
    
        // Leather condition check
        let processedPiecesPerPair = null;
        let processedMaterialLayer = null;
        let processedCuttingDieQty = null;
        let processedTotalPieces = null;
    
        if (modbusClients[ipAddress].sizeDataInfo.isLeather == 1) {
          processedPiecesPerPair = piecesPerPair;
          processedMaterialLayer = materialLayer;
          processedCuttingDieQty = cuttingDieQty;
        } else {
          processedTotalPieces = totalPieces;
        }
    
        console.log(`Processed Data for sizeID ${sizeID}:`, {
          sizeID,
          processedPiecesPerPair,
          processedMaterialLayer,
          processedCuttingDieQty,
          actualCut,
          actualPieces,
          actualSizeQty,
          processedTotalPieces
        });
        await saveActualDataToDB({ 
          OrderID: OrderID,
          IsLeather: modbusClients[ipAddress].sizeDataInfo.isLeather == 2 ? 1 : 0,
          SizeData: [{
              SizeID: sizeID,
              PiecesPerPair: piecesPerPair,
              MaterialLayer: materialLayer,
              CuttingDieQty: cuttingDieQty,
              ActualCut: actualCut,
              ActualPieces: actualPieces,
              ActualSizeQty: actualSizeQty,
              TotalPiecesPerPair: totalPieces 
                }] 
              });
      } catch (error) {
        console.error(`Error processing sizeID ${sizeID}:`, error.message);
        return null; // Continue processing other IDs
      }
    });
    
    // Wait for all promises to resolve
    const results = await Promise.all(promises);
    console.log("Final Results:", results.filter(Boolean)); // Remove failed reads
    

        // Store the data
        // currentData[label] = {
        //   PiecesPerPair: piecesPerPair,
        //   MaterialLayer: materialLayer,
        //   CuttingDieQty: cuttingDieQty,
        //   ActualCut: actualCut,
        //   ActualPieces: actualPieces,
        //   ActualSizeQty: actualSizeQty
        // };

        // Check for changes
        // const prevData = previousData ? previousData[label] : null;
        // const hasChanges = !prevData || (
        //   piecesPerPair !== prevData?.PiecesPerPair ||
        //   materialLayer !== prevData?.MaterialLayer ||
        //   cuttingDieQty !== prevData?.CuttingDieQty ||
        //   actualCut !== prevData?.ActualCut ||
        //   actualPieces !== prevData?.ActualPieces ||
        //   actualSizeQty !== prevData?.ActualSizeQty
        // );

        // if (hasChanges) {
        //   if (piecesPerPair !== 0 || materialLayer !== 0 || cuttingDieQty !== 0 ||
        //       actualCut !== 0 || actualPieces !== 0 || actualSizeQty !== 0) {

        //     console.log(`${label} =`, currentData[label]);

        //     await saveActualDataToDB({
        //       OrderID: OrderID,
        //       SizeData: [{
        //         Size: label,
        //         PiecesPerPair: piecesPerPair,
        //         MaterialLayer: materialLayer,
        //         CuttingDieQty: cuttingDieQty,
        //         ActualCut: actualCut,
        //         ActualPieces: actualPieces,
        //         ActualSizeQty: actualSizeQty
        //       }]
        //     });
        //   }

      //    previousData[label] = { ...currentData[label] };
        
    //   }
    //    catch (err) {
    //     console.error(`Error processing sizeID ${sizeID}:`, err);
    //   }
    // });

    // Wait for all promises to complete
   // await Promise.all(promises);

  } catch (error) {
    console.error(`Error reading actual data: ${error.message}`);
    logToFile(errorLogPath, `Error reading actual data: ${error.message}`);
  }
}

// async function readActualData(client, ipAddress) {
//   try {
//     const sizeLabels = [
//       "10K", "10.5K", "11K", "11.5K", "12K", "12.5K", "13K", "13.5K",
//       "1", "1.5", "2", "2.5", "3", "3.5", "4", "4.5", "5", "5.5",
//       "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5",
//       "11", "11.5", "12", "12.5", "13", "13.5", "14", "14.5"
//     ];

//     const currentData = {};

//     // Đọc OrderID từ thanh ghi 6507
//     const orderIDData = await client.readHoldingRegisters(6507, 1);
//     if (!orderIDData || !orderIDData.response || !orderIDData.response._body) {
//       throw new Error('Không thể đọc OrderID từ thanh ghi 6507');
//     }
//     const OrderID = orderIDData.response._body.values[0];

//     // Lấy PartName từ đối tượng modbusClients
//     const partName = modbusClients[ipAddress]?.partName;
//     console.log(`PartName on readActualData: ${partName}`);

//     const promises = sizeLabels.map((label, i) => {
//       const baseAddress = 6003 + i * 14;
//       return Promise.all([ // Đọc nhiều thanh ghi
//         client.readHoldingRegisters(baseAddress + 2, 1),  // PiecesPerPair
//         client.readHoldingRegisters(baseAddress + 4, 1),  // MaterialLayer
//         client.readHoldingRegisters(baseAddress + 6, 1),  // CuttingDieQty
//         client.readHoldingRegisters(baseAddress + 8, 1),  // ActualCut
//         client.readHoldingRegisters(baseAddress + 10, 1), // ActualPieces
//         client.readHoldingRegisters(baseAddress + 12, 1)  // ActualSizeQty
//       ]).then(async ([piecesPerPairData, materialLayerData, cuttingDieQtyData, actualCutData, actualPiecesData, actualSizeQtyData]) => {

//         // Kiểm tra và xử lý dữ liệu từ Modbus cho mỗi thanh ghi
//         const piecesPerPair = piecesPerPairData?.response?._body?.values[0] ?? 0;
//         const materialLayer = materialLayerData?.response?._body?.values[0] ?? 0;
//         const cuttingDieQty = cuttingDieQtyData?.response?._body?.values[0] ?? 0;
//         const actualCut = actualCutData?.response?._body?.values[0] ?? 0;
//         const actualPieces = actualPiecesData?.response?._body?.values[0] ?? 0;
//         const actualSizeQty = actualSizeQtyData?.response?._body?.values[0] ?? 0;

//         // Lưu dữ liệu vào currentData
//         currentData[label] = {
//           PiecesPerPair: piecesPerPair,
//           MaterialLayer: materialLayer,
//           CuttingDieQty: cuttingDieQty,
//           ActualCut: actualCut,
//           ActualPieces: actualPieces,
//           ActualSizeQty: actualSizeQty
//         };

//         // Kiểm tra sự thay đổi và chỉ lưu nếu có sự thay đổi
//         const prevData = previousData ? previousData[label] : null;
//         let hasChanges = !prevData || (
//           piecesPerPair !== prevData.PiecesPerPair ||
//           materialLayer !== prevData.MaterialLayer ||
//           cuttingDieQty !== prevData.CuttingDieQty ||
//           actualCut !== prevData.ActualCut ||
//           actualPieces !== prevData.ActualPieces ||
//           actualSizeQty !== prevData.ActualSizeQty
//         );

//         if (hasChanges) {
//           if (piecesPerPair !== 0 || materialLayer !== 0 ||
//             cuttingDieQty !== 0 || actualCut !== 0 || actualPieces !== 0 || actualSizeQty !== 0) {

//             console.log(`${label} =`, currentData[label]);

//             await saveActualDataToDB({
//               OrderID: OrderID,
//               SizeData: [{
//                 Size: label,
//                 PiecesPerPair: piecesPerPair,
//                 MaterialLayer: materialLayer,
//                 CuttingDieQty: cuttingDieQty,
//                 ActualCut: actualCut,
//                 ActualPieces: actualPieces,
//                 ActualSizeQty: actualSizeQty
//               }],
//             }, partName);
//           }

//           previousData[label] = {
//             PiecesPerPair: piecesPerPair,
//             MaterialLayer: materialLayer,
//             CuttingDieQty: cuttingDieQty,
//             ActualCut: actualCut,
//             ActualPieces: actualPieces,
//             ActualSizeQty: actualSizeQty
//           };
//         }
//       });
//     });

//     await Promise.all(promises);

//   } catch (error) {
//     console.error(`Error reading actual data: ${error.message}`);
//     logToFile(errorLogPath, `Error reading actual data: ${error.message}`);
//   }
// }
async function writeToModbusRegister(client) {
  try {
    setInterval(async () => {
      counter++;
      await client.writeSingleRegister(8000, counter).catch((err) => {
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

function convert16BitArrayToString(arr) {
  let str = '';

  // Iterate over the array of 16-bit values
  for (let i = 0; i < arr.length; i++) {
    const value = arr[i];

    // Extract low and high bytes
    const lowByte = value & 0xFF; // Get the lower 8 bits
    const highByte = (value >> 8) & 0xFF; // Get the higher 8 bits

    // Convert the low byte to a character
    str += String.fromCharCode(lowByte);

    // If the high byte is non-zero, convert it to a character as well
    if (highByte !== 0) {
      str += String.fromCharCode(highByte);
    }
  }

  return str;
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

    // Write Order Info
    const orderInfo = [data.Model, data.ART, data.SO, data.MasterWorkOrder];
    const orderInfoAddresses = [
      { start: 0, maxRegisters: 25 },
      { start: 25, maxRegisters: 10 },
      { start: 35, maxRegisters: 10 },
      { start: 45, maxRegisters: 10 }
    ];

    const orderInfoPromises = orderInfo.map(async (info, index) => {
      let registerData = stringTo16BitArrayLittleEndian(info);
      const maxRegisters = orderInfoAddresses[index].maxRegisters;
      registerData = registerData.slice(0, maxRegisters * 2);
      const startRegister = orderInfoAddresses[index].start;
      for (let j = 0; j < registerData.length; j++) {
        try {
           console.log(`The covert text:  ${convert16BitArrayToString(registerData)} ${startRegister + j} ${ registerData[j]}`);
          await client.writeSingleRegister(startRegister + j, registerData[j]);
          console.log(`Successfully wrote to register ${startRegister + j}`);
        } catch (error) {
          console.error(`Error writing to register ${startRegister + j}: ${error.message}`);
        }
      }
    });

    await Promise.all(orderInfoPromises);

    // Write Leather Data
    const leatherDataPromise = (async () => {
      try {
        const leatherData = parseInt(data.Leather);
        const startRegister = 1001;
        await client.writeSingleRegister(startRegister, leatherData);
        console.log(`Successfully wrote Leather data to register ${startRegister}`);
      } catch (error) {
        console.error(`Error writing Leather data: ${error.message}`);
      }
    })();
    await leatherDataPromise;

    // Write Material Data
    const materialRegisterAddress = data.Leather == 2 ? 720 : 290;
    const materialCodePromises = data.MaterialData.slice(0, 20).map(async (material) => {
    try {
      // Convert the material code to an array of 16-bit little-endian values
      const materialCodeArray = stringTo16BitArrayLittleEndian(material.MaterialCode);
      
      // Write each 16-bit value to consecutive Modbus registers
      for (let i = 0; i < materialCodeArray.length; i++) {
        const registerAddress = materialRegisterAddress + i; // Increment register address for each 16-bit value
        await client.writeSingleRegister(registerAddress, materialCodeArray[i]);
        console.log(`Successfully wrote materialCode ${materialCodeArray[i]} to register ${registerAddress}`);
      }
    } catch (error) {
      console.error(`Error writing materialCode: ${error.message}`);
    }
    });

    await Promise.all(materialCodePromises);

      // Write partID
      const partIDRegisterAddress = data.Leather == 2 ? 730 : 300;
      const partIDPromises = data.MaterialData.slice(0, 20).map(async (material) => {
      try {
        // Convert the material code to an array of 16-bit little-endian values
        const partIDCodeArray = stringTo16BitArrayLittleEndian(material.PartID);
        
        // Write each 16-bit value to consecutive Modbus registers
        for (let i = 0; i < partIDCodeArray.length; i++) {
          const registerAddress = partIDRegisterAddress + i; // Increment register address for each 16-bit value
          await client.writeSingleRegister(registerAddress, partIDCodeArray[i]);
          console.log(`Successfully wrote partID ${partIDCodeArray[i]} to register ${registerAddress}`);
        }
      } catch (error) {
        console.error(`Error writing partID: ${error.message}`);
      }
      });
    
    await Promise.all(partIDPromises);
    

    // Write Part Names
    const partNameStartRegister = data.Leather == 2 ? 320 : 270;
    const partNamePromises = data.MaterialData.slice(0, 20).map(async (material, i) => {
      try {
        const partName = material.PartName;
        const startRegister = partNameStartRegister + (i * 20);
        let registerData = stringTo16BitArrayLittleEndian(partName).slice(0, 10 * 2);
        for (let j = 0; j < registerData.length; j++) {
          await client.writeSingleRegister(startRegister + j, registerData[j]);
          console.log(`Successfully wrote part name to register ${startRegister + j}`);
        }
      } catch (error) {
        console.error(`Error writing part name: ${error.message}`);
      }
    });

    await Promise.all(partNamePromises);
    
    // Write defaultValue
    if (data.Leather == 1) {
      const BASE_REGISTER = 886; 
      const defaultValue = data.DefaultValue;
    
      for (const item of defaultValue) {
        try {
          await client.writeSingleRegister(BASE_REGISTER, item.PiecesPerPair);
          await client.writeSingleRegister(BASE_REGISTER + 4, item.MaterialLayer);
          await client.writeSingleRegister(BASE_REGISTER + 8, item.CuttingDieQty);
          console.log("✅ Successfully wrote to Modbus registers (Leather == 1)");
        } catch (error) {
          console.error("❌ Error writing to Modbus registers (Leather == 1): ", error);
        }
      }
    } else {
      const BASE_REGISTER = 966;
      const defaultValue = data.DefaultValue; // Đảm bảo có dữ liệu để lặp qua
    
      for (const item of defaultValue) {
        try {
          await client.writeSingleRegister(BASE_REGISTER, item.TotalPiecesPerPair);
          console.log("✅ Successfully wrote to Modbus registers (Leather != 1)");
        } catch (error) {
          console.error("❌ Error writing to Modbus registers (Leather != 1): ", error);
        }
      }
    }
    // Write SizeData
    await writeRegisterSizeData(client , ipAddress, data.SizeData, data.Leather);
    console.log('Data successfully saved to Modbus');
  } catch (error) {
    console.error(`Error saving distribution data to Modbus: ${error.message}`);
    throw new Error(`Failed to save distribution data: ${error.message}`);
  }
}


// Ghi size data vào thanh ghi 
async function writeRegisterSizeData(client, ipAddress, sizeData, isLeather) {
  // kiểm tra nếu null or > 6 size
  if(Array.isArray(sizeData) && sizeData.length > 6) 
    return;

  // Ensure sizeDataInfo exists
  modbusClients[ipAddress] ||= {};              
  modbusClients[ipAddress].sizeDataInfo ||= {};

  // Assign values correctly
  modbusClients[ipAddress].sizeDataInfo.sizeCount = sizeData.length;
  modbusClients[ipAddress].sizeDataInfo.isLeather = isLeather;

  console.log(`[sizeDataInfo] Number of size ${modbusClients[ipAddress].sizeDataInfo.sizeCount} and isLeather ${isLeather}`)

  let registerSize = [];
  if (isLeather == 2 && sizeData.length <= 3) {
    // register number of size
    // size leather
    await client.writeSingleRegister(1003, sizeData.length);
    registerSize = [
      { SizeID: 900, Size: 906, SizeQty: 918, InventoryQty: 79 },
      { SizeID: 902, Size: 910, SizeQty: 934, InventoryQty: 83 },
      { SizeID: 904, Size: 914, SizeQty: 950, InventoryQty: 87 },
    ];
  } else {
    // size Raw
    await client.writeSingleRegister(1002, sizeData.length);
    registerSize = [
      { SizeID: 750, Size: 762, SizeQty: 790, InventoryQty: 55 },
      { SizeID: 752, Size: 766, SizeQty: 806, InventoryQty: 59 },
      { SizeID: 754, Size: 770, SizeQty: 822, InventoryQty: 63 },
      { SizeID: 756, Size: 774, SizeQty: 838, InventoryQty: 67 },
      { SizeID: 758, Size: 778, SizeQty: 854, InventoryQty: 71 },
      { SizeID: 760, Size: 782, SizeQty: 870, InventoryQty: 75 },
    ];
  }
  for (let i = 0; i < sizeData.length; i++) {
    const item = sizeData[i];

    // Validate the values before writing
    const isValidValue = (value) => typeof value === 'number' && value >= 0 && value <= 65535;

    // Logging to debug the values
    console.log(`Writing to Modbus: SizeID = ${item.SizeID}, Size = ${item.Size}, SizeQty = ${item.SizeQty}, InventoryQty = ${item.InventoryQty}`);

    if (!item.Size || typeof item.Size !== 'string') {
      console.error(`Invalid Size value: ${item.Size}`);
      continue;
    }
    if (
      !isValidValue(item.SizeID) ||
      !isValidValue(item.SizeQty) ||
      !isValidValue(item.InventoryQty)
    ) {
      console.error(`Invalid value detected for SizeID ${item.SizeID}: Values must be within the range 0-65535.`);
      continue;  // Skip
    }

    try {
      // Write SizeID, Size, SizeQty, and InventoryQty to the corresponding Modbus registers
      let registerSizeData = stringTo16BitArrayLittleEndian(item.Size).slice(0, 10 * 2);
      const startSizeRegister = registerSize[i].Size; 
      await client.writeSingleRegister(registerSize[i].SizeID, item.SizeID);

      // push sizeID
      if (!Array.isArray(modbusClients[ipAddress].sizeDataInfo.sizeID)) {
        modbusClients[ipAddress].sizeDataInfo.sizeID = [];
      }
      if (!modbusClients[ipAddress].sizeDataInfo.sizeID.includes(registerSize[i].SizeID)) {
        modbusClients[ipAddress].sizeDataInfo.sizeID.push(registerSize[i].SizeID);
        console.log(`[sizeDataInfo] sizeID ${registerSize[i].SizeID}`)
      }      

      console.log(`Writing to register ${registerSize[i].SizeID}, value: ${item.SizeID}`);
      for (let j = 0; j < registerSizeData.length; j++) {
        console.log(`Writing to register ${startSizeRegister + j}, value: ${registerSizeData[j]}`);
        await client.writeSingleRegister(startSizeRegister + j, registerSizeData[j]);
      }             

      console.log(`Writing to register ${registerSize[i].SizeQty}, value: ${item.SizeQty}`);
      console.log(`Writing to register ${registerSize[i].InventoryQty}, value: ${item.InventoryQty}`);

      await client.writeSingleRegister(registerSize[i].SizeQty, item.SizeQty || 0);    
      await client.writeSingleRegister(registerSize[i].InventoryQty, item.InventoryQty  || 0); 
      console.log(`Successfully written to Modbus for SizeID ${item.SizeID}`);
    } catch (error) {
      console.error(`Error writing to Modbus for SizeID ${item.SizeID}:`, error);
    }
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
async function isHostReachable(ipAddress, port = 502) { // Default Modbus port
  return new Promise((resolve) => {
    const socket = new net.Socket();

    socket.setTimeout(1000); // 1-second timeout

    socket.on('connect', () => {
      socket.destroy();
      resolve(true); // Host is reachable
    });

    socket.on('timeout', () => {
      socket.destroy();
      resolve(false); // Timeout reached
    });

    socket.on('error', () => {
      resolve(false); // Error means not reachable
    });

    socket.connect(port, ipAddress);
  });
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
  isHostReachable,
  modbusClients,
};
