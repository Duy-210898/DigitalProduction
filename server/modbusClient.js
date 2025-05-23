const fs = require('fs');
const net = require('net');
const Modbus = require('jsmodbus');
const ping = require('ping');
const { updateDeviceConnectionStatus, getSizeDataFromDB, getDistributionDataFromDb, saveActualDataToDB, setOrderIsComplete, setDistributionIsComplete, getSizeAndDistributionDataFromDb, getDistributionIDFromSizeID, getDistributionCompleteFromDb} = require('./database');
const { notifyClientsToDeleteOrder } = require('./notifications');

let countRemainSizeData = {};
let storeDistributionData = {};
// let previousData = {};  // Store previous data for comparison

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
  else {
    // avoid status connect device is turn off
    console.log(`Update ${ipAddress} is reachable.`);
    await updateDeviceConnectionStatus(ipAddress, true);
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

              // reset index 
              modbusClients[ipAddress].indexMultipleSOs = 0;
              modbusClients[ipAddress].indexMultiplePartNames = 0;
              
              delete modbusClients[ipAddress].client;
              delete modbusClients[ipAddress].socket;
          }

          setTimeout(() => connectToDevice(ipAddress, retries + 1).then(resolve).catch(reject), 10000);
          await handleDisconnection(ipAddress);
      });
  });
}

async function startReadingRegisters(client, ipAddress) {
  if (modbusClients[ipAddress].readerLoopStarted) return;
  modbusClients[ipAddress].readerLoopStarted = true;

  setInterval(async () => {
    try {
      const modbusClient = modbusClients[ipAddress];
      if (!modbusClient || !modbusClient.isConnected || !modbusClient.client) return;

      const socket = modbusClient.socket;
      if (!socket || socket.destroyed) {
        console.warn(`[${ipAddress}] Socket is closed or destroyed. Skipping read.`);
        return;
      }

      // Read only if everything is healthy
      await readOperatorID(client, ipAddress);
      await checkAndSaveDistribution(client, ipAddress);

      const sizeDataInfo = modbusClient.sizeDataInfo;
      if (sizeDataInfo && typeof sizeDataInfo === 'object' && Object.keys(sizeDataInfo).length > 0) {
        await readActualData(client, ipAddress);
      }

    } catch (err) {
      console.error(`[${ipAddress}] Error in reading loop: ${err.message}`);
    }
  }, 1000);
}

