const fs = require('fs');
const net = require('net');
const Modbus = require('jsmodbus');
const { ModbusPollingManager, isHMIConnected } = require('./ModbusPollingManager');
const manager = new ModbusPollingManager(200);
const { updateDeviceConnectionStatus, getDistributionIDFromSizeIDNoneStatus, getSizeDataFromDB, getDistributionDataFromDb, saveActualDataToDB, setDistributionIsComplete, setSubDistributionComplete, getSubDistributions, getSizeAndDistributionDataFromDb, getDistributionIDFromSizeID, getDistributionCompleteFromDb, logCutHistoryToDB } = require('./database');
//const { notifyClientsToDeleteOrder } = require('./notifications');

let countRemainSizeData = {};
let storeDistributionData = {};
let isDelete = true;
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

/**
 * Attempts a single connection to a Modbus device.
 * This function encapsulates the Promise logic for one attempt.
 * @param {string} ipAddress The IP address.
 * @param {object} entry The client entry object from modbusClients.
 */
async function attemptSingleConnection(ipAddress, entry) {
  return new Promise((resolve) => {
    function connect() {
      const socket = new net.Socket();
      const client = new Modbus.client.TCP(socket, 1);
      const CONNECT_TIMEOUT = 60000;

      // Reset state
      entry.isConnected = false;

      // Timeout handler
      socket.setTimeout(CONNECT_TIMEOUT);
      socket.once('timeout', () => {
        // console.warn(`[${ipAddress}] ⚠️ Connection timed out after ${CONNECT_TIMEOUT}ms`);
        delete modbusClients[ipAddress].socket
      });

      // First connection error
      socket.once('error', (err) => {
        //   console.warn(`[${ipAddress}] ⚠️ Socket error: ${err.message}`);
        delete modbusClients[ipAddress].socket
      });

      // On connect
      socket.connect({ host: ipAddress, port: 502 }, () => {
        socket.removeAllListeners('timeout');
        entry.isConnected = true;

        // Monitor errors after connected
        socket.on('error', (err) => {
          console.warn(`[${ipAddress}] ⚠️ Socket error during session: ${err.message}`);
          //  entry.isConnected = false;
          delete modbusClients[ipAddress].socket
        });

        socket.on('close', () => {
          // console.warn(`[${ipAddress}] ⚠️ Socket closed. Retrying in 5s...`);
          entry.isConnected = false;
          delete modbusClients[ipAddress].socket
          //  setTimeout(connect, 2000); // retry
        });

        resolve({ client, socket });
      });
    }
    if (!entry.isConnected) {
      updateDeviceConnectionStatus(ipAddress, false);
    }
    connect();
  });
}

/**
 * The primary and ONLY function to call to initiate a connection.
 * It handles state, retries, and race conditions gracefully.
 * @param {string} ipAddress The IP address of the device.
 */
async function connectToDevice(ipAddress) {
  // if (ipAddress !== '10.30.4.195') return;
  const entry = modbusClients[ipAddress] = modbusClients[ipAddress] || {};

  // Prevent duplicate connects
  if (entry.isConnected && entry.socket?.readyState === 'open') {
    return { client: entry.client, socket: entry.socket };
  }

  try {
    const { client, socket } = await attemptSingleConnection(ipAddress, entry);

  //  console.log(`[${ipAddress}] ✅ Connection successful.`);
    logToFile(successLogPath, `Connected to device at ${ipAddress}`);

    await updateDeviceConnectionStatus(ipAddress, true);
    await startReadingRegisters(client, socket, ipAddress);

    return { client, socket };
  } catch (error) {
 //   console.error(`[${ipAddress}] ❌ Connection failed: ${error.message}`);
    logToFile(errorLogPath, `[${ipAddress}] Connection failed: ${error.message}`);

    entry.isConnected = false;
    await updateDeviceConnectionStatus(ipAddress, false);

    // ❌ remove while(true): let manager’s 'reconnect' handle retries
    return null;
  }
}

async function startReadingRegisters(client, socket, ipAddress) {
  const entry = modbusClients[ipAddress];

  try {
    Object.assign(entry, {
      client,
      socket,
      isConnected: true,
      isDisconnected: false,
      sizeDataInfo: {},
    });
  } catch (err) {
    console.error(`[${ipAddress}] Connection failed: ${err.message}`);
    Object.assign(entry, {
      isConnected: false,
      isDisconnected: true,
    });
    delete entry.client;
    delete entry.socket;
  } finally {
    manager.registerClient(ipAddress, entry);
  }
}
manager.on('poll', async (entry, ip) => {
  if (!entry || !entry.isConnected || typeof entry.client.readHoldingRegisters !== "function") {
    console.warn(`[${ip}] Poll skipped: no active client`);
    return;
  }

  try {
    await checkAndSaveDistribution(entry.client, ip).catch(err => {
      console.error(`[${ip}] Error in checkAndSaveDistribution: ${err.message}`);
    });
  } catch (err) {
    console.error(`[${ip}] Error in poll: ${err.message}`);
  }
});

manager.on('readActual', async (entry, ip) => {
  if (!entry || entry.isDisconnected || typeof entry.client.readHoldingRegisters !== "function") {
    console.warn(`[${ip}] ReadActual skipped: device is marked as disconnected`);
    return;
  }
  await readActualData(entry.client, ip);
});

manager.on('checkConnection', async (entry, ip) => {
  if (!isHMIConnected(entry, ip)) return;
  try {
    await writeToModbusRegister(entry.client, entry, entry.isConnected, ip);
  } catch (err) {
    console.warn(`[${ip}] ⚠️ Initial register write failed: ${err.message}`);
  }
});

