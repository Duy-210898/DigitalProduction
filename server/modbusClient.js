const fs = require('fs');
const net = require('net');
const Modbus = require('jsmodbus');
const ping = require('ping');
const ModbusPollingManager = require('./ModbusPollingManager');
const manager = new ModbusPollingManager(1000);
const { updateDeviceConnectionStatus, getSizeDataFromDB, getDistributionDataFromDb, saveActualDataToDB, setDistributionIsComplete, setSubDistributionComplete , getSubDistributions, getSizeAndDistributionDataFromDb, getDistributionIDFromSizeID, getDistributionCompleteFromDb, logCutHistoryToDB } = require('./database');
//const { notifyClientsToDeleteOrder } = require('./notifications');

let countRemainSizeData = {};
let storeDistributionData = {};
// let previousData = {};  // Store previous data for comparison

let modbusClients = {};
let register3000Address = 3000;
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
  if (!ipAddress || typeof ipAddress !== 'string') {
    console.error(`❌ Invalid IP address: ${ipAddress}`);
    return null;
  }

  const isValidIP = /^(\d{1,3}\.){3}\d{1,3}$/.test(ipAddress);
  if (!isValidIP) {
    console.error(`❌ Invalid IP format: ${ipAddress}`);
    logToFile(errorLogPath, `Invalid IP address skipped: ${ipAddress}`);
    return null;
  }

  // Hardcoded skip
  if (ipAddress !== '10.30.4.144') return null;

  // Nếu đã kết nối
  if (modbusClients[ipAddress]?.isConnected) {
    return { client: modbusClients[ipAddress].client, socket: modbusClients[ipAddress].socket };
  }

  // Kiểm tra ping
  const isReachable = await pingHost(ipAddress);
  if (!isReachable) {
    console.error(`🔴 Device at ${ipAddress} is not reachable.`);
    logToFile(errorLogPath, `Device at ${ipAddress} is not reachable.`);
    await updateDeviceConnectionStatus(ipAddress, false);
    return null;
  }

  console.log(`✅ ${ipAddress} is reachable.`);

  const socket = new net.Socket();
  const client = new Modbus.client.TCP(socket, 1);
  const options = { host: ipAddress, port: 502 };

  return new Promise((resolve, reject) => {
    socket.connect(options, async () => {
      console.log(`✅ Connected to device at ${ipAddress}`);
      logToFile(successLogPath, `Connected to device at ${ipAddress}`);

      modbusClients[ipAddress] = {
        client,
        socket,
        isConnected: true,
        isDisconnected: false,
        indexMultipleSOs: 0,
        SOs: [],
        indexMultiplePartNames: 0,
        readerLoopStarted: false,
        sizeDataInfo: {}
      };

      try {
        await updateDeviceConnectionStatus(ipAddress, true);
      } catch (err) {
        console.error(`⚠️ Error updating connection status: ${err.message}`);
        logToFile(errorLogPath, `Initial index update failed: ${err.message}`);
      }

      startReadingRegisters(ipAddress);
      writeToModbusRegister(ipAddress).catch(err => {
        console.error(`⚠️ Error writing to Modbus: ${err.message}`);
        logToFile(errorLogPath, `Write error: ${err.message}`);
      });

      resolve({ client, socket });
    });

    // Xử lý lỗi kết nối
    socket.once('error', async error => {
      logToFile(errorLogPath, `❌ Connection error to ${ipAddress}:\n${error.stack}`);
      await handleConnectionFailure(ipAddress, error, resolve, reject, retries);
    });

    // Xử lý socket close
    socket.once('close', async hadError => {
      logToFile(successLogPath, `Connection closed at ${ipAddress}`);
      await handleConnectionFailure(ipAddress, null, resolve, reject, retries);
    });
  });
}

async function handleConnectionFailure(ipAddress, error, resolve, reject, retries) {
  try {
    await handleDisconnection(ipAddress);
  } catch (e) {
    console.error(`❌ Error in handleDisconnection: ${e.message}`);
  }

  if (modbusClients[ipAddress]) {
    modbusClients[ipAddress].isConnected = false;
    modbusClients[ipAddress].isDisconnected = true;
    delete modbusClients[ipAddress].client;
    delete modbusClients[ipAddress].socket;
  }

  if (retries < 3) {
    console.warn(`🔄 Retrying connection to ${ipAddress} (${retries + 1}/3)`);
    setTimeout(() => {
      connectToDevice(ipAddress, retries + 1).then(resolve).catch(reject);
    }, 10000);
  } else {
    console.error(`❌ Max retries reached for ${ipAddress}`);
    await updateDeviceConnectionStatus(ipAddress, false);
    reject(new Error("Max retries reached"));
  }
}

async function startReadingRegisters(ipAddress) {
  if (!ipAddress || typeof ipAddress !== 'string') {
    console.error(`Invalid IP address:`, ipAddress);
    return;
  }

  if (!modbusClients[ipAddress]) {
    modbusClients[ipAddress] = { readerLoopStarted: false };
  }

  const entry = modbusClients[ipAddress];
  if (entry.readerLoopStarted) {
    console.log(`[${ipAddress}] Reader loop already started.`);
    return;
  }
  entry.readerLoopStarted = true;

  // Kết nối tới device
  try {
    const result = await connectToDevice(ipAddress);
    if (!result) {
      console.error(`[${ipAddress}] connectToDevice returned null`);
      entry.isConnected = false;
      entry.client = null;
      entry.socket = null;
      manager.registerClient(ipAddress, entry);
      return;
    }

    const { client, socket } = result;
    entry.client = client;
    entry.socket = socket;
    entry.isConnected = true;
    entry.sizeDataInfo = {};

    manager.registerClient(ipAddress, entry);
  } catch (err) {
    console.error(`[${ipAddress}] Initial connect failed: ${err.message}`);
    entry.isConnected = false;
    entry.client = null;
    entry.socket = null;
    manager.registerClient(ipAddress, entry);
  }
}

// Đăng ký event 1 lần
manager.on('poll', async (client, ip) => {
  try {
    await checkAndSaveDistribution(client, ip);
  } catch (err) {
    console.error(`[${ip}] Error in poll: ${err.message}`);
  }
});

manager.on('readActual', async (client, ip) => {
  try {
    await readActualData(client, ip);
  } catch (err) {
    console.error(`[${ip}] Error in readActual: ${err.message}`);
  }
});

manager.on('reconnect', async (ip) => {
  try {
    const result = await connectToDevice(ip);
    if (!result) {
      console.error(`[${ip}] Reconnect failed: connectToDevice returned null`);
      if (modbusClients[ip]) modbusClients[ip].isConnected = false;
      return;
    }

    const { client, socket } = result;
    if (!modbusClients[ip]) modbusClients[ip] = {};
    modbusClients[ip].client = client;
    modbusClients[ip].socket = socket;
    modbusClients[ip].isConnected = true;
  } catch (err) {
    console.error(`[${ip}] Reconnect failed: ${err.message}`);
    if (modbusClients[ip]) modbusClients[ip].isConnected = false;
  }
});