async function checkAndSaveDistribution(client, ipAddress) {
  try {
    // Read register 1000
    let response = await client.readHoldingRegisters(1000, 1);
    let orderID = response.response._body.values[0];
    console.log(`Register 1000 value: ${orderID}`);

    // check complete order
    let responseLeather = await client.readHoldingRegisters(1001, 1);
    let isLeather = responseLeather.response._body.values[0];
    console.log(`Register 1001 value: ${isLeather}`);
    // check register 3000 status
    checkBitOnOffRegister3000(client, ipAddress);

    if (orderID === 0) {
      console.log(`Register 1000 is 0. Fetching distribution data for IP: ${ipAddress}`);

      // Fetch distribution data from DB
      const distributionData = await getDistributionDataFromDb(ipAddress);
      processDistributionData(client, ipAddress, distributionData, false);
    }
    else {
    console.log(`Register 1000 already has value ${orderID} for IP ${ipAddress}`);

    // Store orderID in modbusClients
    if (!modbusClients[ipAddress]) {
      modbusClients[ipAddress] = {};
    }
    countRemainSizeData[ipAddress] = { index: 0 }; 
    modbusClients[ipAddress].orderID = orderID;

    // interrupt HMI set distributionData again
    if (!modbusClients[ipAddress].sizeDataInfo ||
       typeof modbusClients[ipAddress].sizeDataInfo !== 'object' || 
       Object.keys(modbusClients[ipAddress].sizeDataInfo).length === 0) {
        const distributionData = await getDistributionDataFromDb(ipAddress);
        if(distributionData == null) {
          // delete if complete order
          const response = await client.readHoldingRegisters(3000, 1);
          const registerValue = response.response._body.values[0];
          const mask = 1 << 2;
          const valueToWrite = registerValue | mask;

          await client.writeSingleRegister(3000, valueToWrite);
          storeDistributionData[ipAddress] = {};
          modbusClients[ipAddress].previousSizeData = [];
          modbusClients[ipAddress].previousData = {};
          modbusClients[ipAddress].indexMultipleSOs = 0;
          modbusClients[ipAddress].indexMultiplePartNames = 0;
          modbusClients[ipAddress].sizeDataInfo.sizeID = [];
        }
        processDistributionData(client, ipAddress, distributionData, true);
    }
  
    // Ensure checkDelete exists and is initialized to 0
    if (!storeDistributionData[ipAddress]) {
      storeDistributionData[ipAddress] = {};
    }
    
    //retry get storeDistributionData
    if (Object.keys(storeDistributionData[ipAddress]).length === 0) {
      await fetchStoreDistributionData(ipAddress, orderID, isLeather);
    }
    if (storeDistributionData[ipAddress] == null) { return }
    try {

      // check complete size mutiple SO
      const sizeData = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress]?.indexMultipleSOs]?.Data;
      
      const previousCompletedOrders = [];

      if (sizeData != null) {

        // // Continue reading size
        // const response = await client.readHoldingRegisters(sizeAddress, 1);
        // const registerValue = response.response._body.values[0];

        // // Convert the registerValue to a binary string (16-bit)
        // let binaryValue = registerValue.toString(2).padStart(16, '0');
        // console.log(`Output of size at register address ${sizeAddress} = ${registerValue}`);
        // console.log(`Binary representation: ${binaryValue}`);

        // // Count '1' bits in the binary string
        // let countCompleteSize = binaryValue.split('').filter(bit => bit === '1').length;
        // console.log("[Size] Count size of '1' bits:", countCompleteSize);

        // const sizeIDList = modbusClients[ipAddress]?.sizeDataInfo?.sizeID;

        // if (Array.isArray(sizeIDList) && countCompleteSize === sizeIDList.length) {

        //   const data = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data;
        //   modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs].Data = data
        //   .filter(item => item.Status !== 'Complete' && item.Status !== 'Stop');
        //   const response = await client.readHoldingRegisters(3000, 1);
        //   const registerValue = response.response._body.values[0];
        //   const mask = 1 << 2;
        //   const valueToWrite = registerValue | mask;

        //   await client.writeSingleRegister(3000, valueToWrite);
        //   storeDistributionData[ipAddress] = {};
        //   modbusClients[ipAddress].previousSizeData = [];
        //   modbusClients[ipAddress].previousData = {};
        //   modbusClients[ipAddress].indexMultipleSOs = 0;
        //   modbusClients[ipAddress].indexMultiplePartNames = 0;
        //   modbusClients[ipAddress].sizeDataInfo.sizeID = [];
        //   return;
        // }

        // Get OrderID and SizeID where status is 'Complete'
        const completedOrders = sizeData
          .filter(item => item.Status === 'Complete')
          .map(item => ({
            OrderID: item.OrderID,
            SizeID: item.SizeID,
            PartID: item.PartID
          }));

      if (completedOrders.length > 0) {
        for (const completeOrder of completedOrders) {
          // Check if this OrderID and SizeID already processed
          const alreadyProcessed = previousCompletedOrders.some(prev =>
            prev.OrderID === completeOrder.OrderID &&
            prev.SizeID === completeOrder.SizeID && 
            prev.PartID === completeOrder.PartID
          );

          if (alreadyProcessed) {
            console.log(`⏭️ Skipped already completed OrderID: ${completeOrder.OrderID}, SizeID: ${completeOrder.SizeID}, PartID: ${completeOrder.PartID}`);
            continue;
          }
          const distributionIDFromSize = await getDistributionIDFromSizeID(
            ipAddress,
            completeOrder.OrderID,
            isLeather === 1 ? 0 : 1,
            completeOrder.SizeID,
            completeOrder.PartID
          );

          if (distributionIDFromSize?.DistributionID?.length > 0) {
            try {
              const { DistributionID } = distributionIDFromSize.DistributionID[0];
              
              await setDistributionIsComplete(DistributionID, 'Complete');
              console.log(`✅ Completed DistributionID: ${DistributionID}`);

              // Save to previousCompletedOrders for tracking
              previousCompletedOrders.push({
                DistributionID,
                OrderID: completeOrder.OrderID,
                SizeID: completeOrder.SizeID,
                PartID: completeOrder.PartID
              });
            } catch (error) {
              console.error(
                `❌ Error updating DistributionID: ${distributionIDFromSize?.DistributionID[0]?.DistributionID || 'Unknown'}`,
                error
              );
            }
          }
        }
      } else {
          console.log("⚠️ No completed orders found in current sizeData.");
        }
      }
      // Check choose size
      // const responseChooseSize = await client.readHoldingRegisters(chooseSizeAddress, 1);
      // const registerChooseSizeValue = responseChooseSize.response._body.values[0];

      // let binaryChooseSizeValue = registerChooseSizeValue.toString(2).padStart(16, '0');
      // console.log(`Output of size at register address ${chooseSizeAddress} = ${registerChooseSizeValue}`);
      // console.log(`Binary representation: ${binaryChooseSizeValue}`);

      // const index = findSetBitIndex(binaryChooseSizeValue);
      // if (index !== -1) {
      //   let distributionIDFromSize  =  await getDistributionIDFromSizeID(
      //     ipAddress, orderID, isLeather === 1 ? 0 : 1, storeDistributionData[ipAddress].SizeData[index].SizeID);
      //   if (distributionIDFromSize !== null && 
      //       Array.isArray(modbusClients[ipAddress].sizeCompleteID) && 
      //       modbusClients[ipAddress].sizeCompleteID.length > 0 &&
      //       storeDistributionData[ipAddress].SizeData[index].SizeID !== 0 &&
      //       modbusClients[ipAddress].sizeCompleteID.includes(storeDistributionData[ipAddress].SizeData[index].SizeID))
      //     {
      //       for (const item of distributionIDFromSize.DistributionID) {
      //         try {
      //             await setDistributionIsComplete(item.DistributionID, 'Complete');
      //             console.log(`Complete DistributionID: ${item.DistributionID}`);
      //         } catch (error) {
      //             console.error(`Error updating DistributionID: ${item.DistributionID}`, error);
      //         }
      //       }
      //     }
      //   } 

       // let distributionComplete = await getDistributionCompleteFromDb(ipAddress, orderID, isLeather === 1 ? 0 : 1);
        // if (storeDistributionData[ipAddress]?.SizeData) {
        //     if (distributionComplete?.length && storeDistributionData[ipAddress].SizeData.length === distributionComplete.length) {
        //         // Get the remaining SizeData values
        //         console.log(`Complete due to reason: Size ${storeDistributionData[ipAddress].SizeData.length} and Completed ${distributionComplete.length}`);
        //         modbusClients[ipAddress].isComplete = 0;
        //         modbusClients[ipAddress].sizeDataInfo.sizeID = [];
        //         previousData[ipAddress] = {};
        //     } else {
        //         try {
        //             // Continue reading size
        //             const response = await client.readHoldingRegisters(sizeAddress, 1);
        //             const registerValue = response.response._body.values[0];

        //             // Convert the registerValue to a binary string (16-bit)
        //             let binaryValue = registerValue.toString(2).padStart(16, '0');
        //             console.log(`Output of size at register address ${sizeAddress} = ${registerValue}`);
        //             console.log(`Binary representation: ${binaryValue}`);

        //             // Count '1' bits in the binary string
        //             let countCompleteSize = binaryValue.split('').filter(bit => bit === '1').length;
        //             console.log("[Size] Count size of '1' bits:", countCompleteSize);

        //             // check size equal to complete
        //             results.push({ address: sizeAddress, value: registerValue });
        //             // If all sizes are complete, update DistributionIDs
        //             if (countCompleteSize === storeDistributionData[ipAddress].SizeData.length && countCompleteSize === modbusClients[ipAddress].isComplete && Array.isArray(storeDistributionData[ipAddress].DistributionID)) {
        //                 for (const item of storeDistributionData[ipAddress].DistributionID) {
        //                     try {
        //                         console.log(`Updated DistributionID: ${item.DistributionID}, countCompleteSize: ${countCompleteSize}, distribution.SizeData: ${storeDistributionData[ipAddress].SizeData.length}`);
        //                         // Reset sizeID and delete from register
        //                         modbusClients[ipAddress].isComplete = 0;
        //                         modbusClients[ipAddress].sizeDataInfo.sizeID = []; 
        //                         previousData[ipAddress] = {};
        //                     } catch (error) {
        //                         console.error(`Error updating DistributionID: ${item.DistributionID}`, error);
        //                     }
        //                 }
        //             }

        //             console.log("Register Values:", results);
        //         } catch (error) {
        //             console.error("Error reading size register:", error);
        //         }
        //     }
        // } else {
        //     console.log("No valid size data found.");
        // }
      } catch (error) {
          console.error(`Error reading register ${modbusClients[ipAddress].sizeAddress}:`, error.message);
      }        
    }

  } catch (error) {
    const errorMsg = `❌ Error reading register 1000 for IP ${ipAddress}: ${error.message}`; 
    console.error(errorMsg);
    logToFile(errorLogPath, errorMsg);
  }
}