manager.on('reconnect', async (ip) => {
  try {
    const result = await connectToDevice(ip);
    if (!result) throw new Error('connectToDevice returned null');

    const { client, socket } = result;
    const entry = modbusClients[ip] ?? {};

    Object.assign(entry, {
      client,
      socket,
      isConnected: true,
      isDisconnected: false,
    });

    modbusClients[ip] = entry;
    manager.registerClient(ip, entry); // optional: re-register if needed
    console.log(`[${ip}] Successfully reconnected.`);
  } catch (err) {
    console.error(`[${ip}] Reconnect failed: ${err.message}`);
    const entry = modbusClients[ip] ?? {};
    entry.isConnected = false;
    entry.isDisconnected = true;
    delete entry.client;
    delete entry.socket;
    modbusClients[ip] = entry;
    manager.registerClient(ip, entry); // optional: re-register failed state
  }
});
async function checkAndSaveDistribution(client, ipAddress) {
  try {
    // Read register 1000
    let response = await client.readHoldingRegisters(1000, 1);
    if (!response) return;
    let orderID = response.response._body.values[0];
    //  console.log(`Register 1000 value: ${orderID}`);

    // check complete order
    let responseLeather = await client.readHoldingRegisters(1001, 1);
    let isLeather = responseLeather.response._body.values[0];
    // console.log(`Register 1001 value: ${isLeather}`);
    // check register 3000 status
    checkBitOnOffRegister3000(client, ipAddress, register3000Address);

    let distributionData;
    if (orderID === 0) {
      //  console.log(`Register 1000 is 0. Fetching distribution data for IP: ${ipAddress}`);

      // Fetch distribution data from DB
      distributionData = await getDistributionDataFromDb(ipAddress);
      processDistributionData(client, ipAddress, distributionData, false);
    }
    else {
      //get index SOs and Part if available
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
      if (!modbusClients[ipAddress]?.SOs || modbusClients[ipAddress].SOs.length === 0) {
        return;
      }
      // if (ipAddress !== '10.30.4.195') return;
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
        processDistributionData(client, ipAddress, distributionData, true);
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
            if (item.ActualSizeQty >= item.SizeQty) {
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
              OperatorID: item.OperatorID
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
                  const { DistributionID } = distributionIDFromSize.DistributionID[0];

                  // 🔁 Get SubDistributions under this Distribution
                  const subList = await getSubDistributions(DistributionID);

                  if (!Array.isArray(subList) || subList.length === 0) {
                    // console.warn(`⚠️ No subdistributions found for DistributionID: ${DistributionID}`);
                    await setDistributionIsComplete(DistributionID, 'Complete');
                  }
                  else {
                    let subCompletedCount = 0;

                    for (const sub of subList.filter(s => s.IpAddress === ipAddress)) {
                      //const status = sub.Status;
                      // if (status !== 'Complete') {
                      sub.Status = "Complete";
                      await setSubDistributionComplete(sub.SubDistributionID, 'Complete');
                      console.log(`✅ SubDistribution ${sub.SubDistributionID} marked Complete`);
                      subCompletedCount++;
                      // }
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
                    OperatorID: completeOrder.OperatorID
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
async function getClient(ipAddress, unitId = 1, port = 502) {
  let entry = modbusClients[ipAddress];

  // Reuse if already connected
  if (entry && entry.isConnected) {
    return entry;
  }

  // Create socket
  const socket = new net.Socket();
  const client = new Modbus.client.TCP(socket, unitId);

  return new Promise((resolve, reject) => {
    socket.connect({ host: ipAddress, port }, () => {
      console.log(`[${ipAddress}] ✅ Connected`);

      entry = { socket, client, isConnected: true };
      modbusClients[ipAddress] = entry;

      // handle socket close
      socket.on("close", () => {
        console.warn(`[${ipAddress}] ⚠️ Connection closed`);
        modbusClients[ipAddress].isConnected = false;
      });

      // handle socket error
      socket.on("error", (err) => {
        console.error(`[${ipAddress}] ❌ Socket error: ${err.message}`);
        modbusClients[ipAddress].isConnected = false;
      });

      resolve(entry);
    });

    socket.once("error", (err) => {
      reject(new Error(`[${ipAddress}] ❌ Connection error: ${err.message}`));
    });
  });
}

// 🔧 helper: delay
function delay(ms) {
  return new Promise(res => setTimeout(res, ms));
}

// 🔧 helper: safe batch write
async function batchWrite(client, writes, batchSize = 10, batchDelay = 50) {
  // if (!client.isOpen && !client.isConnected) {
  //  // console.warn("⚠️ Cannot write — Modbus client not connected.");
  //   return;
  // }
  for (let i = 0; i < writes.length; i += batchSize) {
    const chunk = writes.slice(i, i + batchSize);

    try {
      const results = await Promise.allSettled(
        chunk.map(({ addr, val }) =>
          client.writeSingleRegister(addr, val)
        )
      );

      results.forEach((res, idx) => {
        const { addr, val } = chunk[idx];
        if (res.status === "rejected") {
          console.error(`❌ Error writing ${val} to register ${addr}:`, res.reason?.message || res.reason);
        }
      });
    } catch (err) {
      // chỉ bắt lỗi bất ngờ (không thuộc reject của từng promise)
      console.error("⚠️ Batch write failed unexpectedly:", err.message);
    }

    if (batchDelay > 0) await delay(batchDelay);
  }
}

async function processDistributionData(client, ipAddress, distributionData, isExisted) {
  if (!distributionData) {
    if (isDelete) {
    //  console.warn(`No distribution data found for IP ${ipAddress}. Skipping save.`);
      const response = await client.readHoldingRegisters(register3000Address, 1);
      const registerValue = response.response._body.values[0];
      await clearDeleteBit(client, registerValue, register3000Address, 2);
    }
    isDelete = false;
    return;
  }

  isDelete = true;
  modbusClients[ipAddress].hasMultipleSOs = false;

  const hasMultipleSOs =
    distributionData.OrderIDWithSOs && distributionData.OrderIDWithSOs.length >= 1;

  const clientData = (modbusClients[ipAddress] ??= {});

  if (!hasMultipleSOs) {
    console.warn(`No multiple Sales Orders detected for IP ${ipAddress}. Data will not be processed.`);
    return;
  }

  // -----------------------------
  // Multiple SOs handling
  // -----------------------------
  if (Array.isArray(distributionData.OrderIDWithSOs) && distributionData.OrderIDWithSOs.length > 0) {
    if (clientData.indexMultipleSOs == null) {
      clientData.indexMultipleSOs = 0;
    }
    clientData.hasMultipleSOs = true;
    clientData.SOs = distributionData.OrderIDWithSOs;

    const soGroup = clientData.SOs[clientData.indexMultipleSOs];
    const totalSOs = clientData.SOs.length;

    await client.writeSingleRegister(
      1020,
      clientData.indexMultipleSOs + 1 > totalSOs ? totalSOs : clientData.indexMultipleSOs + 1
    );
    await client.writeSingleRegister(1021, totalSOs);

    if (!soGroup || !Array.isArray(soGroup.Data) || soGroup.Data.length === 0) {
      console.warn(`❌ Invalid SO group or empty Data array for IP: ${ipAddress}`);
      return;
    }

    let orderID = soGroup.Data[0].OrderID;
    clientData.operatorID = soGroup.Data[0].OperatorID;
    distributionData.Leather = soGroup.Data[0].IsLeather ? 2 : 1;

    if (isExisted) {
      const orderIDData = await client.readHoldingRegisters(1000, 1);
      if (!orderIDData?.response?._body?.values?.[0]) {
        return;
      }
      orderID = orderIDData.response._body.values[0];
    }

    try {
      await client.writeSingleRegister(1000, orderID);
      await writeOperatorID(client, ipAddress, clientData.operatorID);
    } catch (err) {
      console.error(`❌ Failed to write OperatorID for IP ${ipAddress}:`, err.message);
    }
  } else {
    console.warn(`⚠️ distributionData.OrderIDWithSOs is empty or invalid for IP: ${ipAddress}`);
    return;
  }

  // -----------------------------
  // Write Order Info
  // -----------------------------
  
  const soGroup = clientData.SOs[clientData.indexMultipleSOs];
  if (soGroup && Array.isArray(soGroup.Data) && soGroup.Data.length > 0) {
    const orderInfo = [
      soGroup.Data[0]?.Model ?? "",
      soGroup.Data[0]?.ART ?? ""
    ];

    const orderInfoAddresses = [
      { start: 0, maxRegisters: 25 },  // Model
      { start: 25, maxRegisters: 10 }  // ART
    ];

    for (let i = 0; i < orderInfo.length; i++) {
      let registerData = stringTo16BitArrayLittleEndian(orderInfo[i] || "");
      const { start, maxRegisters } = orderInfoAddresses[i];

      // Ensure we do not exceed maxRegisters
      if (registerData.length > maxRegisters * 2) {
        registerData = registerData.slice(0, maxRegisters * 2);
      }

      for (let j = 0; j < registerData.length; j++) {
        try {
          await client.writeSingleRegister(start + j, registerData[j]);
        } catch (error) {
          console.error(
            `Error writing order info register ${start + j} for IP ${ipAddress}:`,
            error.message
          );
        }
      }
    }
  } else {
    console.warn(`⚠️ Cannot write Order Info: Invalid soGroup.Data for IP ${ipAddress}`);
  }

  // -----------------------------
  // Consolidate SizeData
  // -----------------------------
  if (
    distributionData?.SizeData &&
    clientData.SOs?.length > 0 &&
    Array.isArray(clientData.SOs[clientData.indexMultipleSOs]?.Data)
  ) {
    const sizeData = clientData.SOs[clientData.indexMultipleSOs].Data;

    const sizeTotals = sizeData.reduce((acc, item) => {
      const key = `${item.SizeID}-${item.PartName}-${item.PartID}`;
      if (!acc.has(key)) {
        acc.set(key, {
          SizeID: item.SizeID,
          Size: item.Size,
          SizeQty: 0,
          InventoryQty: item.InventoryQty,
          PartName: item.PartName,
          PartID: item.PartID
        });
      }
      acc.get(key).SizeQty += item.SizeQty;
      return acc;
    }, new Map());

    distributionData.SizeData = Array.from(sizeTotals.values());

    // -----------------------------
    // Write SOs (batched)
    // -----------------------------
    const registerSOAddress = [
      35, 40, 117, 122, 127, 132, 137, 142, 147, 152,
      157, 162, 167, 172, 177, 182, 187, 192, 197, 202
    ];
    const uniqueSOs = [
      ...new Set(
        (clientData.SOs?.[clientData.indexMultipleSOs]?.Data ?? []).map(so => so.SO)
      )
    ];
    const soWrites = [];
    uniqueSOs.forEach((so, i) => {
      const values = stringTo16BitArrayLittleEndian(so);
      values.forEach((val, j) => {
        soWrites.push({ addr: registerSOAddress[i] + j, val });
      });
    });
    await batchWrite(client, soWrites, 5, 30).catch(err => {
      console.error("🚨 batchWrite crashed:", err);
    });

    // -----------------------------
    // Part Names
    // -----------------------------
    const rawParts = clientData?.SOs[clientData.indexMultipleSOs]?.Data || [];

    // Compute total UnitUsage per Part
    const usageMap = rawParts.reduce((acc, p) => {
      const key = `${p.PartName}|${p.PartID}`;
      acc[key] = (acc[key] || 0) + (p.UnitUsage || 0);
      return acc;
    }, {});

    // Build your existing uniquePartSOsMap (but add DisplayText)
    const uniquePartSOsMap = [
      ...new Set(rawParts.map(p => `${p.PartName}|${p.PartID}`))
    ].map(item => {
      const [PartName, PartID] = item.split('|');
      const TotalUnitUsage = usageMap[item] || 0;

      return {
        PartName,
        PartID: parseInt(PartID),
        TotalUnitUsage,
        DisplayText: `${PartName} - ${TotalUnitUsage}`
      };
    });


    // -----------------------------------------------------
    // REMAINDER OF YOUR LOGIC (with DisplayText applied)
    // -----------------------------------------------------
    if (uniquePartSOsMap.length > 0) {
      if (modbusClients[ipAddress].indexMultiplePartNames == null) {
        modbusClients[ipAddress].indexMultiplePartNames = 0;
      }

    // Write registers 1022 + 1023
    await client.writeSingleRegister(
      1022,
      modbusClients[ipAddress].indexMultiplePartNames + 1 > uniquePartSOsMap.length
        ? uniquePartSOsMap.length
        : modbusClients[ipAddress].indexMultiplePartNames + 1
    );
    await delay(50);
    await client.writeSingleRegister(1023, uniquePartSOsMap.length);

    // Display part name (now PartName + Usage)
    const partDisplayStartRegister = distributionData.Leather === 2 ? 115 : 270;

    if (distributionData.Leather === 2) {
      await client.writeSingleRegister(partDisplayStartRegister, uniquePartSOsMap.length);
    } else {
      const selectedPart =
        uniquePartSOsMap[modbusClients[ipAddress].indexMultiplePartNames];

      if (!selectedPart || !selectedPart.PartName) {
        console.warn("⚠️ selectedPart not found or missing PartName for IP:", ipAddress);
        return;
      }

      distributionData.SizeData = distributionData.SizeData.filter(
        item => item.PartName === selectedPart.PartName &&
                item.PartID === selectedPart.PartID
      );

      // 👇 display "PartName (Usage)" instead of only PartName
      const registerData = stringTo16BitArrayASCII(selectedPart.DisplayText).slice(0, 20);

      const writes = registerData.map((val, j) => ({
        addr: partDisplayStartRegister + j,
        val
      }));

      await batchWrite(client, writes, 10, 20).catch(err => {
        console.error("🚨 batchWrite crashed:", err);
      });
    }

    // Multiple part names list (starting at 320)
    const partWrites = [];

    uniquePartSOsMap.slice(0, 20).forEach((part, i) => {
      const startRegister = 320 + i * 20;

      // 👇 Use DisplayText here too
      const registerData = stringTo16BitArrayASCII(part.DisplayText).slice(0, 20);

      registerData.forEach((val, j) => {
        partWrites.push({ addr: startRegister + j, val });
      });
    });

    await batchWrite(client, partWrites, 10, 30).catch(err =>
      console.error("🚨 batchWrite crashed:", err)
    );


    // Part IDs
    if (distributionData.Leather === 2) {
      const partIDWrites = uniquePartSOsMap.map((part, i) => ({
        addr: 91 + i,
        val: part.PartID
      }));

      await batchWrite(client, partIDWrites, 10, 50).catch(err =>
        console.error("🚨 batchWrite crashed:", err)
      );
    } else {
        const index = modbusClients[ipAddress]?.indexMultiplePartNames;
        const singlePart = uniquePartSOsMap[index];

        if (singlePart) {
          await client.writeSingleRegister(300, singlePart.PartID);
        }
      }
    }
  }
  // -----------------------------
  // Save DistributionData
  // -----------------------------
  await saveDistributionDataToModbus(client, ipAddress, distributionData);
}
function removeDiacritics(str) {
  return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

function stringTo16BitArrayASCII(str) {
  const clean = removeDiacritics(str);  // remove mark "Cổ giày" -> "Co giay"
  const bytes = new TextEncoder().encode(clean);
  const result = [];

  for (let i = 0; i < bytes.length; i += 2) {
    const low = bytes[i];
    const high = i + 1 < bytes.length ? bytes[i + 1] : 0;
    result.push((high << 8) | low);
  }

  return result;
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
        await processDistributionData(client, ipAddress, distributionData, true);

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
      modbusClients[ipAddress].sizeDataInfo = {};
      modbusClients[ipAddress].sizeDataInfo.sizeID = [];
    }
    // if (modbusClients[ipAddress].lockPartNameAdvance) {
    //   console.warn(`Action ignored: still processing for ${ipAddress}`);
    //   return;
    // }

    // if (modbusClients[ipAddress].lockPartNameAdvance) {
    //   console.warn(`Action ignored: still processing for ${ipAddress}`);
    //   return;
    // }

    // modbusClients[ipAddress].lockPartNameAdvance = true;
    if (isBitOn(binaryValue3000, previousSOIndex)) {
      await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
      let previous = Math.max(0, modbusClients[ipAddress].indexMultipleSOs - 1);

      modbusClients[ipAddress].indexMultipleSOs = previous;
      //  console.log(`Previous value ${previous} to register 3000 ${ipAddress}`);

      //await client.writeSingleRegister(1020, previous);
      storeDistributionData[ipAddress] = {};
      modbusClients[ipAddress].previousSizeData = [];
      modbusClients[ipAddress].previousData = {};
      modbusClients[ipAddress].sizeDataInfo = {};
      modbusClients[ipAddress].sizeDataInfo.sizeID = [];
    }

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
       // let uniquePartNameSOs = [];
        let seenPartIDs = new Set();
        let uniquePartNameSOs = [];
        
        if (currentSO && Array.isArray(currentSO.Data)) {
          for (const item of currentSO.Data) {
            if (!seenPartIDs.has(item.PartID)) {
              seenPartIDs.add(item.PartID);   // mark as seen
              uniquePartNameSOs.push(item);                 // add to diff if PartID is new
            }
          }
        
          // console.log("Unique PartIDs:", [...seenPartIDs]);
          // console.log("New unique items added to diff:", diff);
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

      // Kiểm tra SO hiện tại
      let currentSOComplete = false;

      //   console.log(`Nhat: ${clientData.indexMultipleSOs}`)
      const currentSO = clientData.SOs[clientData.indexMultipleSOs];
      if (currentSO) {
        currentSOComplete = await updateCompletionStatus(currentSO);
      }
      async function updateCompletionStatus(currentSO) {
        if (!currentSO || !Array.isArray(currentSO.Data)) return false;
      
        const data = currentSO.Data;
      
        for (const item of data) {
          const hasValidQty =
            typeof item.ActualSizeQty === 'number' &&
            typeof item.SizeQty === 'number';
      
          if (!hasValidQty) continue;
      
          // === CASE 1: Mark Complete when ActualSizeQty >= SizeQty ===
          if (item.Status !== 'Stop' && item.ActualSizeQty >= item.SizeQty) {
            item.Status = 'Complete';
      
            const distributionIDFromSize = await getDistributionIDFromSizeID(
              ipAddress,
              item.OrderID,
              item.IsLeather ? 1 : 0,
              item.SizeID,
              item.PartID
            );
      
            if (distributionIDFromSize?.DistributionID?.length > 0) {
              const { DistributionID } = distributionIDFromSize.DistributionID[0];
              setDistributionIsComplete(DistributionID, "Complete");
            }
          }
      
          // === CASE 2: If condition is NOT met anymore, change Complete → Pending ===
          if (item.Status === 'Complete' && item.ActualSizeQty < item.SizeQty) {
            item.Status = 'Pending';
            const distributionIDFromSize = await  getDistributionIDFromSizeIDNoneStatus(
              ipAddress,
              item.OrderID,
              item.IsLeather ? 1 : 0,
              item.SizeID,
              item.PartID
            );
      
            if (distributionIDFromSize?.DistributionID?.length > 0) {
              const { DistributionID } = distributionIDFromSize.DistributionID[0];
              setDistributionIsComplete(DistributionID, "Pending");
            }
          }
        }
      
        // Finally, check if all items are Complete or Stop
        return data.every(item =>
          item.Status === 'Complete' || item.Status === 'Stop'
        );
      }
      
      function removeCurrentSO(clientData) {
        if (
          !Array.isArray(clientData.SOs) ||
          clientData.SOs.length === 0 ||
          clientData.indexMultipleSOs < 0 ||
          clientData.indexMultipleSOs >= clientData.SOs.length
        ) {
          return;
        }

        // Remove the current SO
        clientData.SOs.splice(clientData.indexMultipleSOs, 1);

        // Adjust the index safely
        if (clientData.SOs.length === 0) {
          clientData.indexMultipleSOs = -1; // No SOs left
        } else if (clientData.indexMultipleSOs >= clientData.SOs.length) {
          clientData.indexMultipleSOs = clientData.SOs.length - 1; // Point to the last valid SO
        } 
      }

      // Nếu tất cả SOs complete hoặc distributionData null
      if (currentSOComplete) {
        removeCurrentSO(clientData);
        resetClientData(ipAddress, clientData);
        await delay(3000);
        await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
        return; // Nếu clear thì return luôn
      }
      const distributionData = await getDistributionDataFromDb(ipAddress);
      // Check if distribution data has more SOs than client memory
      let isPendingSize = false;
      if (distributionData?.OrderIDWithSOs?.length > clientData.SOs.length || distributionData?.OrderIDWithSOs?.length < clientData.SOs.length) {
        isPendingSize = true;
      }

      // Nếu isPendingSize = true => Clear và return ngay
      if (isPendingSize) {
        resetClientData(ipAddress, clientData);
        await delay(3000);
        await clearDeleteBit(client, registerValue, register3000Address, deleteIndex);
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
    } catch (error) {
      console.error(`Error processing part name change for ${ipAddress}:`, error);
    }
    // finally {
    //   modbusClients[ipAddress].lockPartNameAdvance = false;
    // }

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
  // ✅ Create a unique list by both PartName and PartID

  const uniquePartNameSOs = [
    ...new Map(
      currentSO.Data.map(item => [`${item.PartName}_${item.PartID}`, item])
    ).values()
  ];

  if (uniquePartNameSOs.length === 0) return;

  // ✅ Pick the currently selected part (by index)
  const selectedItem = uniquePartNameSOs[modbusClients[ipAddress].indexMultiplePartNames];
  if (!selectedItem) return;

  // ✅ Find all entries in the same group (same PartName & PartID)
  const distribution = currentSO.Data.filter(
    item => item.PartName === selectedItem.PartName && item.PartID === selectedItem.PartID
  );

  // console.log(`Selected PartName: ${selectedItem.PartName}, PartID: ${selectedItem.PartID}`);
  // console.log(`Distribution:`, distribution);

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
            console.warn(`⚠️ Skipping invalid sizeID at group ${g} index ${index} ${ipAddress}`);
            return;
          }

          const matched = summedValues.find(item => parseInt(item.sizeID) === sizeID);
          if (!matched) return;

          let leatherSizeIDs = [45, 47, 49];
          let isLeather = leatherSizeIDs.includes(sizeAddressID);

          // Determine spacing rule
          if (isLeather) {
            baseActual = 223;
          }
          let sizeSpace = 16;
          const actualAddress = baseActual + sizeSpace * (index);
          //const existing = modbusClients[ipAddress].previousSizeData.find(data => data.sizeID === sizeID);

          // const isSame =
          //   existing &&
          //   existing.actualCut === newData.actualCut &&
          //   existing.actualPieces === newData.actualPieces &&
          //   existing.actualQtys === newData.actualQtys;

          // if (!isSame) {



          async function safeWrite(client, address, value) {
            if (value !== 0) {

              await client.writeSingleRegister(address, value);


            }
          }

          await safeWrite(client, actualAddress, matched.totalCuts);
          await safeWrite(client, actualAddress + 4, matched.totalPieces);
          await safeWrite(client, actualAddress + 8, matched.totalQtys);

          // console.log(
          //   `✅ Written values for sizeID=${sizeID}, addrID=${sizeAddressID}, cuts=${matched.totalCuts}, pieces=${matched.totalPieces}, qtys=${matched.totalQtys}, baseAddr=${actualAddress}, index=${index}, IP=${ipAddress}`
          // );

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

  } catch (error) {
    const errorMsg = `❌ Error writting OperatorID to IP ${ipAddress}: ${error.message}`;
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
  try {
    if (modbusClients[ipAddress].isConnected == false) return;
    const rawLeather = await safeRead(1001, client);
    const isLeather = rawLeather === 2 ? 1 : 0;

    if (modbusClients[ipAddress].hasMultipleSOs) {
      processSizeID(ipAddress);
    }

    // Read OrderID from register 1000
    const orderIDData = await client.readHoldingRegisters(1000, 1);
    if (!orderIDData?.response?._body?.values?.[0]) {
      return;
    }
    let OrderID = orderIDData.response._body.values[0];

    const baseAddress = isLeather ? 966 : 886;
    let baseActual = isLeather ? 922 : 794;
    let baseSizeQtyAddress = isLeather ? 918 : 790;
    let collectPartAndOrderID;
    let operatorID;
    let completeSizeCount = 0;
    let SizeID = 0;

    if (!modbusClients[ipAddress].previousData) {
      modbusClients[ipAddress].previousData = {};
    }

    const sizeInfo = modbusClients[ipAddress].sizeDataInfo;
    if (Object.keys(sizeInfo).length == 0) return;

    const sizeCompleteID = modbusClients[ipAddress].sizeCompleteID || [];

    // Đọc chọn size từ thanh ghi
    const responseChooseSize = await client.readHoldingRegisters(modbusClients[ipAddress].chooseSizeAddress, 1);
    const registerChooseSizeValue = responseChooseSize.response._body.values[0];
    let binaryChooseSizeValue = registerChooseSizeValue.toString(2).padStart(16, '0');
    const index = findSetBitIndex(binaryChooseSizeValue);
    if (index === -1) return;

    if (Object.keys(sizeInfo).length == 0) return;
    const sizeAddressID = sizeInfo.sizeID[index];
    let sizeQtyAddress;
    let actualAddress;

    try {
      if (sizeAddressID === undefined) return;
      const sizeIDValue = parseInt(sizeAddressID);

      // leather rules
      const leatherSizeIDs = [45, 47, 49];
      const isLeatherSize = leatherSizeIDs.includes(sizeIDValue);

      if (isLeatherSize) {
        const customIndex = getLeatherIndex(sizeIDValue);
        baseSizeQtyAddress = 219;
        baseActual = 223;
        sizeQtyAddress = baseSizeQtyAddress + 16 * customIndex;
        actualAddress = baseActual + 16 * customIndex;
      } else {
        sizeQtyAddress = baseSizeQtyAddress + 16 * index;
        actualAddress = baseActual + 16 * index;
      }

      // Đọc nhiều thanh ghi song song với safeRead
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
      SizeID = sizeID;

      const isComplete = actualCut === sizeQty;
      if (isComplete && !sizeCompleteID.includes(SizeID)) {
        sizeCompleteID.push(SizeID);
      }

      completeSizeCount++;

      // ----- Multiple SO logic -----
      let partID;
      let checkPendingSize = [];
      if (modbusClients[ipAddress].hasMultipleSOs != null) {
        if (isLeather) {
          checkPendingSize =
            modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
              ?.filter(item => item.SizeID === SizeID && item.Status === 'Pending') || [];

          const pendingItem = checkPendingSize.reduce((max, item) => {
            if (item.Status === 'Pending') {
              return (!max || item.SizeQty > max.SizeQty) ? item : max;
            }
            return max;
          }, null);
          if (pendingItem) {
            SizeID = pendingItem.SizeID;
            partID = pendingItem.PartID;
            OrderID = pendingItem.OrderID;
            checkPendingSize = [pendingItem];
          } else return;

          if (checkPendingSize.length === 0) return;
          collectPartAndOrderID = (
            checkPendingSize
              ?.map(item => ({
                PartID: item.PartID,
                OrderID: item.OrderID,
                SizeID: item.SizeID,
                OperatorID: item.OperatorID,
                SizeQty: item.SizeQty
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
        } else {
          partID = await safeRead(300, client);
          checkPendingSize = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
            ?.filter(item => item.SizeID === SizeID && item.PartID === partID) || [];

          if (checkPendingSize.length > 1) {
            const pendingItem = checkPendingSize.reduce((max, item) => {
              if (item.Status === 'Pending') {
                return (!max || item.SizeQty > max.SizeQty) ? item : max;
              }
              return max;
            }, null);
            if (pendingItem) {
              SizeID = pendingItem.SizeID;
              partID = pendingItem.PartID;
              OrderID = pendingItem.OrderID;
              checkPendingSize = [pendingItem];
            } else return;
          } else if (checkPendingSize.length === 1) {
            if (checkPendingSize[0].Status !== 'Pending') return;
          } else return;
        }

        if (!checkPendingSize.length) return;

        modbusClients[ipAddress].storedActualCut = actualCut ?? 0;
        modbusClients[ipAddress].storedActualSizeQty = actualSizeQty ?? 0;
        modbusClients[ipAddress].storedActualPieces = actualPieces ?? 0;

        const firstPending = checkPendingSize[0];
        if (!firstPending) return;

        SizeID = firstPending.SizeID;
        OrderID = firstPending.OrderID;
        partID = firstPending.PartID;
        operatorID = firstPending.OperatorID;

        let totalCompletedSize;
        if (isLeather) {
          const data = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data ?? [];
          const completed = data.filter(item => item.Status === "Complete" && item.SizeID === SizeID);
          const uniqueCompleted = [];
          const seen = new Set();
          for (const item of completed) {
            const key = `${item.SizeID}-${item.SizeQty}`;
            if (!seen.has(key)) {
              seen.add(key);
              uniqueCompleted.push(item);
            }
          }
          totalCompletedSize =
            uniqueCompleted.reduce(
              (acc, item) => {
                acc.totalSizeQty += item.SizeQty ?? 0;
                acc.totalActualCut += item.ActualCut ?? 0;
                return acc;
              },
              { totalSizeQty: 0, totalActualCut: 0 }
            ) ?? { totalSizeQty: 0, totalActualCut: 0 };
        } else {
          totalCompletedSize = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs]?.Data
            ?.filter(item => item.Status === 'Complete' && item.SizeID === SizeID && item.PartID === partID)
            ?.reduce(
              (acc, item) => {
                acc.totalSizeQty += item.SizeQty ?? 0;
                acc.totalActualCut += item.ActualCut ?? 0;
                return acc;
              },
              { totalSizeQty: 0, totalActualCut: 0 }
            ) ?? { totalSizeQty: 0, totalActualCut: 0 };
        }

        if ((firstPending.InventoryQty + actualSizeQty) >= firstPending.SizeQty && firstPending.InventoryQty !== 0) {
          const totalQty = firstPending.InventoryQty + actualSizeQty;
          modbusClients[ipAddress].storedActualSizeQty = totalQty;
        }

        const completedQty = Number(totalCompletedSize.totalSizeQty) || 0;
        const availableQty = modbusClients[ipAddress].storedActualSizeQty - completedQty;
        if (availableQty < 0) return;

        if (availableQty >= firstPending.SizeQty) {
          if (isLeather) {
            const so = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs];
            if (so?.Data?.length) {
              for (const item of collectPartAndOrderID) {
                const matches = so.Data.filter(
                  x => x.SizeID === SizeID &&
                    x.OrderID === item.OrderID &&
                    x.SizeQty === item.SizeQty &&
                    x.Status === 'Pending'
                );
                if (matches.length > 1) {
                  for (const match of matches) {
                    match.Status = "Complete";
                    match.ActualCut = modbusClients[ipAddress].storedActualCut - totalCompletedSize.totalActualCut;
                    match.ActualSizeQty = actualSizeQty > firstPending.SizeQty
                      ? firstPending.SizeQty
                      : modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
                  }
                  collectPartAndOrderID = matches.map(m => ({
                    PartID: m.PartID,
                    OrderID: m.OrderID,
                    SizeID: m.SizeID,
                    OperatorID: m.OperatorID,
                    SizeQty: m.SizeQty
                  }));
                } else {
                  const match = so.Data.find(
                    x => x.SizeID === SizeID &&
                      x.PartID === item.PartID &&
                      x.OrderID === item.OrderID &&
                      x.Status === 'Pending'
                  );
                  if (match) {
                    match.Status = 'Complete';
                    match.ActualCut = modbusClients[ipAddress].storedActualCut - totalCompletedSize.totalActualCut;
                    match.ActualSizeQty = actualSizeQty > firstPending.SizeQty
                      ? firstPending.SizeQty
                      : modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
                  }
                }
              }
            }
          } else {
            firstPending.Status = 'Complete';
            firstPending.ActualCut = modbusClients[ipAddress].storedActualCut - totalCompletedSize.totalActualCut;
            firstPending.ActualSizeQty = actualSizeQty > firstPending.SizeQty
              ? firstPending.SizeQty
              : modbusClients[ipAddress].storedActualSizeQty - totalCompletedSize.totalActualSizeQty;
          }

          actualSizeQty = await readActualSizeQty(sizeQty, actualSizeQty, firstPending, totalCompletedSize);

          if (isLeather) {
            actualPieces = firstPending.TotalPiecesPerPair * actualSizeQty + actualCut;
          } else {
            if (totalCompletedSize.totalSizeQty !== 0) {
              modbusClients[ipAddress].storedActualCut -= totalCompletedSize.totalActualCut;
              actualCut = modbusClients[ipAddress].storedActualCut;
            } else {
              actualCut -= totalCompletedSize.totalActualCut;
            }
            actualPieces = (firstPending.MaterialLayer * firstPending.CuttingDieQty) * actualCut;
          }
        } else {
          if (isLeather) {
            const so = modbusClients[ipAddress]?.SOs?.[modbusClients[ipAddress].indexMultipleSOs];
            for (const item of collectPartAndOrderID) {
              const matches = so.Data.filter(
                x => x.SizeID === SizeID &&
                  x.OrderID === item.OrderID &&
                  x.SizeQty === item.SizeQty &&
                  x.Status === 'Pending'
              );
              collectPartAndOrderID = matches.map(m => ({
                PartID: m.PartID,
                OrderID: m.OrderID,
                SizeID: m.SizeID,
                OperatorID: m.OperatorID,
                SizeQty: m.SizeQty
              }));
            }
            modbusClients[ipAddress].storedActualSizeQty -= totalCompletedSize.totalSizeQty;
            actualPieces = firstPending.TotalPiecesPerPair * modbusClients[ipAddress].storedActualSizeQty + actualCut;
            actualSizeQty = modbusClients[ipAddress].storedActualSizeQty;
          } else {
            modbusClients[ipAddress].storedActualCut -= totalCompletedSize.totalActualCut;
            modbusClients[ipAddress].storedActualSizeQty -= totalCompletedSize.totalSizeQty;
            actualCut = modbusClients[ipAddress].storedActualCut;
            actualSizeQty = modbusClients[ipAddress].storedActualSizeQty;
            actualPieces = (firstPending.MaterialLayer * firstPending.CuttingDieQty) * actualCut;
          }
        }

        firstPending.ActualCut = actualCut;
        firstPending.ActualPieces = actualPieces;
        firstPending.ActualSizeQty = actualSizeQty;
      }

      // ✅ Only process if data changed
      if (collectPartAndOrderID?.length > 0) {
        for (const item of collectPartAndOrderID) {
          const key = `${SizeID}-${item.PartID}-${item.OrderID}`;
          const newData = { actualCut, actualPieces, actualSizeQty };
          const prevData = modbusClients[ipAddress].previousData[key];
          const isDifferent = !prevData ||
            prevData.actualCut !== newData.actualCut ||
            prevData.actualPieces !== newData.actualPieces ||
            prevData.actualSizeQty !== newData.actualSizeQty;

          if (isDifferent) {
            processActualDataChange(
              ipAddress,
              SizeID,
              item.PartID,
              piecesPerPair,
              materialLayer,
              cuttingDieQty,
              actualCut,
              actualPieces,
              actualSizeQty,
              totalPieces,
              isLeather,
              item.OrderID,
              operatorID
            );
            modbusClients[ipAddress].previousData[key] = newData;
          }
        }
      } else {
        const key = `${SizeID}-${partID}-${OrderID}`;
        const newData = { actualCut, actualPieces, actualSizeQty };
        const prevData = modbusClients[ipAddress].previousData[key];
        const isDifferent = !prevData ||
          prevData.actualCut !== newData.actualCut ||
          prevData.actualPieces !== newData.actualPieces ||
          prevData.actualSizeQty !== newData.actualSizeQty;

        if (isDifferent) {
          processActualDataChange(
            ipAddress,
            SizeID,
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
          );
          modbusClients[ipAddress].previousData[key] = newData;
        }
      }

    } catch (error) {
      console.error(`Error processing sizeID ${sizeAddressID}:`, error.message);
    }

    modbusClients[ipAddress].isComplete = completeSizeCount;

  } catch (error) {
    console.error(`Error reading actual data: ${error.message}`);
    logToFile(errorLogPath, `Error reading actual data: ${error.message}`);
  }

  // Helper function
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
      CutQuantity: isLeather ? actualPieces : actualCut,
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
        OperatorID: OperatorID,
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
async function writeToModbusRegister(client, entry, isConnected, ipAddress, registerAddress = 8000) {
  // Nếu đã có vòng lặp ghi thì bỏ qua
  if (entry.writeInterval) return;

  let counter = 0;

  entry.writeInterval = setInterval(async () => {
    try {
      counter = (counter + 1) % 60000;
      await safeWriteRegister(client, entry, isConnected, ipAddress, registerAddress, counter);
    } catch (err) {
      const errMsg = `[${ipAddress}] ❌ Write error: ${err.message}`;
      console.error(errMsg);
      logToFile(errorLogPath, errMsg);

      if (modbusClients[ipAddress]) {
        modbusClients[ipAddress].isConnected = false;
        modbusClients[ipAddress].isDisconnected = true;
        console.warn(`[${ipAddress}] 🔌 Marked client as disconnected`);
      }

      // ❌ Dừng loop khi lỗi
      clearInterval(entry.writeInterval);
      entry.writeInterval = null;
    }
  }, 1000);
}

async function safeWriteRegister(client, entry, isConnected, ipAddress, register, value) {
  if (!entry || !isConnected) {
    console.warn(`[${ipAddress}] ❌ No client found, cannot write`);
    return;
  }
  try {
    // Lock để đảm bảo 1 thiết bị chỉ có 1 write chạy cùng lúc
    if (!entry.writeLock) entry.writeLock = Promise.resolve();

    entry.writeLock = entry.writeLock
      .then(async () => {
        if (typeof client.writeSingleRegister !== "function") {
          console.warn(`[${ipAddress}] ⚠️ writeSingleRegister is not available`);
          return;
        }

        try {
          await client.writeSingleRegister(register, value);
          // console.log(`✅ Wrote ${value} → ${register} @ ${ipAddress}`);
        } catch (err) {
          // console.error(`[${ipAddress}] ❌ Write failed: ${err.message}`);
          entry.isConnected = false;
          if (entry.socket) {
            try {
              entry.socket.end();
              entry.socket.destroy();
            } catch (_) { }
            delete entry.socket;
          }
          entry.isConnecting = false;

          // // Gọi reconnect
          // connectToDevice(ipAddress);
        }
      })
      .catch(err => {
        console.error(`[${ipAddress}] ❌ Unexpected write error: ${err.message}`);
        entry.isConnected = false;
      });
  } catch (err) {
    console.error(`[${ipAddress}] ❌ Fatal error in safeWriteRegister: ${err.message}`);
    entry.isConnected = false;
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

async function saveDistributionDataToModbus(client, ipAddress, data) {

  try {

    // Ensure modbusClients structure exists
    modbusClients[ipAddress] ||= {};
    modbusClients[ipAddress].sizeDataInfo ||= {};

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
  await delay(100);
  // Ensure sizeData is an array and limit its length based on isLeather
  if (!Array.isArray(sizeData)) return;

  const chunkSize = 6;
  let processedSizeData = [];

  if (Array.isArray(sizeData)) {
    // ✅ Deduplicate by Size (works even if Size is undefined)
    const uniqueSizeData = Array.from(
      new Map(sizeData.map(item => [item.Size || Symbol(), item])).values()
    );

    // ✅ Slice after deduplication
    processedSizeData = uniqueSizeData.slice(0, chunkSize);
  } else {
    console.warn('sizeData is not an array:', sizeData);
  }
  // // Khởi tạo sizeDataInfo
  // if (!modbusClients[ipAddress].sizeDataInfo) {
  //   modbusClients[ipAddress].sizeDataInfo = { sizeID: [] };
  // }
  // if (!Array.isArray(modbusClients[ipAddress].sizeDataInfo.sizeID)) {
  //   modbusClients[ipAddress].sizeDataInfo.sizeID = [];
  // }
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
      { SizeID: 45, Size: 207, SizeQty: 219, InventoryQty: 302 },
      { SizeID: 47, Size: 211, SizeQty: 235, InventoryQty: 306 },
      { SizeID: 49, Size: 215, SizeQty: 251, InventoryQty: 310 },
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

    //console.log(`Writing to Modbus: SizeID = ${item.SizeID}, Size = ${item.Size}, SizeQty = ${item.SizeQty}, InventoryQty = ${item.InventoryQty}, Address ${ipAddress}`);

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

      modbusClients[ipAddress] ??= {};
      modbusClients[ipAddress].sizeDataInfo ??= { sizeID: [] };
      modbusClients[ipAddress].sizeDataInfo.sizeID ??= [];

      if (!modbusClients[ipAddress].sizeDataInfo.sizeID.includes(registerSize[i].SizeID)) {
        modbusClients[ipAddress].sizeDataInfo.sizeID.push(registerSize[i].SizeID);
        // console.log(`[sizeDataInfo] sizeID ${registerSize[i].SizeID}`);
        // console.log(`Writing to register ${registerSize[i].SizeID}, value: ${item.SizeID}`);
      }

      for (let j = 0; j < registerSizeData.length; j++) {
        //   console.log(`Writing to register ${startSizeRegister + j}, value: ${registerSizeData[j]}`);
        await client.writeSingleRegister(startSizeRegister + j, registerSizeData[j]);
      }

      //console.log(`Writing to register ${registerSize[i].SizeQty}, value: ${item.SizeQty}, Address ${ipAddress}`);
      //console.log(`Writing to register ${registerSize[i].InventoryQty}, value: ${item.InventoryQty}`);

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

let currentIndex = 0; // lưu vị trí đang check dở
const BATCH_SIZE = 5; // số IP check mỗi lần gọi
async function setIpAddresses(ipAddresses) {
  try {
    if (ipAddresses.length === 0) return;

    // Lấy batch kế tiếp
    const batch = ipAddresses.slice(currentIndex, currentIndex + BATCH_SIZE);
    currentIndex += BATCH_SIZE;
    if (currentIndex >= ipAddresses.length) {
      currentIndex = 0; // reset khi hết danh sách
    }

    // Check batch song song (limit bằng BATCH_SIZE)
    await Promise.all(batch.map(async (ipAddress) => {
      let reachable = false;
      try {
        reachable = await isHostReachable(ipAddress);
      } catch (err) {
        const msg = `❌ Error checking reachability for ${ipAddress}: ${err.message}`;
        console.error(msg);
        logToFile(errorLogPath, msg);
        return;
      }
      if (!reachable) {
        // const msg = `🚫 ${ipAddress} is not reachable on port 502. Skipping.`;
        // logToFile(errorLogPath, msg);
        return;
      }
      try {
        await connectToDevice(ipAddress);
      } catch (err) {
        const msg = `❌ Error connecting to device at ${ipAddress}: ${err.message}`;
        console.error(msg);
        logToFile(errorLogPath, msg);
      }
    }));

  } catch (error) {
    const msg = `❌ Error in setIpAddresses: ${error.message}`;
    console.error(msg);
    logToFile(errorLogPath, msg);
  }
}

async function isHostReachable(ipAddress, port = 502) {
  return new Promise((resolve) => {
    const socket = new net.Socket();

    socket.setTimeout(1000); // 1-second timeout

    socket.on('connect', () => {
      socket.destroy();
      resolve(true);
    });

    socket.on('timeout', () => {
      socket.destroy();
      resolve(false);
    });

    socket.on('error', () => {
      socket.destroy();   // important cleanup
      resolve(false);
    });

    socket.connect(port, ipAddress);
  });
}

startMonitoring();
module.exports = {
  connectToDevice,
  startMonitoring,
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