async function checkAndSaveDistribution(client, ipAddress) {
  try {
    if (client === undefined) return;
    // Read register 1000
    let response = await client.readHoldingRegisters(1000, 1);
    if (!response) return;
    let orderID = response.response._body.values[0];
  //  console.log(`Register 1000 value: ${orderID}`);

    // check complete order
    let responseLeather = await client.readHoldingRegisters(1001, 1);
    let isLeather = responseLeather.response._body.values[0];
 //   console.log(`Register 1001 value: ${isLeather}`);
    // check register 3000 status
    checkBitOnOffRegister3000(client, ipAddress, register3000Address);

    let distributionData;
    if (orderID === 0) {
      console.log(`Register 1000 is 0. Fetching distribution data for IP: ${ipAddress}`);

      // Fetch distribution data from DB
      distributionData = await getDistributionDataFromDb(ipAddress);
      processDistributionData(client, ipAddress, distributionData);
    }
    else {
       // get index SOs and Part if available
       adjustModbusIndex(await safeRead(1020, client), ipAddress, true);
       adjustModbusIndex(await safeRead(1022, client), ipAddress, false);

      storeDistributionData[ipAddress] = {};
      //retry get storeDistributionData
      if (Object.keys(storeDistributionData[ipAddress]).length === 0) {
        await fetchStoreDistributionData(client, ipAddress, orderID, isLeather, distributionData);
      }
      if (storeDistributionData[ipAddress] == null) { return }
      // Store orderID in modbusClients
      if (!modbusClients[ipAddress]) {
        modbusClients[ipAddress] = {};
      }
      countRemainSizeData[ipAddress] = { index: 0 };
      modbusClients[ipAddress].orderID = orderID;
      if (modbusClients[ipAddress].SOs.length == 0) { 
        return
      }
      if (ipAddress !== '10.30.4.144') return;
      // interrupt HMI set distributionData again
      if (!modbusClients[ipAddress].sizeDataInfo ||
        typeof modbusClients[ipAddress].sizeDataInfo !== 'object' ||
        Object.keys(modbusClients[ipAddress].sizeDataInfo).length === 0) {
        const distributionData = await getDistributionDataFromDb(ipAddress);
        if (distributionData == null) {

          modbusClients[ipAddress].previousSizeData = [];
          modbusClients[ipAddress].previousData = {};
          modbusClients[ipAddress].indexMultipleSOs = 0;
          modbusClients[ipAddress].indexMultiplePartNames = 0;
          modbusClients[ipAddress].sizeDataInfo.sizeID = [];
          modbusClients[ipAddress].hasMultipleSOs = false;
        }
        processDistributionData(client, ipAddress, distributionData);
      }
      // Ensure checkDelete exists and is initialized to 0
      if (!storeDistributionData[ipAddress]) {
        storeDistributionData[ipAddress] = {};
      }

      try {

        // check complete size mutiple SO
        const sizeData = storeDistributionData[ipAddress]?.SizeData || [];
        const previousCompletedOrders = [];
        if (sizeData != null) {

          // First, update status where ActualCut equals SizeQty
          sizeData.forEach(item => {
            if (item.ActualCut === item.SizeQty) {
              item.Status = 'Complete';
            }
          });
          
          // Filter and map the completed orders
          const completedOrders = sizeData
            .filter(item => item.Status === 'Complete')
            .map(item => ({
              OrderID: item.OrderID,
              SizeID: item.SizeID,
              PartID: item.PartID,
              OperatorID : item.OperatorID
            }));
          
          if (completedOrders.length > 0) {
            for (const completeOrder of completedOrders) {
              const alreadyProcessed = previousCompletedOrders.some(prev =>
                prev.OrderID === completeOrder.OrderID &&
                prev.SizeID === completeOrder.SizeID &&
                prev.PartID === completeOrder.PartID && 
                prev.OperatorID === completeOrder.OperatorID
              );
          
              if (alreadyProcessed) continue;
          
              const distributionIDFromSize = await getDistributionIDFromSizeID(
                ipAddress,
                completeOrder.OrderID,
                isLeather === 1 ? 0 : 1,
                completeOrder.SizeID,
                completeOrder.PartID
              );
              
              if (distributionIDFromSize?.DistributionID?.length > 0) {
                try {
                  const { DistributionID }  = distributionIDFromSize.DistributionID[0];
                  
                  // 🔁 Get SubDistributions under this Distribution
                  const subList = await getSubDistributions(DistributionID);
              
                  if (!Array.isArray(subList) || subList.length === 0) {
                    console.warn(`⚠️ No subdistributions found for DistributionID: ${DistributionID}`);
                    await setDistributionIsComplete(DistributionID, 'Complete');
                  }
                  else {
                    let subCompletedCount = 0;
                    
                    for (const sub of subList.filter(s => s.IpAddress === ipAddress)) {
                      const status = sub.Status;
                      if (status !== 'Complete') {
                        await setSubDistributionComplete(sub.SubDistributionID, 'Complete');
                        console.log(`✅ SubDistribution ${sub.SubDistributionID} marked Complete`);
                        subCompletedCount++;
                      }
                    }
                    
                    const allSubCompleted = subList.every(sub =>
                      sub.Status === 'Complete'
                    );
                    
                    if (allSubCompleted) {
                      await setDistributionIsComplete(DistributionID, 'Complete');
                      console.log(`✅ Distribution ${DistributionID} marked Complete (all subs done)`);
                    } else if (subCompletedCount > 0) {
                      console.log(`🔁 Distribution ${DistributionID} has ${subCompletedCount} sub(s) completed`);
                    }
                  }
                  // ✅ Track completed order
                  previousCompletedOrders.push({
                    DistributionID,
                    OrderID: completeOrder.OrderID,
                    SizeID: completeOrder.SizeID,
                    PartID: completeOrder.PartID,
                    OperatorID : completeOrder.OperatorID
                  });
                } catch (error) {
                  console.error(
                    `❌ Error updating DistributionID: ${distributionIDFromSize?.DistributionID[0]?.DistributionID || 'Unknown'}`,
                    error
                  );
                }
              } else {
                //console.warn(`⚠️ No DistributionID found for OrderID=${completeOrder.OrderID}, SizeID=${completeOrder.SizeID}`);
              }
            }
          }
        }
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
async function processDistributionData(client, ipAddress, distributionData) {
  if (!distributionData) {
    console.warn(`No distribution data found for IP ${ipAddress}. Skipping save.`);
    return;
  }

  const writeRegister = async (address, value) => {
    await client.writeSingleRegister(address, value);
  };

  modbusClients[ipAddress].hasMultipleSOs = false;

  // Check for multiple Sales Orders
  const hasMultipleSOs = distributionData.OrderIDWithSOs && distributionData.OrderIDWithSOs.length >= 1;

  const clientData = modbusClients[ipAddress] ??= {};
  if (hasMultipleSOs) {
    if (
      Array.isArray(distributionData.OrderIDWithSOs) &&
      distributionData.OrderIDWithSOs.length > 0
    ) {
      if (clientData.indexMultipleSOs == null) {
        clientData.indexMultipleSOs = 0;
      }
      clientData.hasMultipleSOs = true;
      clientData.SOs = distributionData.OrderIDWithSOs;
      const soGroup = clientData.SOs[clientData.indexMultipleSOs]; 
      const totalSOs = clientData.SOs.length;
    
      if (clientData.indexMultipleSOs + 1 > totalSOs) {
        await writeRegister(1020, totalSOs);
      } else {
        await writeRegister(1020, clientData.indexMultipleSOs + 1);
      }
      await writeRegister(1021, totalSOs);
    
      // 🔒 Validate soGroup and soGroup.Data
      if (
        !soGroup ||
        !Array.isArray(soGroup.Data) ||
        soGroup.Data.length === 0
      ) {
        console.warn(`❌ Invalid SO group or empty Data array for IP: ${ipAddress}`);
        return;
      }
    
      clientData.operatorID = soGroup.Data[0].OperatorID;
      distributionData.Leather = soGroup.Data[0].IsLeather ? 2 : 1;
      try {
        // Write order ID to register 1000
        await writeRegister(1000, soGroup.Data[0].OrderID);
        await writeOperatorID(client, ipAddress, clientData.operatorID);
      } catch (err) {
        console.error(`❌ Failed to write OperatorID for IP ${ipAddress}:`, err.message);
      }
    } else {
      console.warn(`⚠️ distributionData.OrderIDWithSOs is empty or invalid for IP: ${ipAddress}`);
      return;
    }     
   // console.log(`Successfully wrote TotalSOs ${totalSOs} to register 112`);
   // console.log(`Successfully wrote DefaultOrderIndex ${modbusClients[ipAddress].indexMultipleSOs} to register 111`);

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
      //console.log("Total Sizes:", distributionData.SizeData);

      const registerSoAddress = [
        35, 40, 117, 122, 127, 132, 137, 142, 147, 152,
        157, 162, 167, 172, 177, 182, 187, 192, 197, 202
      ];

      // Write SOs to register addresses
      const uniqueSOs = [...new Set(modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs].Data.map(so => so.SO))];
      console.log(`Unique SOs:`, uniqueSOs);

      let currentRegisterIndex = 0;
      if (uniqueSOs.length > 0) {
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
            console.log(`CurrentRegisterIndex SO ${currentRegisterIndex} Address ${ipAddress}`);
          } catch (error) {
            console.error(`Error writing SO ${uniqueSOs[i]} to register: ${error.message}`);
          }
        }
      }
      // // Write PartName to register addresses
      // const clientEntry = modbusClients[ipAddress];

      if (
        !clientData ||
        !Array.isArray(clientData.SOs) ||
        clientData.SOs.length === 0 ||
        !clientData.SOs[clientData.indexMultipleSOs] ||
        !Array.isArray(clientData.SOs[clientData.indexMultipleSOs].Data)
      ) {
        console.warn(`[${ipAddress}] ❌ Missing or invalid SOs data`);
        return []; // or handle fallback logic
      }
      
      const uniquePartSOsMap = [
        ...new Set(
          clientData.SOs[clientData.indexMultipleSOs].Data.map(
            p => `${p.PartName}|${p.PartID}`
          )
        )
      ].map(item => {
        const [PartName, PartID] = item.split('|');
        return { PartName, PartID: parseInt(PartID) };
      });
      
      //console.log(`Unique Part:`, uniquePartSOsMap);
      // assign PartName position to switch out
      if (uniquePartSOsMap.length > 0) {
        if (modbusClients[ipAddress].indexMultiplePartNames == null) {
          modbusClients[ipAddress].indexMultiplePartNames = 0;
        }

        try {
          // write register partname position
          if (modbusClients[ipAddress].indexMultiplePartNames + 1 > uniquePartSOsMap.length) {
            await client.writeSingleRegister(1022, uniquePartSOsMap.length);
          } else {
            await client.writeSingleRegister(1022, modbusClients[ipAddress].indexMultiplePartNames + 1);
          }
          await delay(10);
          await client.writeSingleRegister(1023, uniquePartSOsMap.length);

       //   console.log(`Next value PartName:`, modbusClients[ipAddress].indexMultiplePartNames + 1);

          // display partName name
          const partDisplayStartRegister = distributionData.Leather === 2 ? 115 : 270;

          // filter out partName index
          if (distributionData.Leather === 2) {
           // distributionData.SizeData = distributionData.SizeDataDB;
            await client.writeSingleRegister(partDisplayStartRegister, uniquePartSOsMap.length);
          }
          else {
            const selectedPart = uniquePartSOsMap[modbusClients[ipAddress].indexMultiplePartNames];
            distributionData.SizeData = distributionData.SizeData.filter(item => item.PartName === selectedPart.PartName);
            let registerDisplayData = stringTo16BitArrayLittleEndian(selectedPart.PartName).slice(0, 10 * 2);
            // Write part name to registers sequentially
            for (let j = 0; j < registerDisplayData.length; j++) {
              await client.writeSingleRegister(partDisplayStartRegister + j, registerDisplayData[j]);
             // console.log(`Successfully wrote part name display to register ${partDisplayStartRegister + j}`);
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
               // console.log(`Successfully wrote part name to register ${startRegister + j}`);
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
              //  console.log(`✅ Wrote PartID ${partID} to register ${registerAddress}`);
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
            //  console.log(`✅ Wrote PartID ${partList.PartID} to register ${registerAddress}`);
            } catch (error) {
              console.error(`❌ Error writing PartID ${part?.PartID} at index ${i}:`, error.message);
            }
          }

        } catch (error) {
          console.error(`Error writing registers: ${error.message}`);
        }
      }
    }
   // console.log('Saving distribution data to Modbus...');
    await saveDistributionDataToModbus(client, ipAddress, distributionData);
  } else {
    console.warn(`No multiple Sales Orders detected for IP ${ipAddress}. Data will not be processed.`);
  }
}