async function processDistributionData(client, ipAddress, distributionData, retryData) {
  if (!distributionData) {
    console.warn(`No distribution data found for IP ${ipAddress}. Skipping save.`);
    return;
  }

  // let index = await safeRead(1020, client);

  // modbusClients[ipAddress].indexMultipleSOs = index >= 1 ? index - 1 : 0;

  const writeRegister = async (address, value) => {
    await client.writeSingleRegister(address, value);
   // await delay(1000);
  };

  if (!retryData) {
    // Write order ID to register 1000
    await writeRegister(1000, distributionData.OrderID);
  }
  modbusClients[ipAddress].hasMultipleSOs = false;

  // Check for multiple Sales Orders
  const hasMultipleSOs = distributionData.OrderIDWithSOs && distributionData.OrderIDWithSOs.length >= 1;
  if (hasMultipleSOs) {
    if (!modbusClients[ipAddress]?.indexMultipleSOs || modbusClients[ipAddress].indexMultipleSOs === 0 || modbusClients[ipAddress].indexMultipleSOs == undefined) {
      modbusClients[ipAddress].indexMultipleSOs = 0;
    }
    modbusClients[ipAddress].hasMultipleSOs = true;

    // Reset SizeDataDB Status if it's an array
    if (Array.isArray(distributionData.SizeDataDB)) {
      distributionData.SizeDataDB = distributionData.SizeDataDB.map(sizeItem => ({
        ...sizeItem,
        Status: false,
      }));
    }

    // Assign Sales Orders
    modbusClients[ipAddress].SOs = distributionData.OrderIDWithSOs;

    const totalSOs = distributionData.OrderIDWithSOs.length;

    await writeRegister(1021, totalSOs);
    console.log(`Successfully wrote TotalSOs ${totalSOs} to register 112`);
    await writeRegister(1020, modbusClients[ipAddress].indexMultipleSOs + 1);
    console.log(`Successfully wrote DefaultOrderIndex ${modbusClients[ipAddress].indexMultipleSOs} to register 111`);

    if (distributionData?.SizeData && modbusClients[ipAddress]?.SOs
                  && modbusClients[ipAddress].SOs.length > 0 && Array.isArray(modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs]?.Data)) {
      const sizeData = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs].Data;

      // Use a Map to consolidate sizes
      const sizeTotals = sizeData.reduce((acc, item) => {
        const key = `${item.SizeID}-${item.PartName}`;
        if (!acc.has(key)) {
          acc.set(key, { SizeID: item.SizeID, Size: item.Size, SizeQty: 0, InventoryQty: item.InventoryQty, PartName: item.PartName });
        }
        acc.get(key).SizeQty += item.SizeQty;
        return acc;
      }, new Map());

      // Convert Map to an array
      distributionData.SizeData = Array.from(sizeTotals.values());
      console.log("Total Sizes:", distributionData.SizeData);

      const registerSoAddress = [
        35, 40, 117, 122, 127, 132, 137, 142, 147, 152,
        157, 162, 167, 172, 177, 182, 187, 192, 197, 202
      ];
      
      // Write SOs to register addresses
      const uniqueSOs = [...new Set(modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs].Data.map(so => so.SO))];
      console.log(`Unique SOs:`, uniqueSOs);

      let currentRegisterIndex = 0;
      if(uniqueSOs.length > 0) {
        for (let i = 0; i < uniqueSOs.length; i++) {
            try {
                // Convert SO to 5-bit values
                const registerValues = stringTo16BitArrayLittleEndian(uniqueSOs[i]); 
                for (let j = 0; j < registerValues.length; j++) {
                    const registerAddress = registerSoAddress[i] + j; 
                    await client.writeSingleRegister(registerAddress, registerValues[j]);
                    console.log(`Wrote SO ${uniqueSOs[i]} part to register ${registerAddress}: ${registerValues[j]}`);
                }
                // Move to the next available register set
                currentRegisterIndex += registerValues.length;
            } catch (error) {
                console.error(`Error writing SO ${uniqueSOs[i]} to register: ${error.message}`);
            }
        }
    }
    // Write PartName to register addresses
    const uniquePartSOsMap = [
      ...new Set(
        modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs].Data.map(p => `${p.PartName}|${p.PartID}`)
      )
    ].map(item => {
      const [PartName, PartID] = item.split('|');
      return { PartName, PartID: parseInt(PartID) }; // Convert PartID to integer if needed
    });
    console.log(`Unique Part:`, uniquePartSOsMap);

    // assign PartName position to switch out
    if (uniquePartSOsMap.length > 0) {
      if (!modbusClients[ipAddress]?.indexMultiplePartNames || modbusClients[ipAddress].indexMultiplePartNames === 0) {
        modbusClients[ipAddress].indexMultiplePartNames = 0;
      }

      try {

        // write register partname position
        await client.writeSingleRegister(1022, modbusClients[ipAddress].indexMultiplePartNames + 1);
        await client.writeSingleRegister(1023, uniquePartSOsMap.length);

        console.log(`Next value PartName:`, modbusClients[ipAddress].indexMultiplePartNames + 1);

         // display partName name
         const partDisplayStartRegister = distributionData.Leather === 2 ? 115 : 270;

        // filter out partName index
        if (distributionData.Leather === 2) {
          distributionData.SizeData = distributionData.SizeDataDB;
          await client.writeSingleRegister(partDisplayStartRegister, uniquePartSOsMap.length);
        }
        else {
          const selectedPart = uniquePartSOsMap[modbusClients[ipAddress].indexMultiplePartNames];
          distributionData.SizeData = distributionData.SizeData.filter(item => item.PartName === selectedPart.PartName);
          let registerDisplayData = stringTo16BitArrayLittleEndian(selectedPart.PartName).slice(0, 10 * 2);
          // Write part name to registers sequentially
          for (let j = 0; j < registerDisplayData.length; j++) {
            await client.writeSingleRegister(partDisplayStartRegister + j, registerDisplayData[j]);
            console.log(`Successfully wrote part name display to register ${partDisplayStartRegister + j}`);
          }
        }
        // Write part names to multiple registers starting at 320 sequentially
        const partNameStartRegister = 320;
        for (let i = 0; i < uniquePartSOsMap.slice(0, 20).length; i++) {
          const part = uniquePartSOsMap[i];
          try {
            const startRegister = partNameStartRegister + (i * 20);
            let registerData = stringTo16BitArrayLittleEndian(part.PartName).slice(0, 10 * 2);
            for (let j = 0; j < registerData.length; j++) {
              await client.writeSingleRegister(startRegister + j, registerData[j]);
              console.log(`Successfully wrote part name to register ${startRegister + j}`);
            }
          } catch (error) {
            console.error(`Error writing part name: ${error.message}`);
          }
        }
        // Write partID using distributionData.MaterialData sequentially
        let partIDRegisterAddress;
        let partList = Object.values(uniquePartSOsMap);

        if (distributionData.Leather === 2) {
          partIDRegisterAddress = 91;     
          for (let i = 0; i < partList.length; i++) {
            const part = partList[i];
            try {
              const partID = part.PartID;
              const registerAddress = partIDRegisterAddress + i;
              await client.writeSingleRegister(registerAddress, partID);
              console.log(`✅ Wrote PartID ${partID} to register ${registerAddress}`);
            } catch (error) {
              console.error(`❌ Error writing PartID ${part?.PartID} at index ${i}:`, error.message);
            }
          }     
        } else {
          const index = modbusClients[ipAddress]?.indexMultiplePartNames;
          partList = partList[index];
          partIDRegisterAddress = 300;
            try {
              const registerAddress = partIDRegisterAddress;
              await client.writeSingleRegister(registerAddress, partList.PartID);
              console.log(`✅ Wrote PartID ${partList.PartID} to register ${registerAddress}`);
            } catch (error) {
              console.error(`❌ Error writing PartID ${part?.PartID} at index ${i}:`, error.message);
          }
        }
      
      } catch (error) {
        console.error(`Error writing registers: ${error.message}`);
      }
     }
    }
    console.log('Saving distribution data to Modbus...');
    await saveDistributionDataToModbus(ipAddress, distributionData);
  } else {
    console.warn(`No multiple Sales Orders detected for IP ${ipAddress}. Data will not be processed.`);
  }
}
function indexCompleteOnes(binaryString) {
  let count = binaryString.split('').filter(bit => bit === '1').length;

  if (binaryString === '0000000000000000') {
      return -1; // Return -1 if all bits are 0
  }
  return count > 1 ? count - 1 : 0; // If at least two '1's exist, return count - 1, otherwise return 0
}

async function fetchStoreDistributionData(ipAddress, orderID, isLeather) {
  while (true) {
      if (Object.keys(storeDistributionData[ipAddress]).length === 0) {
          if (isLeather === 1) {
              modbusClients[ipAddress].chooseSizeAddress = 3101;
              modbusClients[ipAddress].sizeAddress = 3102;
              storeDistributionData[ipAddress] = await getSizeAndDistributionDataFromDb(ipAddress, orderID, isLeather == 1 ? 0 : 1);
          } else {
            modbusClients[ipAddress].chooseSizeAddress = 3103;
            modbusClients[ipAddress].sizeAddress = 3104;
            storeDistributionData[ipAddress] = await getSizeAndDistributionDataFromDb(ipAddress, orderID, isLeather == 1 ? 0 : 1);
          }
          return storeDistributionData[ipAddress]; // Exit loop after fetching data
      } else {
          // If data exists, you might want to log or handle this case differently
          console.log('Data already exists for IP Address:', ipAddress);
          
          // If you want to exit or do something specific for existing data, do it here
          return storeDistributionData[ipAddress]; // Return existing data
      }
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
async function checkBitOnOffRegister3000(client, ipAddress) {
  let register3000Address = 3000;
  const previousSOIndex = 4;
  const deleteIndex = 2;
  const nextSOIndex = 6;
  const previousPartNameIndex = 8;
  const nextPartNameIndex = 10;
  try {
    const response = await client.readHoldingRegisters(register3000Address, 1);
    const registerValue = response.response._body.values[0];

    // Convert the registerValue to a binary string with leading zeros
    let binaryValue3000 = registerValue.toString(2).padStart(16, '0');  // assuming 16-bit register, adjust if needed

    // Log the current value and binary representation
    console.log(`Register value ${register3000Address} = ${registerValue}`);
    console.log(`Binary 3000: ${binaryValue3000}`);
     // delete if complete order
    if (modbusClients[ipAddress].SOs !== undefined && modbusClients[ipAddress].SOs.length !== 0) {
      const distributionData = await getDistributionDataFromDb(ipAddress);
      const data = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data;

      let isPendingSize = false;
      if (distributionData != null) {
        isPendingSize = distributionData.OrderIDWithSOs.length >  modbusClients[ipAddress].SOs.length;
      }
      const allComplete = Array.isArray(data)
        ?  data.every(item => item.Status === 'Complete' || item.Status === 'Stop')
        : false;

      if (allComplete || distributionData == null || isPendingSize) {
        const mask = 1 << deleteIndex;
        const valueToWrite = registerValue | mask;
    
        await client.writeSingleRegister(register3000Address, valueToWrite);
        storeDistributionData[ipAddress] = {};
        modbusClients[ipAddress].previousSizeData = [];
        modbusClients[ipAddress].previousData = {};
        modbusClients[ipAddress].indexMultipleSOs = 0;
        modbusClients[ipAddress].indexMultiplePartNames = 0;
        modbusClients[ipAddress].sizeDataInfo.sizeID = [];
      }
    }
    
    if (isBitOn(binaryValue3000, previousSOIndex)) {
      const mask = 1 << deleteIndex;
      const valueToWrite = registerValue | mask;
      let previous = modbusClients[ipAddress].indexMultipleSOs;
      if (previous <= 0) {
        previous = 0;
      } else {
        previous = modbusClients[ipAddress].indexMultipleSOs - 1;
      }
    
      modbusClients[ipAddress].indexMultipleSOs = previous;
      console.log(`Previous value ${previous} to register 3000 ${ipAddress}`);
    
      await client.writeSingleRegister(register3000Address, valueToWrite);
      storeDistributionData[ipAddress] = {};
      modbusClients[ipAddress].previousSizeData = [];
      modbusClients[ipAddress].previousData = {};
    }
    
    if (isBitOn(binaryValue3000, nextSOIndex)) {
      const mask = 1 << deleteIndex;
      const valueToWrite = registerValue | mask;
    
      const maxSOs = modbusClients[ipAddress].SOs.length;
      let next = modbusClients[ipAddress].indexMultipleSOs;
      console.log(`Before Next value ${next} to register 3000 address ${ipAddress}`);
      if (next >= maxSOs) {
        next = maxSOs - 1;
      } else {
        next = modbusClients[ipAddress].indexMultipleSOs + 1;
      }
    
      console.log(`Next value ${next} to register 3000 address ${ipAddress}`);
    
      modbusClients[ipAddress].indexMultipleSOs = next;
      await client.writeSingleRegister(register3000Address, valueToWrite);
      storeDistributionData[ipAddress] = {};
      modbusClients[ipAddress].previousSizeData = [];
      modbusClients[ipAddress].previousData = {};
    }
    if (modbusClients[ipAddress].lockPartNameAdvance) {
      console.warn(`Action ignored: still processing for ${ipAddress}`);
      return;
    }
    
    if (modbusClients[ipAddress].lockPartNameAdvance) {
      console.warn(`Action ignored: still processing for ${ipAddress}`);
      return;
    }
    
    modbusClients[ipAddress].lockPartNameAdvance = true;
    
    try {

      if (modbusClients[ipAddress].SOs === undefined) {
        const mask = 1 << deleteIndex;
        const valueToWrite = registerValue | mask;
        await client.writeSingleRegister(register3000Address, valueToWrite);
        modbusClients[ipAddress].indexMultipleSOs = 0;
        modbusClients[ipAddress].indexMultiplePartNames = 0;

        await client.writeSingleRegister(1020, 1);
        await client.writeSingleRegister(1021, 1);
        return;
      };
      // === Previous Logic ===
      if (isBitOn(binaryValue3000, previousPartNameIndex)) {
        const mask = 1 << deleteIndex;
        const valueToWrite = registerValue | mask;
    
        let previous = modbusClients[ipAddress].indexMultiplePartNames;
        previous = previous > 0 ? previous - 1 : 0;
    
        modbusClients[ipAddress].indexMultiplePartNames = previous;
        await client.writeSingleRegister(register3000Address, valueToWrite);
    
        console.log(`Previous value ${previous} to register 3000 address ${ipAddress}`);
    
        storeDistributionData[ipAddress] = {};
        modbusClients[ipAddress].previousSizeData = [];
        modbusClients[ipAddress].previousData = {};
      }
    
      // === Next Logic ===
      if (isBitOn(binaryValue3000, nextPartNameIndex)) {
        const mask = 1 << deleteIndex;
        const valueToWrite = registerValue | mask;
    
        const currentSO = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
        let uniquePartNameSOs = [];
    
        if (currentSO && Array.isArray(currentSO.Data)) {
          uniquePartNameSOs = [...new Set(currentSO.Data.map(p => p.PartName))];
          console.log(`Unique PartName Store:`, uniquePartNameSOs);
        } else {
          console.warn(`currentSO or currentSO.Data is undefined for IP: ${ipAddress}`);
        }
    
        const maxPartNames = uniquePartNameSOs.length;
        let next = modbusClients[ipAddress].indexMultiplePartNames;
    
        if (next + 1 >= maxPartNames) {
          next = maxPartNames - 1;
        } else {
          next = next + 1;
        }
    
        modbusClients[ipAddress].indexMultiplePartNames = next;
        await client.writeSingleRegister(register3000Address, valueToWrite);
        console.log(`Next value ${next} maxPartNames:${maxPartNames} to register 3000 address ${ipAddress}`);
    
        storeDistributionData[ipAddress] = {};
        modbusClients[ipAddress].previousSizeData = [];
        modbusClients[ipAddress].previousData = {};
      }
    } catch (error) {
      console.error(`Error processing part name change for ${ipAddress}:`, error);
    } finally {
      modbusClients[ipAddress].lockPartNameAdvance = false;
    }
    
    

    // check max size SOS index, subtract - 1 element index
    if(modbusClients[ipAddress].SOs !== undefined) {
      if (modbusClients[ipAddress].SOs.length === 0) {
        modbusClients[ipAddress].indexMultipleSOs = 0;
      } else if (modbusClients[ipAddress].indexMultipleSOs >= modbusClients[ipAddress].SOs.length) {
        modbusClients[ipAddress].indexMultipleSOs = modbusClients[ipAddress].SOs.length - 1;
      }
    }
  } catch (error) {
    console.error(`Error reading register ${register3000Address}:`, error.message);
  }
}

async function writeActualSizesForMultipleSOs(ipAddress, client, isLeather) {
  if (!modbusClients[ipAddress].hasMultipleSOs) return;

  const currentSO = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
  if(currentSO == undefined) return;
  let uniquePartNameSOs = [];
  if (currentSO && Array.isArray(currentSO.Data)) {
    uniquePartNameSOs = [...new Set(currentSO.Data.map(p => p.PartName))];
    console.log(`Unique PartName Store:`, uniquePartNameSOs);
  } else {
    console.warn(`currentSO or currentSO.Data is undefined for IP: ${ipAddress}`);
  }

  if (uniquePartNameSOs.length === 0) return;

  const selectedPartName = uniquePartNameSOs[modbusClients[ipAddress].indexMultiplePartNames];
  const distribution = currentSO.Data.filter(item => item.PartName === selectedPartName);

  if (distribution.length === 0) return;

  const groupedBySizeExisted = distribution
  .filter(({ ActualCut, ActualSizeQty, ActualPieces }) =>
    ActualCut != null && ActualSizeQty != null && ActualPieces != null
  )
  .reduce((acc, item) => {
    const sizeID = item.SizeID;
    const actualCut = item.ActualCut ?? 0;
    const actualSizeQty = item.ActualSizeQty ?? 0;
    const actualPieces = item.ActualPieces ?? 0;

    if (!acc[sizeID]) {
      acc[sizeID] = { cuts: [], qtys: [], pieces: [] };
    }

    acc[sizeID].cuts.push(actualCut);
    acc[sizeID].qtys.push(actualSizeQty);
    acc[sizeID].pieces.push(actualPieces);

    return acc;
  }, {});


  if (!groupedBySizeExisted) return;

  const summedValues = Object.keys(groupedBySizeExisted).map(sizeID => {
    const { cuts, qtys, pieces } = groupedBySizeExisted[sizeID];
    return {
      sizeID,
      totalCuts: cuts.reduce((sum, cut) => sum + cut, 0),
      totalQtys: qtys.reduce((sum, qty) => sum + qty, 0),
      totalPieces: pieces.reduce((sum, piece) => sum + piece, 0)
    };
  });

  if (summedValues.length === 0) return;

  const sizeInfo = modbusClients[ipAddress].sizeDataInfo;
  if (!sizeInfo) return;

  const sizeAddress = sizeInfo.sizeID.slice(0, 6);
  if (sizeAddress.length === 0) return;

  if (!modbusClients[ipAddress].previousSizeData) {
    modbusClients[ipAddress].previousSizeData = [];
  }

  try {
    await Promise.all(
      sizeAddress.map(async (sizeAddressID, index) => {
        if (sizeAddressID == null) return; // Skip null/undefined
  
        const sizeID = await safeRead(sizeAddressID, client);
        if (sizeID == null || sizeID == 0) {
          console.warn(`⚠️ Skipping null/undefined sizeID at index ${index}`);
          return;
        }
  
        const matched = summedValues.find(item => parseInt(item.sizeID) === sizeID);
        if (!matched) {
          console.log(`⚠️ No match found in summedValues for sizeID ${sizeID}`);
          return;
        }
  
        const typeMaterial = isLeather === 2 ? 1 : 0; 
        const baseActual = typeMaterial ? 922 : 794;
        const actualAddress = baseActual + 16 * index;
  
        const newData = {
          sizeID,
          actualCut: matched.totalCuts,
          actualPieces: matched.totalPieces,
          actualQtys: matched.totalQtys
        };
  
        const existing = modbusClients[ipAddress].previousSizeData.find(data => data.sizeID === sizeID);
  
        const isSame =
          existing &&
          existing.actualCut === newData.actualCut &&
          existing.actualPieces === newData.actualPieces &&
          existing.actualQtys === newData.actualQtys;
  
        if (!isSame) {
          await client.writeSingleRegister(actualAddress, newData.actualCut);
          await client.writeSingleRegister(actualAddress + 4, newData.actualPieces);
          await client.writeSingleRegister(actualAddress + 8, newData.actualQtys);
  
          console.log(`✅ Written values for sizeID ${sizeID} ${sizeAddressID} ${newData.actualQtys} ${actualAddress} at index ${index}`);
  
          if (existing) {
            Object.assign(existing, newData);
          } else {
            modbusClients[ipAddress].previousSizeData.push(newData);
          }
        } else {
          console.log(`⏭️ Skipped writing for sizeID ${sizeID}, no change in values.`);
        }
      })
    );
  
    console.log("✅ All necessary registers written.");
  } catch (error) {
    console.error("❌ Error writing registers:", error.message);
  }  
}

/**
 * Check if a specific bit is on (1) or off (0)
 * @param {number} value - The value to check.
 * @param {number} bitIndex - The bit index to check (0-based index).
 * @returns {boolean} - Returns true if the bit is on (1), false if the bit is off (0).
 */
function isBitOn(binaryString, bitIndex) {
  const value = parseInt(binaryString, 2); // base 2 => binary to number
  const bitmask = 1 << bitIndex;
  return (value & bitmask) !== 0;
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

// Function to read registers safely
const safeRead = async (address, client) => {
  try {
    const response = await client.readHoldingRegisters(address, 1);
    return response?.response?._body?.values?.[0] ?? 0;
  } catch (err) {
    console.error(`Error reading register ${address}:`, err.message);
    return 0; // Default to 0 in case of error
  }
};
async function readActualData(client, ipAddress) {
  // deplay time to read first
  //await delay(1000);

  const rawLeather = await safeRead(1001, client);
  const isLeather = rawLeather === 2 ? 1 : 0;  
  try {
    if (modbusClients[ipAddress].hasMultipleSOs)
    {
      processSizeID(ipAddress, isLeather);
    }
    // Read OrderID from register 1000
    const orderIDData = await client.readHoldingRegisters(1000, 1);
    if (!orderIDData?.response?._body?.values?.[0]) {
      console.log("Không thể đọc OrderID từ thanh ghi 1000");
      return;
    }

    let OrderID = orderIDData.response._body.values[0];
    const baseAddress = isLeather ? 966 : 886;
    const baseActual = isLeather ? 922 : 794;
    const baseSizeQtyAddress = isLeather ? 918 : 790;
    let collectPartAndOrderID;

    let completeSizeCount = 0;

    if (!modbusClients[ipAddress].previousData) {
      modbusClients[ipAddress].previousData = {};
    }
    const sizeInfo = modbusClients[ipAddress].sizeDataInfo;
    const sizeCompleteID = modbusClients[ipAddress].sizeCompleteID || [];

    // Check choose size
    const responseChooseSize = await client.readHoldingRegisters(modbusClients[ipAddress].chooseSizeAddress, 1);
    const registerChooseSizeValue = responseChooseSize.response._body.values[0];

    let binaryChooseSizeValue = registerChooseSizeValue.toString(2).padStart(16, '0');
    console.log(`Output of size at register address ${modbusClients[ipAddress].chooseSizeAddress} = ${registerChooseSizeValue}`);
    console.log(`Binary representation: ${binaryChooseSizeValue}`);

    const index = findSetBitIndex(binaryChooseSizeValue);
    if (index === -1) {
      return;
    }
    if (sizeInfo.sizeID == undefined || sizeInfo.sizeID[index] == null) return;

    const sizeAddressID = sizeInfo.sizeID[index];
    
    try {
      const sizeQtyAddress = baseSizeQtyAddress + 16 * index;
      const actualAddress = baseActual + 16 * index;
    
      let [
        sizeID,
        sizeQty = 0,
        piecesPerPair = 0,
        materialLayer = 0,
        cuttingDieQty = 0,
        actualCut = 0,
        actualPieces = 0,
        actualSizeQty = 0,
        totalPieces = 0,
      ] = await Promise.all([
        safeRead(sizeAddressID, client),
        safeRead(sizeQtyAddress, client),
        !isLeather ? safeRead(baseAddress, client) : Promise.resolve(0),
        !isLeather ? safeRead(baseAddress + 4, client) : Promise.resolve(0),
        !isLeather ? safeRead(baseAddress + 8, client) : Promise.resolve(0),
        safeRead(actualAddress, client),
        safeRead(actualAddress + 4, client),
        safeRead(actualAddress + 8, client),
        isLeather ? safeRead(baseAddress, client) : Promise.resolve(0),
      ]);
    
      if (sizeID === null) {
        console.warn(`Skipping sizeID ${sizeAddressID} due to read failure`);
        return;
      }
    
      const isComplete = actualCut === sizeQty;
      if (isComplete && !sizeCompleteID.includes(sizeID)) {
        sizeCompleteID.push(sizeID);
        console.log(`[sizeCompleteID] Added: ${sizeID}`);
      }
    
      completeSizeCount++;
    
      // ----- Multiple SO logic -----
      let partID;
      if (modbusClients[ipAddress].hasMultipleSOs != null) {
        let checkPendingSize;
        if(!isLeather) {
          partID = await safeRead(300, client);
          checkPendingSize = 
          modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
            ?.filter(item => item.SizeID === sizeID && item.Status === 'Pending' && item.PartID === partID) || [];
        } else {
          checkPendingSize = 
          modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
            ?.filter(item => item.SizeID === sizeID && item.Status === 'Pending') || [];

          collectPartAndOrderID = (
            checkPendingSize
              ?.filter(item => item.SizeID === sizeID && item.Status === 'Pending')
              .map(item => ({
                PartID: item.PartID,
                OrderID: item.OrderID
              }))
          ) || [];
        }
    
        if (checkPendingSize.length == 0) return;
        checkPendingSize = checkPendingSize[0];
        sizeID = checkPendingSize.SizeID;
        OrderID = checkPendingSize.OrderID;
        partID = checkPendingSize.PartID;
    
        // collect total size complete actualSizeQty and actualCut
        let totalCompletedSize;
        if(!isLeather) {
          totalCompletedSize = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
          ?.filter(item => item.Status === 'Complete' && item.SizeID === sizeID && item.PartID === partID)
          ?.reduce(
            (acc, item) => {
              acc.totalSizeQty += item.SizeQty ?? 0;
              acc.totalActualCut += item.ActualCut ?? 0;
              return acc;
            },
            { totalSizeQty: 0, totalActualCut: 0 }
          ) ?? { totalSizeQty: 0, totalActualCut: 0 };
        } else {
          totalCompletedSize = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
          ?.filter(item => item.Status === 'Complete' && item.SizeID === sizeID)
          ?.reduce(
            (acc, item) => {
              acc.totalSizeQty += item.SizeQty ?? 0;
              acc.totalActualCut += item.ActualCut ?? 0;
              return acc;
            },
            { totalSizeQty: 0, totalActualCut: 0 }
          ) ?? { totalSizeQty: 0, totalActualCut: 0 };
        }
        
        // reset stored when have complete total size
        // if (totalCompletedSize.totalSizeQty !== 0) {
        //   modbusClients[ipAddress].storedActualCut = undefined;
        //   modbusClients[ipAddress].storedActualSizeQty = undefined;
        //   modbusClients[ipAddress].storedActualPieces = undefined;
        // }
    
        // if (modbusClients[ipAddress].storedActualCut === undefined &&
        //     modbusClients[ipAddress].storedActualSizeQty === undefined &&
        //     modbusClients[ipAddress].storedActualPieces === undefined || actualSizeQty == null) {
        //   modbusClients[ipAddress].storedActualCut = actualCut ?? 0;
        //   modbusClients[ipAddress].storedActualSizeQty = actualSizeQty ?? 0;
        //   modbusClients[ipAddress].storedActualPieces = actualPieces ?? 0;
        // }
    
        modbusClients[ipAddress].storedActualCut = actualCut ?? 0;
        modbusClients[ipAddress].storedActualSizeQty = actualSizeQty ?? 0;
        modbusClients[ipAddress].storedActualPieces = actualPieces ?? 0;

        let sizeRemain;
          if ((checkPendingSize.InventoryQty + actualSizeQty) >= checkPendingSize.SizeQty && checkPendingSize.InventoryQty !== 0) {
            modbusClients[ipAddress].actualSizeQty = checkPendingSize.InventoryQty + actualSizeQty;
            totalCompletedSize.totalSizeQty = checkPendingSize.InventoryQty;
          }
          const completedQty = Number(totalCompletedSize.totalSizeQty) || 0;

          sizeRemain = completedQty;

          console.log(`sizeRemain [Address] ${ipAddress} ${sizeRemain} ActualCut ${actualCut} actualSizeQty ${actualSizeQty}`);
    
          const availableQty = modbusClients[ipAddress].storedActualSizeQty - sizeRemain;

          if (availableQty < 0) {
              return;
          }
          
          if (availableQty >= checkPendingSize.SizeQty) {
            if(isLeather) {
              // case leather
              const so = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs];
              if (so?.Data?.length) {
                for (const item of collectPartAndOrderID) {
                  const match = so.Data.find(
                    x => x.SizeID === sizeID &&
                        x.PartID === item.PartID &&
                        x.OrderID === item.OrderID &&
                        x.Status === 'Pending'
                  );
                  if (match) {
                    match.Status = 'Complete';
                    match.ActualCut = modbusClients[ipAddress].storedActualCut - totalCompletedSize.totalActualCut;
                    if(actualSizeQty > checkPendingSize.SizeQty) {
                      match.ActualSizeQty = checkPendingSize.SizeQty;
                    } else {
                      match.ActualSizeQty = modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
                    }
                    console.log(`Updated status to Complete for SizeID: ${sizeID}, PartID: ${item.PartID}, OrderID: ${item.OrderID}`);
                  }
                }
              }
            } else {
              // case rawleather
              checkPendingSize.Status = 'Complete';
              checkPendingSize.ActualCut = modbusClients[ipAddress].storedActualCut - totalCompletedSize.totalActualCut;
              if(actualSizeQty > checkPendingSize.SizeQty) {  
                checkPendingSize.ActualSizeQty = checkPendingSize.SizeQty;
              } else {
                checkPendingSize.ActualSizeQty = modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
              }
              console.log(`Updated status to Complete for SizeID: ${sizeID}, OrderID: ${OrderID}`);
        }
        if (totalCompletedSize.totalSizeQty !== 0) {
          if (isLeather) {
            actualSizeQty = await  readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
            actualPieces = checkPendingSize.TotalPiecesPerPair * actualSizeQty + actualCut;
          }
          else {
            actualSizeQty = await  readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
            modbusClients[ipAddress].storedActualCut -= totalCompletedSize.totalActualCut;
            actualCut = modbusClients[ipAddress].storedActualCut;
            actualPieces = (checkPendingSize.MaterialLayer * checkPendingSize.CuttingDieQty) * actualCut; 
          }
        } else {
          if (isLeather) {
            actualSizeQty = await  readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
            actualPieces = checkPendingSize.TotalPiecesPerPair * actualSizeQty + actualCut;
          }
          else {
            actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
            actualCut -= totalCompletedSize.totalActualCut;
            actualPieces = (checkPendingSize.MaterialLayer * checkPendingSize.CuttingDieQty) * actualCut;
          }
        }
        console.log(`Total Complete => ActualCut [Address] ${ipAddress} ${actualCut} actualSizeQty ${actualSizeQty}`);
        } else {
          if (isLeather) {
            actualPieces = checkPendingSize.TotalPiecesPerPair * actualSizeQty + actualCut;
            modbusClients[ipAddress].storedActualSizeQty -= totalCompletedSize.totalSizeQty;
            actualSizeQty = modbusClients[ipAddress].storedActualSizeQty;
          }
          else {
            modbusClients[ipAddress].storedActualCut -= totalCompletedSize.totalActualCut;
            modbusClients[ipAddress].storedActualSizeQty -= totalCompletedSize.totalSizeQty;
            actualCut = modbusClients[ipAddress].storedActualCut;
            actualSizeQty = modbusClients[ipAddress].storedActualSizeQty;
            actualPieces = (checkPendingSize.MaterialLayer * checkPendingSize.CuttingDieQty) * actualCut;
          }
          // actualPieces = actualSizeQty  * (checkPendingSize.MaterialLayer / checkPendingSize.PiecesPerPair);
          console.log(`ActualCut [Address] ${ipAddress} actualSizeQty ${actualSizeQty}`);
        }
      }
      if(collectPartAndOrderID !== undefined && collectPartAndOrderID.length > 0) {
        for (const item of collectPartAndOrderID) { 
          processActualDataChange(ipAddress, sizeID, item.PartID, piecesPerPair, materialLayer, cuttingDieQty, actualCut, actualPieces, actualSizeQty, totalPieces, isLeather, item.OrderID); 
        }
      } else {
        processActualDataChange(ipAddress, sizeID, partID, piecesPerPair, materialLayer, cuttingDieQty, actualCut, actualPieces, actualSizeQty, totalPieces, isLeather, OrderID);
      }
    } catch (error) {
      console.error(`Error processing sizeID ${sizeAddressID}:`, error.message);
    }    
    modbusClients[ipAddress].isComplete = completeSizeCount;
  } catch (error) {
    console.error(`Error reading actual data: ${error.message}`);
    logToFile(errorLogPath, `Error reading actual data: ${error.message}`);
  }

async function readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize) {
    if (modbusClients[ipAddress].storedActualSizeQty >= sizeQty) {
      let extraSizeRemaining = modbusClients[ipAddress].storedActualSizeQty - sizeQty;
      actualSizeQty = checkPendingSize.SizeQty + extraSizeRemaining;
    } else {
      modbusClients[ipAddress].storedActualSizeQty -= totalCompletedSize.totalSizeQty;
      actualSizeQty = modbusClients[ipAddress].storedActualSizeQty;
    }
    return parseInt(actualSizeQty);
  }
}

async function processActualDataChange(
  ipAddress,
  sizeID,
  partID,
  piecesPerPair,
  materialLayer,
  cuttingDieQty,
  actualCut,
  actualPieces,
  actualSizeQty,
  totalPieces,
  isLeather,
  OrderID ) {
  const newData = {
    SizeID: sizeID,
    PartID: partID,
    PiecesPerPair: piecesPerPair,
    MaterialLayer: materialLayer,
    CuttingDieQty: cuttingDieQty,
    ActualCut: actualCut,
    ActualPieces: actualPieces,
    ActualSizeQty: actualSizeQty,
    TotalPiecesPerPair: totalPieces,
  };

  // previousData exist
  if (!modbusClients[ipAddress]) {
    console.warn(`modbusClients[${ipAddress}] is undefined.`);
    return;
  }

  modbusClients[ipAddress].previousData ||= {};
  const prevData = modbusClients[ipAddress].previousData[sizeID] || {};
  const isNewData = !modbusClients[ipAddress].previousData[sizeID];
  let hasChanges = false;
  if (isLeather) { 
    hasChanges = isNewData;
  } else {
    hasChanges = isNewData || JSON.stringify(newData) !== JSON.stringify(prevData);
  }

  if (hasChanges) {
    console.log(`Data changed or first-time load for sizeID ${sizeID}, updating DB...`, newData);

    await saveActualDataToDB({
      OrderID,
      IsLeather: isLeather,
      SizeData: [newData],
    });

    modbusClients[ipAddress].previousData[sizeID] = { ...newData };
  }
}

// proccess to mutiple SO size -- save on DB
async function processSizeID(ipAddress, isLeather) {
  if (!modbusClients[ipAddress]?.SOs?.length) {
    console.log(`[sizeID] No SOs data available for IP: ${ipAddress}`);
    return;
  }

  // Extract OrderID and SizeID pairs from SOs.Data array
  const validPairs = modbusClients[ipAddress].SOs.flatMap(so =>
    (so.Data || []).map(dataItem => ({
      OrderID: dataItem.OrderID,
      SizeID: dataItem.SizeID,
      PartID: dataItem.PartID,
      ActualCut : dataItem.ActualCut, 
      CuttingDieQty : dataItem.CuttingDieQty,
      PiecesPerPair : dataItem.PiecesPerPair,
      MaterialLayer : dataItem.MaterialLayer,
      TotalPiecesPerPair: dataItem.TotalPiecesPerPair
    }))
  );

  for (const { OrderID, SizeID, PartID, ActualCut, CuttingDieQty, PiecesPerPair, MaterialLayer, TotalPiecesPerPair } of validPairs) {

    for (const so of modbusClients[ipAddress]?.SOs || []) {
      const match = (so.Data || []).find(item =>
        item.OrderID === OrderID &&
        item.SizeID === SizeID &&
        item.PartID === PartID
      );
      if (match) {
        match.ActualCut = typeof match.ActualCut === 'number' ? match.ActualCut : 0;
        console.log(`Set ActualCut for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
      }
    }
    // Check if this pair was already processed
    if (ActualCut !== null) {
      console.log(`[sizeID] Skipping duplicate OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
      continue; // Skip processing if already saved
    }

    console.log(`[sizeID] Processing OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);

    const newData = {
      SizeID,
      PartID,
      PiecesPerPair: PiecesPerPair,
      MaterialLayer: MaterialLayer,
      CuttingDieQty: CuttingDieQty,
      ActualCut: 0,
      ActualPieces: 0,
      ActualSizeQty: 0,
      TotalPiecesPerPair: TotalPiecesPerPair,
    };

    try {
      await saveActualDataToDB({
        OrderID,
        IsLeather: isLeather,
        SizeData: [newData],
      });
      console.log(`[sizeID] Successfully saved data for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
    } catch (error) {
      console.error(`[sizeID] Error saving data for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}: ${error.message}`);
    }
  }
}

async function writeToModbusRegister(client) {
  async function writeLoop() {
    try {
      counter++;
      if(counter == 60000) {
        counter = 0;
      }
      await client.writeSingleRegister(8000, counter);
    } catch (err) {
      const errorMessage = `Error writing to Modbus register 8000: ${err.message}`;
      console.error(errorMessage);
      logToFile(errorLogPath, errorMessage);
    } finally {
      setTimeout(writeLoop, 1000); // schedule the next write after 1s
    }
  }

  writeLoop(); // start the loop
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
    // Ensure modbusClients structure exists
    modbusClients[ipAddress] ||= {};
    modbusClients[ipAddress].sizeDataInfo ||= {};
    // Write Order Info
    const orderInfo = [data.Model, data.ART];
    const orderInfoAddresses = [
      { start: 0, maxRegisters: 25 },
      { start: 25, maxRegisters: 10 }
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

    //   // Write partID
    //   const partIDRegisterAddress = data.Leather == 2 ? 730 : 300;
    //   const partIDPromises = data.MaterialData.slice(0, 20).map(async (material) => {
    //   try {
    //     // Convert the material code to an array of 16-bit little-endian values
    //     const partIDCodeArray = stringTo16BitArrayLittleEndian(material.PartID);
        
    //     // Write each 16-bit value to consecutive Modbus registers
    //     for (let i = 0; i < partIDCodeArray.length; i++) {
    //       const registerAddress = partIDRegisterAddress + i; // Increment register address for each 16-bit value
    //       await client.writeSingleRegister(registerAddress, partIDCodeArray[i]);
    //       console.log(`Successfully wrote partID ${partIDCodeArray[i]} to register ${registerAddress}`);
    //     }
    //   } catch (error) {
    //     console.error(`Error writing partID: ${error.message}`);
    //   }
    // });
    
    // await Promise.all(partIDPromises);
    
    // if (!modbusClients[ipAddress].hasMultipleSOs) {
    // // Write Part Names
    // const partNameStartRegister = data.Leather == 2 ? 115 : 270;
    // const partNamePromises = data.MaterialData.slice(0, 20).map(async (material, i) => {
    //   try {
    //     const partName = material.PartName;
    //     const startRegister = partNameStartRegister + (i * 20);
    //     let registerData = stringTo16BitArrayLittleEndian(partName).slice(0, 10 * 2);
    //     for (let j = 0; j < registerData.length; j++) {
    //       await client.writeSingleRegister(startRegister + j, registerData[j]);
    //       console.log(`Successfully wrote part name to register ${startRegister + j}`);
    //     }
    //   } catch (error) {
    //     console.error(`Error writing part name: ${error.message}`);
    //   }
    // });

    // await Promise.all(partNamePromises);
    // }
    
    // Write defaultValue
    const defaultValue =  modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
    if (data.Leather == 1) {
      const BASE_REGISTER = 886; 
      try {
        await client.writeSingleRegister(BASE_REGISTER, defaultValue.Data[0].PiecesPerPair ?? 0);
        await client.writeSingleRegister(BASE_REGISTER + 4, defaultValue.Data[0].MaterialLayer ?? 0);
        await client.writeSingleRegister(BASE_REGISTER + 8, defaultValue.Data[0].CuttingDieQty ?? 0);
        console.log("✅ Successfully wrote to Modbus registers (Leather == 1)");
      } catch (error) {
        console.error("❌ Error writing to Modbus registers (Leather == 1): ", error);
      }
    } else {
      const BASE_REGISTER = 966; // Đảm bảo có dữ liệu để lặp qua
      try {
        await client.writeSingleRegister(BASE_REGISTER, defaultValue.Data[0].TotalPiecesPerPair ?? 0);
        console.log("✅ Successfully wrote to Modbus registers (Leather != 1)");
      } catch (error) {
        console.error("❌ Error writing to Modbus registers (Leather != 1): ", error);
      }
      
    }
    // Write SizeData
    await writeRegisterSizeData(client , ipAddress, data.SizeData, data.Leather);
    writeActualSizesForMultipleSOs(ipAddress, client, data.Leather);
    console.log('Data successfully saved to Modbus');
  } catch (error) {
    console.error(`Error saving distribution data to Modbus: ${error.message}`);
    throw new Error(`Failed to save distribution data: ${error.message}`);
  }
}


// Ghi size data vào thanh ghi 
async function writeRegisterSizeData(client, ipAddress, sizeData, isLeather) {
  // Ensure sizeData is an array and limit its length based on isLeather
  if (!Array.isArray(sizeData)) return;

  const maxSize = isLeather == 2 ? 3 : 6;
  const processedSizeData = sizeData.slice(0, maxSize);

  // Assign values correctly
  modbusClients[ipAddress].sizeDataInfo.sizeCount = processedSizeData.length;
  modbusClients[ipAddress].sizeDataInfo.isLeather = isLeather;

  console.log(`[sizeDataInfo] Number of size ${modbusClients[ipAddress].sizeDataInfo.sizeCount} and isLeather ${isLeather}`);

  let registerSize = [];
  if (isLeather == 2) {
    // Leather sizes
    await client.writeSingleRegister(1003, processedSizeData.length);
    registerSize = [
      { SizeID: 900, Size: 906, SizeQty: 918, InventoryQty: 79 },
      { SizeID: 902, Size: 910, SizeQty: 934, InventoryQty: 83 },
      { SizeID: 904, Size: 914, SizeQty: 950, InventoryQty: 87 },
    ];
  } else {
    // Raw sizes
    await client.writeSingleRegister(1002, processedSizeData.length);
    registerSize = [
      { SizeID: 750, Size: 762, SizeQty: 790, InventoryQty: 55 },
      { SizeID: 752, Size: 766, SizeQty: 806, InventoryQty: 59 },
      { SizeID: 754, Size: 770, SizeQty: 822, InventoryQty: 63 },
      { SizeID: 756, Size: 774, SizeQty: 838, InventoryQty: 67 },
      { SizeID: 758, Size: 778, SizeQty: 854, InventoryQty: 71 },
      { SizeID: 760, Size: 782, SizeQty: 870, InventoryQty: 75 },
    ];
  }

  for (let i = 0; i < processedSizeData.length; i++) {
    const item = processedSizeData[i];

    // Validate the values before writing
    const isValidValue = (value) => typeof value === 'number' && value >= 0 && value <= 65535;

    console.log(`Writing to Modbus: SizeID = ${item.SizeID}, Size = ${item.Size}, SizeQty = ${item.SizeQty}, InventoryQty = ${item.InventoryQty}`);

    if (!item.Size || typeof item.Size !== 'string') {
      console.error(`Invalid Size value: ${item.Size}`);
      continue;
    }
    if (!isValidValue(item.SizeID) || !isValidValue(item.SizeQty) || !isValidValue(item.InventoryQty)) {
      console.error(`Invalid value detected for SizeID ${item.SizeID}: Values must be within the range 0-65535.`);
      continue;  // Skip
    }

    try {
      // Write SizeID, Size, SizeQty, and InventoryQty to the corresponding Modbus registers
      let registerSizeData = stringTo16BitArrayLittleEndian(item.Size).slice(0, 10 * 2);
      const startSizeRegister = registerSize[i].Size; 
      await client.writeSingleRegister(registerSize[i].SizeID, item.SizeID);

      // Ensure sizeID array exists
      if (!Array.isArray(modbusClients[ipAddress].sizeDataInfo.sizeID)) {
        modbusClients[ipAddress].sizeDataInfo.sizeID = [];
      }
 
      if (!modbusClients[ipAddress].sizeDataInfo.sizeID.includes(registerSize[i].SizeID)) {
        modbusClients[ipAddress].sizeDataInfo.sizeID.push(registerSize[i].SizeID);
        console.log(`[sizeDataInfo] sizeID ${registerSize[i].SizeID}`);
        console.log(`Writing to register ${registerSize[i].SizeID}, value: ${item.SizeID}`);
      }      
      
      for (let j = 0; j < registerSizeData.length; j++) {
        console.log(`Writing to register ${startSizeRegister + j}, value: ${registerSizeData[j]}`);
        await client.writeSingleRegister(startSizeRegister + j, registerSizeData[j]);
      }             

      console.log(`Writing to register ${registerSize[i].SizeQty}, value: ${item.SizeQty}`);
      console.log(`Writing to register ${registerSize[i].InventoryQty}, value: ${item.InventoryQty}`);

      await client.writeSingleRegister(registerSize[i].SizeQty, item.SizeQty || 0);    
      await client.writeSingleRegister(registerSize[i].InventoryQty, item.InventoryQty || 0); 
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
      if (modbusClient?.socket) {
        modbusClient.socket.end();
        console.log("Socket connection closed.");
      }
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
  saveDistributionDataToModbus,
  closeAllConnections,
  setIpAddresses,
  startReadingRegisters,
  isHostReachable,
  modbusClients,
};