/**
 * Adjusts Modbus index and stores it in modbusClients structure.
 * 
 * @param {number} value - Raw index value read from Modbus.
 * @param {string} ipAddress - IP address of the device.
 * @param {boolean} isMultipleSOs - True for SOs, false for PartNames.
 * @returns {number} adjusted index
 */
function adjustModbusIndex(value, ipAddress, isMultipleSOs) {
  if (typeof value !== 'number' || isNaN(value)) {
    console.warn(`[adjustModbusIndex] Invalid value received: ${value} for ${ipAddress}`);
    return 0;
  }

  const clients = modbusClients[ipAddress];
  if (!clients) {
    console.warn(`[adjustModbusIndex] No client found for ${ipAddress}`);
    return 0;
  }

  const adjusted = value !== 0 ? value - 1 : 0;

  if (isMultipleSOs) {
    clients.indexMultipleSOs = adjusted;
   // console.log(`[adjustModbusIndex] [${ipAddress}] indexMultipleSOs set to ${adjusted} (raw: ${value})`);
  } else {
    clients.indexMultiplePartNames = adjusted;
   // console.log(`[adjustModbusIndex] [${ipAddress}] indexMultiplePartNames set to ${adjusted} (raw: ${value})`);
  }

  return adjusted;
}



function indexCompleteOnes(binaryString) {
  let count = binaryString.split('').filter(bit => bit === '1').length;

  if (binaryString === '0000000000000000') {
    return -1; // Return -1 if all bits are 0
  }
  return count > 1 ? count - 1 : 0; // If at least two '1's exist, return count - 1, otherwise return 0
}

async function fetchStoreDistributionData(client, ipAddress, orderID, isLeather, distributionData) {
  while (true) {
    if (Object.keys(storeDistributionData[ipAddress]).length === 0) {
      if (isLeather === 1) {
        modbusClients[ipAddress].chooseSizeAddress = 3101;
        modbusClients[ipAddress].sizeAddress = 3102;
        //  storeDistributionData[ipAddress] = await getSizeAndDistributionDataFromDb(ipAddress, orderID, isLeather == 1 ? 0 : 1);
      } else {
        modbusClients[ipAddress].chooseSizeAddress = 3103;
        modbusClients[ipAddress].sizeAddress = 3104;
        //storeDistributionData[ipAddress] = await getSizeAndDistributionDataFromDb(ipAddress, orderID, isLeather == 1 ? 0 : 1);
      }
      // modbusClients SOs data if available
      let soData = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress]?.indexMultipleSOs]?.Data;

      // Check if it's null/undefined
      if (soData === undefined) {
        distributionData = await getDistributionDataFromDb(ipAddress);
        await processDistributionData(client, ipAddress, distributionData);

        // Refresh soData after async update
        soData = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress]?.indexMultipleSOs]?.Data;
      }
      if (Array.isArray(soData) && soData.length > 0) {
        storeDistributionData[ipAddress].SizeData = soData;
      }
      return storeDistributionData[ipAddress]; // Exit loop after fetching data
    } else {
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
async function checkBitOnOffRegister3000(client, ipAddress, register3000Address) {
  const previousSOIndex = 4;
  const deleteIndex = 2;
  const nextSOIndex = 6;
  const previousPartNameIndex = 8;
  const nextPartNameIndex = 10;
  try {
    // Read the register value from the device
    const response = await client.readHoldingRegisters(register3000Address, 1);
    const registerValue = response.response._body.values[0];
    
    // Convert register value to binary (16-bit)
    const binaryValue3000 = registerValue.toString(2).padStart(16, '0');
  //  console.log(`Register ${register3000Address} = ${registerValue}`);
   // console.log(`Binary 3000: ${binaryValue3000}`);
    
    const clientData = modbusClients[ipAddress];
    if (!clientData?.SOs?.length) return;
    
    const distributionData = await getDistributionDataFromDb(ipAddress);
    
    // Kiểm tra SO hiện tại
    let currentSOComplete = false;
    const currentSO = clientData.SOs[clientData.indexMultipleSOs];
    if (currentSO) {
      const data = currentSO.Data ?? [];
      currentSOComplete = Array.isArray(data) && data.every(
        item => item.Status === 'Complete' || item.Status === 'Stop'
      );
    }

    // Nếu tất cả SOs complete hoặc distributionData null
    if (currentSOComplete || !distributionData) {
      await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
      resetClientData(ipAddress, clientData);
      await delay(3000);
      return; // Nếu clear thì return luôn
    }

    // Check if distribution data has more SOs than client memory
    let isPendingSize = false;
    if (distributionData?.OrderIDWithSOs?.length > clientData.SOs.length) {
      isPendingSize = true;
    }

    // Nếu isPendingSize = true => Clear và return ngay
    if (isPendingSize) {
      await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
      resetClientData(ipAddress, clientData);
      return;
    }

    // Helper để reset dữ liệu client
    function resetClientData(ip, clientData) {
      storeDistributionData[ip] = {};
      clientData.previousSizeData = [];
      clientData.previousData = {};
      clientData.indexMultipleSOs = 0;
      clientData.indexMultiplePartNames = 0;
      if (clientData.sizeDataInfo) {
        clientData.sizeDataInfo.sizeID = [];
      }
    }



    if (isBitOn(binaryValue3000, previousSOIndex)) {
      await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
      let previous = Math.max(0, modbusClients[ipAddress].indexMultipleSOs - 1);

      modbusClients[ipAddress].indexMultipleSOs = previous;
    //  console.log(`Previous value ${previous} to register 3000 ${ipAddress}`);

      //await client.writeSingleRegister(1020, previous);
      storeDistributionData[ipAddress] = {};
      modbusClients[ipAddress].previousSizeData = [];
      modbusClients[ipAddress].previousData = {};
      modbusClients[ipAddress].sizeDataInfo.sizeID = [];
    }

    if (isBitOn(binaryValue3000, nextSOIndex)) {
      await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);

      const maxSOs = modbusClients[ipAddress].SOs.length;
      let next = Math.min(modbusClients[ipAddress].indexMultipleSOs + 1, maxSOs - 1);

    //  console.log(`Next value ${next} to register 3000 address ${ipAddress}`);

      modbusClients[ipAddress].indexMultipleSOs = next;
      //await client.writeSingleRegister(1020, next);
      storeDistributionData[ipAddress] = {};
      modbusClients[ipAddress].previousSizeData = [];
      modbusClients[ipAddress].previousData = {};
      modbusClients[ipAddress].sizeDataInfo.sizeID = [];
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
        await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
        modbusClients[ipAddress].indexMultipleSOs = 0;
        modbusClients[ipAddress].indexMultiplePartNames = 0;

        await client.writeSingleRegister(1020, 1);
        await client.writeSingleRegister(1021, 1);
        return;
      };
      // === Previous Logic ===
      if (isBitOn(binaryValue3000, previousPartNameIndex)) {
        await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);

        let previous = Math.max(0, modbusClients[ipAddress].indexMultiplePartNames - 1);

        modbusClients[ipAddress].indexMultiplePartNames = previous;
        //  await client.writeSingleRegister(1022, previous);

     //   console.log(`Previous value ${previous} to register 3000 address ${ipAddress}`);

        storeDistributionData[ipAddress] = {};
        modbusClients[ipAddress].previousSizeData = [];
        modbusClients[ipAddress].previousData = {};
      }

      // === Next Logic ===
      if (isBitOn(binaryValue3000, nextPartNameIndex)) {
        await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);

        const currentSO = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
        let uniquePartNameSOs = [];

        if (currentSO && Array.isArray(currentSO.Data)) {
          uniquePartNameSOs = [...new Set(currentSO.Data.map(p => p.PartName))];
        //  console.log(`Unique PartName Store:`, uniquePartNameSOs);
        } else {
          console.warn(`currentSO or currentSO.Data is undefined for IP: ${ipAddress}`);
        }

        const maxPartNames = uniquePartNameSOs.length;
        let next = Math.min(modbusClients[ipAddress].indexMultiplePartNames + 1, maxPartNames - 1);

        modbusClients[ipAddress].indexMultiplePartNames = next;
        // await client.writeSingleRegister(1022, next);
    //    console.log(`Next value ${next} maxPartNames:${maxPartNames} to register 3000 address ${ipAddress}`);

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
    if (modbusClients[ipAddress].SOs !== undefined) {
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

// delete data
async function clearDeleteBit(client, registerValue, address, deleteIndex) {
  const mask = 1 << deleteIndex;
  const valueToWrite = registerValue | mask;
  await client.writeSingleRegister(address, valueToWrite);
  await client.writeSingleRegister(1000, 0);
}

async function writeActualSizesForMultipleSOs(ipAddress, client, isLeather) {
  const hasMultipleSOs = modbusClients[ipAddress]?.hasMultipleSOs || false;
  if (!hasMultipleSOs) return;

  const currentSO = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
  if (currentSO == undefined) return;
  let uniquePartNameSOs = [];
  if (currentSO && Array.isArray(currentSO.Data)) {
    uniquePartNameSOs = [...new Set(currentSO.Data.map(p => p.PartName))];
   // console.log(`Unique PartName Store:`, uniquePartNameSOs);
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
  
  const fullSizeAddress = sizeInfo.sizeID;
  if (!Array.isArray(fullSizeAddress) || fullSizeAddress.length === 0) return;
  
  if (!modbusClients[ipAddress].previousSizeData) {
    modbusClients[ipAddress].previousSizeData = [];
  }
  
  const isLeatherMaterial = isLeather === 2;
  const typeMaterial = isLeatherMaterial ? 1 : 0;
  let baseActual = typeMaterial ? 922 : 794;
  
  try {
    let sizeGroups = [];
  
    if (isLeatherMaterial) {
      // Split into 2 arrays of 3 items
      sizeGroups = [
        fullSizeAddress.slice(0, 3),
        fullSizeAddress.slice(3, 6)
      ];
    } else {
      // Just one group of up to 6 items
      sizeGroups = [fullSizeAddress.slice(0, 6)];
    }
  
    // Iterate each group
    for (let g = 0; g < sizeGroups.length; g++) {
      const group = sizeGroups[g];
  
      await Promise.all(
        group.map(async (sizeAddressID, index) => {
          if (!sizeAddressID) return;
  
          const sizeID = await safeRead(sizeAddressID, client);
          if (!sizeID || sizeID === 0) {
            console.warn(`⚠️ Skipping invalid sizeID at group ${g} index ${index}`);
            return;
          }
  
          const matched = summedValues.find(item => parseInt(item.sizeID) === sizeID);
          if (!matched) return;
  
          let leatherSizeIDs = [45, 47, 49];
          let isLeather = leatherSizeIDs.includes(sizeAddressID);

          // Determine spacing rule
          if (isLeather) {
            baseActual = 215;
          }
          let sizeSpace = isLeather ? 20 : 16;
          const actualAddress = baseActual + sizeSpace * index;
        //const existing = modbusClients[ipAddress].previousSizeData.find(data => data.sizeID === sizeID);

        // const isSame =
        //   existing &&
        //   existing.actualCut === newData.actualCut &&
        //   existing.actualPieces === newData.actualPieces &&
        //   existing.actualQtys === newData.actualQtys;

        // if (!isSame) {

          await client.writeSingleRegister(actualAddress, matched.totalCuts);
          await client.writeSingleRegister(actualAddress + 4, matched.totalPieces);
          await client.writeSingleRegister(actualAddress + 8, matched.totalQtys);

        // console.log(`✅ Written values for sizeID ${sizeID} ${sizeAddressID} ${newData.actualQtys} ${actualAddress} at index ${index} IPAdress ${ipAddress}`);

        //   if (existing) {
        //     Object.assign(existing, newData);
        //   } else {
        //     modbusClients[ipAddress].previousSizeData.push(newData);
        //   }
        // } else {
        //   console.log(`⏭️ Skipped writing for sizeID ${sizeID}, no change in values.`);
        // }

  
          //console.log(`✅ Written sizeID ${sizeID} at group ${g}, index ${index}, address ${actualAddress}`);
        })
      );
    }
  } catch (err) {
    console.error("❌ Error during write:", err);
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

async function writeOperatorID(client, ipAddress, operatorID) {
  try {
    // Split 32-bit operatorID into two 16-bit values
    const lowRegister = operatorID & 0xFFFF;             // Lower 16 bits
    const highRegister = (operatorID >> 16) & 0xFFFF;    // Upper 16 bits

    // Write both registers starting at address 1018
    await client.writeMultipleRegisters(1018, [lowRegister, highRegister]);

    console.log(`✅ Wrote OperatorID (${operatorID}) to IP: ${ipAddress}`);
  } catch (error) {
    const errorMsg = `❌ Error writing OperatorID to IP ${ipAddress}: ${error.message}`;
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
    if (response == null) return;
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
    if (modbusClients[ipAddress].hasMultipleSOs) {
      processSizeID(ipAddress);
    }
    // Read OrderID from register 1000
    const orderIDData = await client.readHoldingRegisters(1000, 1);
    if (!orderIDData?.response?._body?.values?.[0]) {
     // console.log("Không thể đọc OrderID từ thanh ghi 1000");
      return;
    }

    let OrderID = orderIDData.response._body.values[0];
    const baseAddress = isLeather ? 966 : 886;
    let baseActual = isLeather ? 922 : 794;
    let baseSizeQtyAddress = isLeather ? 918 : 790;
    let collectPartAndOrderID;
    let operatorID;
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
   // console.log(`Output of size at register address ${modbusClients[ipAddress].chooseSizeAddress} = ${registerChooseSizeValue}`);
   // console.log(`Binary representation: ${binaryChooseSizeValue}`);

    const index = findSetBitIndex(binaryChooseSizeValue);
    if (index === -1) {
      return;
    }
    if (sizeInfo === undefined || sizeInfo.sizeID == undefined || sizeInfo.sizeID[index] == null) return;

    const sizeAddressID = sizeInfo.sizeID[index];
    let sizeQtyAddress;
    let actualAddress;
    try {
      const sizeIDValue = parseInt(sizeAddressID);
      const leatherSizeIDs = [45, 47, 49];
      const isLeatherSize = leatherSizeIDs.includes(sizeIDValue);

      if (isLeatherSize) {
        const customIndex = getLeatherIndex(sizeIDValue);

        baseSizeQtyAddress = 211;
        baseActual = 215;

        sizeQtyAddress = baseSizeQtyAddress + 20 * customIndex;
        actualAddress = baseActual + 20 * customIndex;
      }
      else {
        sizeQtyAddress = baseSizeQtyAddress + 16 * index;
        actualAddress = baseActual + 16 * index;
      }

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
      //  console.log(`[sizeCompleteID] Added: ${sizeID}`);
      }

      completeSizeCount++;

      // ----- Multiple SO logic -----
      let partID;
      if (modbusClients[ipAddress].hasMultipleSOs != null) {
        let checkPendingSize;
        if (!isLeather) {
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
                OrderID: item.OrderID,
                SizeID: item.SizeID,
                OperatorID: item.OperatorID
              }))
              .reduce((acc, curr) => {
                const key = `${curr.PartID}-${curr.OrderID}-${curr.SizeID}-${curr.OperatorID}`;
                if (!acc.map.has(key)) {
                  acc.map.set(key, true);
                  acc.result.push(curr);
                }
                return acc;
              }, { map: new Map(), result: [] }).result
          ) || [];              
        }

        if (checkPendingSize.length == 0) return;
        checkPendingSize = checkPendingSize[0];
        sizeID = checkPendingSize.SizeID;
        OrderID = checkPendingSize.OrderID;
        partID = checkPendingSize.PartID;
        operatorID = checkPendingSize.OperatorID;

        // collect total size complete actualSizeQty and actualCut
        let totalCompletedSize;
        if (!isLeather) {
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

        modbusClients[ipAddress].storedActualCut = actualCut ?? 0;
        modbusClients[ipAddress].storedActualSizeQty = actualSizeQty ?? 0;
        modbusClients[ipAddress].storedActualPieces = actualPieces ?? 0;

        let sizeRemain;
        if ((checkPendingSize.InventoryQty + actualSizeQty) >= checkPendingSize.SizeQty && checkPendingSize.InventoryQty !== 0) {
           // Save total actual size qty to modbus client
          const totalQty = checkPendingSize.InventoryQty + actualSizeQty;

          modbusClients[ipAddress].storedActualSizeQty = totalQty;
          //totalCompletedSize.totalSizeQty = totalQty;
        }
        const completedQty = Number(totalCompletedSize.totalSizeQty) || 0;

        sizeRemain = completedQty;

       // console.log(`sizeRemain [Address] ${ipAddress} ${sizeRemain} ActualCut ${actualCut} actualSizeQty ${actualSizeQty}`);

        const availableQty = modbusClients[ipAddress].storedActualSizeQty - sizeRemain;

        if (availableQty < 0) {
          return;
        }

        if (availableQty >= checkPendingSize.SizeQty) {
          if (isLeather) {
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
                  if (actualSizeQty > checkPendingSize.SizeQty) {
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
            if (actualSizeQty > checkPendingSize.SizeQty) {
              checkPendingSize.ActualSizeQty = checkPendingSize.SizeQty;
            } else {
              checkPendingSize.ActualSizeQty = modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
            }
           // console.log(`Updated status to Complete for SizeID: ${sizeID}, OrderID: ${OrderID}`);
          }
          if (totalCompletedSize.totalSizeQty !== 0) {
            if (isLeather) {
              actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
              actualPieces = checkPendingSize.TotalPiecesPerPair * actualSizeQty + actualCut;
            }
            else {
              actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
              modbusClients[ipAddress].storedActualCut -= totalCompletedSize.totalActualCut;
              actualCut = modbusClients[ipAddress].storedActualCut;
              actualPieces = (checkPendingSize.MaterialLayer * checkPendingSize.CuttingDieQty) * actualCut;
            }
          } else {
            if (isLeather) {
              actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
              actualPieces = checkPendingSize.TotalPiecesPerPair * actualSizeQty + actualCut;
            }
            else {
              actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, checkPendingSize, totalCompletedSize);
              actualCut -= totalCompletedSize.totalActualCut;
              actualPieces = (checkPendingSize.MaterialLayer * checkPendingSize.CuttingDieQty) * actualCut;
            }
          }
        //  console.log(`Total Complete => ActualCut [Address] ${ipAddress} ${actualCut} actualSizeQty ${actualSizeQty}`);
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
        // console.log(`ActualCut [Address] ${ipAddress} actualSizeQty ${actualSizeQty}`);
        }
      }
      if (collectPartAndOrderID !== undefined && collectPartAndOrderID.length > 0) {
        for (const item of collectPartAndOrderID) {
          processActualDataChange(ipAddress, sizeID, item.PartID, piecesPerPair, materialLayer, cuttingDieQty, actualCut, actualPieces, actualSizeQty, totalPieces, isLeather, item.OrderID, operatorID);
        }
      } else {
        //const subDist = findSubDistribution(ipAddress, sizeID, item.PartID, item.OrderID, operatorID);
        processActualDataChange(ipAddress, sizeID, partID, piecesPerPair, materialLayer, cuttingDieQty, actualCut, actualPieces, actualSizeQty, totalPieces, isLeather, OrderID, operatorID);
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

function getLeatherIndex(sizeIDValue) {
  const leatherMap = {
    45: 0,
    47: 1,
    49: 2
  };

  return leatherMap[sizeIDValue] ?? 0; // default to 0 if not found
}

function findSubDistribution(ip, sizeID, partID, orderID, operatorID) {
  const allSOData = modbusClients[ip]?.SOs?.flatMap(so => so.Data || []);
  return allSOData?.find(x =>
    x.SizeID === sizeID &&
    x.PartID === partID &&
    x.OrderID === orderID &&
    x.OperatorID === operatorID &&
    x.IsSub === true
  );
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
  OrderID, 
  operatorID
) {
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
    OrderID: OrderID,
    OperatorID: operatorID
  };

  if (!modbusClients[ipAddress]) {
    console.warn(`modbusClients[${ipAddress}] is undefined.`);
    return;
  }

  modbusClients[ipAddress].previousData ||= {};
  const prevData = modbusClients[ipAddress].previousData[sizeID] || {};
  const isNewData = !modbusClients[ipAddress].previousData[sizeID];
  let hasChanges = false;

  hasChanges = isNewData || JSON.stringify(newData) !== JSON.stringify(prevData);

  if (hasChanges) {
   // console.log(`Data changed or first-time load for sizeID ${sizeID}, updating DB...`, newData);

    // Calculate cut delta
    // const previousCut = prevData?.ActualCut ?? 0;
    // const cutDelta = actualCut - previousCut;

   // if (cutDelta > 0) {
    const cutDate = new Date().toISOString().split("T")[0]; // format YYYY-MM-DD

    //console.log('Nhat: ' + operatorID);
    if (!operatorID) {
      console.warn(`[sizeID] Missing operatorID for IP: ${ipAddress}, skipping OrderID: ${OrderID}, SizeID: ${sizeID}, PartID: ${partID}`);
      return;
    }
    // Insert into CutHistory
    await logCutHistoryToDB({
      OrderID: OrderID,
      PartID: partID,
      SizeID: sizeID,
      CutQuantity: actualCut,
      CutDate: cutDate,
      EmployeeID: operatorID
    });

    // Save actual data
    await saveActualDataToDB({
      OrderID: OrderID,
      OperatorID: operatorID,
      IsLeather: isLeather,
      SizeData: [newData],
    });

    if (!modbusClients[ipAddress] || typeof modbusClients[ipAddress] !== 'object') {
      console.warn(`modbusClients[${ipAddress}] is invalid. Re-initializing.`);
      modbusClients[ipAddress] = {};
    }
    
    if (!modbusClients[ipAddress].previousData || typeof modbusClients[ipAddress].previousData !== 'object') {
      modbusClients[ipAddress].previousData = {};
    }
    // Update cached data
    modbusClients[ipAddress].previousData[sizeID] = {
      ...newData,
      OrderID: OrderID,
      OperatorID: operatorID
    };
  }
}

// proccess to mutiple SO size -- save on DB
async function processSizeID(ipAddress) {
  if (!modbusClients[ipAddress]?.SOs?.length) {
   // console.log(`[sizeID] No SOs data available for IP: ${ipAddress}`);
    return;
  }

  // Extract OrderID and SizeID pairs from SOs.Data array
  const validPairs = Array.from(
    new Map(
      modbusClients[ipAddress].SOs.flatMap(so =>
        (so.Data || []).map(dataItem => {
          const key = `${dataItem.OrderID}-${dataItem.SizeID}-${dataItem.PartID}`;
          return [key, {
            OrderID: dataItem.OrderID,
            SizeID: dataItem.SizeID,
            PartID: dataItem.PartID,
            ActualCut: dataItem.ActualCut,
            CuttingDieQty: dataItem.CuttingDieQty,
            PiecesPerPair: dataItem.PiecesPerPair,
            MaterialLayer: dataItem.MaterialLayer,
            TotalPiecesPerPair: dataItem.TotalPiecesPerPair,
            OperatorID: dataItem.OperatorID,
            Leather: dataItem.IsLeather
          }];
        })
      )
    ).values()
  );
  
  for (const { OrderID, SizeID, PartID, ActualCut, CuttingDieQty, PiecesPerPair, MaterialLayer, TotalPiecesPerPair, OperatorID, Leather } of validPairs) {

    for (const so of modbusClients[ipAddress]?.SOs || []) {
      const match = (so.Data || []).find(item =>
        item.OrderID === OrderID &&
        item.SizeID === SizeID &&
        item.PartID === PartID && 
        item.OperatorID === OperatorID
      );
      if (match) {
        match.ActualCut = typeof match.ActualCut === 'number' ? match.ActualCut : 0;
    //    console.log(`Set ActualCut for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
      }
    }
    // Check if this pair was already processed
    if (ActualCut !== null) {
      //console.log(`[sizeID] Skipping duplicate OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
      continue; // Skip processing if already saved
    }

  // console.log(`[sizeID] Processing OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}, Leather ${Leather}`);

    const newData = {
      SizeID,
      PartID,
      PiecesPerPair: PiecesPerPair,
      MaterialLayer: MaterialLayer,
      CuttingDieQty: CuttingDieQty,
      ActualCut: 0,
      ActualPieces: 0,
      ActualSizeQty: 0,
      TotalPiecesPerPair: TotalPiecesPerPair
    };

    try {
      if (!OperatorID) {
        console.warn(`[sizeID] Missing operatorID for IP: ${ipAddress}, skipping OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
        return;
      }
      await saveActualDataToDB({
        OrderID,
        OperatorID : OperatorID,
        IsLeather: Leather,
        SizeData: [newData],
      });
    //  console.log(`[sizeID] Successfully saved data for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}`);
    } catch (error) {
      console.error(`[sizeID] Error saving data for OrderID: ${OrderID}, SizeID: ${SizeID}, PartID: ${PartID}: ${error.message}`);
    }
  }
}

/**
 * Continuously writes an incrementing counter to the given register,
 * reconnecting automatically if the client is disconnected.
 */
async function writeToModbusRegister(ipAddress, registerAddress = 8000) {
  let counter = 0;

  async function writeLoop() {
    try {
      if (typeof ipAddress === 'object') {
        delete modbusClients[ipAddress];
        return;
      }
      let entry = modbusClients[ipAddress];

      // 🔁 Reconnect if necessary
      if (!entry || !entry.client || entry.isConnected === false) {
        console.warn(`[${ipAddress}] No connection — attempting reconnect before write...`);
        try {
          await connectToDevice(ipAddress);
          entry = modbusClients[ipAddress];
          if (!entry || !entry.client) {
            return;
          }
        } catch (connErr) {
          const reconnectErrMsg = `[${ipAddress}] Reconnect failed: ${connErr.message}`;
          console.error(reconnectErrMsg);
          logToFile(errorLogPath, reconnectErrMsg);
          return setTimeout(writeLoop, 1000); // retry later
        }
      }

      // 🔢 Increment and wrap the counter
      counter = (counter + 1) % 60000;

      // ✍️ Write to register
      await entry.client.writeSingleRegister(registerAddress, counter);
      console.log(`[${ipAddress}] Wrote value ${counter} to register ${registerAddress}`);
    } catch (err) {
      const errorMessage = `[${ipAddress}] Error writing to register ${registerAddress}: ${err.message}`;
      console.error(errorMessage);
      logToFile(errorLogPath, errorMessage);
    } finally {
      setTimeout(writeLoop, 1000); // Loop every second
    }
  }

  writeLoop(); // 🔁 Start the loop
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
//    console.log(`IP Address: ${ipAddress}`);

    // Đọc dữ liệu OrderID từ Modbus
    const [orderIdData] = await Promise.all([client.readHoldingRegisters(6507, 1)]);
    const partName = modbusClients[ipAddress]?.partName;
 //   console.log(`PartName on write: ${partName}`);

    // Kiểm tra OrderID hợp lệ
    const orderID = parseInt(orderIdData.response._body.values[0], 10);
    if (isNaN(orderID) || orderID <= 0) {
      console.error(`Invalid OrderID: ${orderID}`);
      return;
    }
  //  console.log(`OrderID: ${orderID}`);

    // Kiểm tra partName hợp lệ
    if (!partName || partName.trim() === "") {
      console.error('Invalid PartName, skipping database query.');
      return;
    }

  //  console.log(`Fetching size data with OrderID: ${orderID} and PartName: ${partName}`);

    // Lấy dữ liệu kích thước từ cơ sở dữ liệu
    const sizeData = await getSizeDataFromDB(ipAddress, orderID, partName);

    if (!sizeData || sizeData.length === 0) {
     // console.log('No size data found.');
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
       //   console.log(`Successfully written data to registers starting from ${registerAddress} for size ${Size}`);
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

async function saveDistributionDataToModbus(client, ipAddress, data) {
  // const modbusClient = await connectToDevice(ipAddress);
  // if (!modbusClient || !modbusClient.client) {
  //   console.error("modbusClient or modbusClient.client is undefined.");
  //   return;
  // }
  // const client = modbusClient.client;

  if (!client || !client.writeSingleRegister) {
    console.warn(`Modbus client not properly initialized for device at ${ipAddress}`);
  }

  try {

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
      //    console.log(`The covert text:  ${convert16BitArrayToString(registerData)} ${startRegister + j} ${registerData[j]}`);
          await client.writeSingleRegister(startRegister + j, registerData[j]);
        //  console.log(`Successfully wrote to register ${startRegister + j}`);
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
       // console.log(`Successfully wrote Leather data to register ${startRegister} with value ${leatherData} ${ipAddress}`);
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
        //  console.log(`Successfully wrote materialCode ${materialCodeArray[i]} to register ${registerAddress}`);
        }
      } catch (error) {
        console.error(`Error writing materialCode: ${error.message}`);
      }
    });

    await Promise.all(materialCodePromises);

    // Write defaultValue
    const defaultValue = modbusClients[ipAddress].SOs[modbusClients[ipAddress].indexMultipleSOs];
    if (defaultValue == undefined) return;
    if (data.Leather == 1) {
      const BASE_REGISTER = 886;
      try {
        await client.writeSingleRegister(BASE_REGISTER, defaultValue.Data[0].PiecesPerPair ?? 0);
        await client.writeSingleRegister(BASE_REGISTER + 4, defaultValue.Data[0].MaterialLayer ?? 0);
        await client.writeSingleRegister(BASE_REGISTER + 8, defaultValue.Data[0].CuttingDieQty ?? 0);
     //   console.log(`✅ Successfully wrote to Modbus registers (Leather == 1), Register ${BASE_REGISTER}, Address ${ipAddress}`);
      } catch (error) {
        console.error("❌ Error writing to Modbus registers (Leather == 1): ", error);
      }
    } else {
      const BASE_REGISTER = 966; // Đảm bảo có dữ liệu để lặp qua
      try {
        await client.writeSingleRegister(BASE_REGISTER, defaultValue.Data[0].TotalPiecesPerPair ?? 0);
    //    console.log("✅ Successfully wrote to Modbus registers (Leather != 1)");
      } catch (error) {
        console.error("❌ Error writing to Modbus registers (Leather != 1): ", error);
      }
    }

    // Write SizeData
    await writeRegisterSizeData(client, ipAddress, data.SizeData, data.Leather);
    await writeActualSizesForMultipleSOs(ipAddress, client, data.Leather);
  //  console.log('Data successfully saved to Modbus');
  } catch (error) {
    console.error(`Error saving distribution data to Modbus: ${error.message}`);
  }
}


// Ghi size data vào thanh ghi 
async function writeRegisterSizeData(client, ipAddress, sizeData, isLeather) {
  // Ensure sizeData is an array and limit its length based on isLeather
  if (!Array.isArray(sizeData)) return;

  const chunkSize = 6;
  let processedSizeData = [];
  
  if (Array.isArray(sizeData)) {
      //const index = parseInt(modbusClients[ipAddress]?.indexMultipleSOs, 10) || 0;

      // Define fixed slice ranges
      //const startIndex = index * chunkSize;
      //const endIndex = startIndex + chunkSize;
  
      processedSizeData = sizeData.slice(0, chunkSize);
  } else {
    console.warn('sizeData is not an array:', sizeData);
  }

  // Assign values correctly
  modbusClients[ipAddress].sizeDataInfo.sizeCount = processedSizeData.length;
  modbusClients[ipAddress].sizeDataInfo.isLeather = isLeather;

 // console.log(`[sizeDataInfo] Number of size ${modbusClients[ipAddress].sizeDataInfo.sizeCount} and isLeather ${isLeather}`);

  let registerSize = [];
  if (isLeather == 2) {
    // Leather sizes
    await client.writeSingleRegister(1003, processedSizeData.length);
    registerSize = [
      { SizeID: 900, Size: 906, SizeQty: 918, InventoryQty: 79 },
      { SizeID: 902, Size: 910, SizeQty: 934, InventoryQty: 83 },
      { SizeID: 904, Size: 914, SizeQty: 950, InventoryQty: 87 },
      { SizeID: 45, Size: 207, SizeQty: 211, InventoryQty: 302 },
      { SizeID: 47, Size: 227, SizeQty: 231, InventoryQty: 306 },
      { SizeID: 49, Size: 247, SizeQty: 251, InventoryQty: 310 },
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

   // console.log(`Writing to Modbus: SizeID = ${item.SizeID}, Size = ${item.Size}, SizeQty = ${item.SizeQty}, InventoryQty = ${item.InventoryQty}, Address ${ipAddress}`);

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
      //  console.log(`[sizeDataInfo] sizeID ${registerSize[i].SizeID}`);
       // console.log(`Writing to register ${registerSize[i].SizeID}, value: ${item.SizeID}`);
      }

      for (let j = 0; j < registerSizeData.length; j++) {
     //   console.log(`Writing to register ${startSizeRegister + j}, value: ${registerSizeData[j]}`);
        await client.writeSingleRegister(startSizeRegister + j, registerSizeData[j]);
      }

      //console.log(`Writing to register ${registerSize[i].SizeQty}, value: ${item.SizeQty}, Address ${ipAddress}`);
     // console.log(`Writing to register ${registerSize[i].InventoryQty}, value: ${item.InventoryQty}`);

      await client.writeSingleRegister(registerSize[i].SizeQty, item.SizeQty || 0);
      await client.writeSingleRegister(registerSize[i].InventoryQty, item.InventoryQty || 0);
   //   console.log(`Successfully written to Modbus for SizeID ${item.SizeID}`);
    } catch (error) {
      console.error(`Error writing to Modbus for SizeID ${item.SizeID}:`, error);
    }
  }
}

async function closeAllConnections() {
  for (const ipAddress in modbusClients) {
    try {
      const modbusClient = modbusClients[ipAddress];
      if (!modbusClient) continue;

      await updateDeviceConnectionStatus(ipAddress, false).catch((err) => {
        console.error(`Error updating device connection status for ${ipAddress}: ${err.message}`);
      });

      if (modbusClient.socket && !modbusClient.socket.destroyed) {
        modbusClient.socket.end();
        modbusClient.socket.destroy();
     //   console.log(`🔒 Socket connection destroyed for ${ipAddress}`);
      }

      delete modbusClient.client;
      delete modbusClient.socket;
      delete modbusClient.sizeDataInfo;
      modbusClient.isConnected = false;
      modbusClient.isDisconnected = true;

      logToFile(successLogPath, `Closed connection to device at ${ipAddress}`);
    } catch (err) {
      console.error(`❌ Failed to close connection for ${ipAddress}: ${err.message}`);
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
// fakeHMI.js
// const net = require('net');
// const { performance } = require('perf_hooks');

// const NUM_DEVICES = 170;
// const START_PORT = 15000;
// const fakeDevices = [];

// function createFakeHMIs() {
//   for (let i = 0; i < NUM_DEVICES; i++) {
//     const port = START_PORT + i;
//     fakeDevices.push({ ip: '127.0.0.1', port });

//     const server = net.createServer(socket => {
//       console.log(`📡 HMI Device connected on port ${port}`);

//       socket.on('data', data => {
//         // Simulate Modbus response
//         socket.write(data);
//       });

//       socket.on('close', () => {
//         console.log(`❌ HMI Device disconnected on port ${port}`);
//       });
//     });

//     server.listen(port, '127.0.0.1', () => {
//       console.log(`✅ Fake HMI listening on port ${port}`);
//     });
//   }
// }

// function connectToDevice(ip, port) {
//   return new Promise((resolve, reject) => {
//     const socket = new net.Socket();
//     socket.setTimeout(2000);

//     socket.connect(port, ip, () => {
//       socket.write(Buffer.from('010300000001840A', 'hex')); // sample Modbus read
//       socket.once('data', () => {
//         socket.destroy();
//         resolve();
//       });
//     });

//     socket.on('error', reject);
//     socket.on('timeout', () => {
//       socket.destroy();
//       reject(new Error('Timeout'));
//     });
//   });
// }

// async function testConnections() {
//   const start = performance.now();

//   const results = await Promise.allSettled(
//     fakeDevices.map(({ ip, port }) => connectToDevice(ip, port))
//   );

//   const end = performance.now();
//   const duration = Math.round(end - start);

//   const failed = results.filter(r => r.status === 'rejected');
//   console.log(`\n⏱️ Total time for ${NUM_DEVICES} HMIs: ${duration} ms`);
//   console.log(`✅ Success: ${NUM_DEVICES - failed.length}`);
//   console.log(`❌ Failed: ${failed.length}`);
// }

// // Run
// createFakeHMIs();

// setTimeout(() => {
//   testConnections();
// }, 2000); // Give servers time to start
