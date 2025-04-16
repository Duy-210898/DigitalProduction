using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucDistribution : XtraUserControl
    {
        private DbHelper dbHelper;
        private WebSocketClient _webSocketClient;
        private List<MaterialData> materialDataList;
        private HashSet<int> orderIDs = new HashSet<int>();
        private int operatorID = 0;
        private int deviceID = 0;
        private int userID = 0;
        private int cuttingDieQty = 0;
        private int piecesPerPair = 0;
        private int materialLayer = 0;
        private int totalPiecesPerPair = 0;
        private HashSet<int> sizeIDs = new HashSet<int>();
        private HashSet<int> partIDs = new HashSet<int>();
        private HashSet<int> partSizeOrderIDs = new HashSet<int>();
        private List<SizeData> sizeDataList;
        private List<List<ProductionSchedule>> productionSchedules = new List<List<ProductionSchedule>>();
        private DataTable table = new DataTable();
        private int countSO = 0;

        public ucDistribution()
        {
            dbHelper = new DbHelper();
            InitializeComponent();
            ApplyLocalization();
            lbl_operatorName.Visible = false;
            loadDeviceDistribution();
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        private DataTable ConvertSizeDataToDataTable(List<SizeData> sizeDataList)
        {
            DataTable table = new DataTable();

            table.Columns.Add("SizeID", typeof(int));
            table.Columns.Add("Size", typeof(string));
            table.Columns.Add("SizeQty", typeof(int));
            table.Columns.Add("UnitUsage", typeof(decimal));
            table.Columns.Add("TotalUsage", typeof(decimal));
            table.Columns.Add("SelectSize", typeof(bool));

            foreach (var data in sizeDataList)
            {
                table.Rows.Add(data.SizeID, data.Size, data.SizeQty, data.UnitUsage, data.TotalUsage, false);
            }

            return table;
        }

        private void HandleReceivedSchedule(List<ProductionSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
            {
                ShowErrorNotification("No schedule data received.");
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action(() => HandleReceivedSchedule(schedules)));
                return;
            }

            // collect orderIDs
            orderIDs.UnionWith(schedules.Select(s => s.OrderID));

            sizeDataList = new List<SizeData>();
            materialDataList = new List<MaterialData>();

            // Lọc và sắp xếp các kích thước duy nhất
            var uniqueSortedSizes = schedules
                .Where(s => !string.IsNullOrEmpty(s.Size))
                .Select(s => s.Size)
                .Distinct()
                .OrderBy(size => size)
                .ToList();

            foreach (var size in uniqueSortedSizes)
            {
                var sizeSchedule = schedules.FirstOrDefault(s => s.Size == size);
                sizeDataList.Add(new SizeData
                {
                    SizeID = sizeSchedule.SizeID,
                    Size = size,
                    SizeQty = sizeSchedule?.SizeQty ?? 0,
                    UnitUsage = sizeSchedule?.UnitUsage ?? 0
                });
            }
            // Lọc và sắp xếp các phần duy nhất từ schedule
            var uniqueParts = schedules
                .GroupBy(s => s.PartName)
                .Select(g => g.First())
                .OrderBy(part => part.PartCode)
                .ToList();
            // Chuyển đổi dữ liệu sang DataTable
            DataTable originalTable = ConvertSizeDataToDataTable(sizeDataList);
            //  DataTable transformedTable = TransformSizeData(originalTable);
            // Gán dữ liệu vào gridControl_Size


            // Duyệt qua các phần vật liệu và thêm vào materialDataList
            foreach (var schedule in uniqueParts)
            {
                materialDataList.Add(new MaterialData
                {
                    PartID = schedule.PartId,
                    PartCode = schedule.PartCode,
                    PartName = schedule.PartName,
                    VietnameseName = schedule.VietnameseName,
                    MaterialCode = schedule.MaterialCode,
                    MaterialName = schedule.MaterialName,
                    Unit = schedule.MaterialUnit
                });
            }

           // cbxPart.Properties.DataSource = materialDataList;
            // Cập nhật thông tin Header
            string factoryName = schedules.FirstOrDefault()?.Factory ?? string.Empty;
            switch (factoryName)
            {
                case "4001":
                    factoryName = "APACHE FOOTWEAR";
                    break;
                case "4011":
                    factoryName = "MEGA";
                    break;
                case "4021":
                    factoryName = "TERA";
                    break;
                default:
                    factoryName = "Unknown Factory";
                    break;
            }

            lblFactory.Text = $"Factory: {factoryName}";
            lblLastNo.Text = $"Last No: {schedules.FirstOrDefault()?.LastNo ?? string.Empty}";
            lblMasterWorkOrder.Text = $"Master Work Order: {string.Join(", ", schedules.Select(s => s.MasterWorkOrder).Distinct())}";
            lblSO.Text = $"SO: {schedules.FirstOrDefault()?.SO ?? string.Empty}";
            lblPO.Text = $"PO: {schedules.FirstOrDefault()?.PO ?? string.Empty}";
            lblModel.Text = $"Model: {schedules.FirstOrDefault()?.Model ?? string.Empty}";
            lblArt.Text = $"ART: {schedules.FirstOrDefault()?.ART ?? string.Empty}";

            // set defaultInfo
            DefaultInfo defaultInfo = dbHelper.getDefaultValueFromART(lblArt.Text.Split(':')[1]);
            if (defaultInfo != null)
            {
                numPiecesPerPair.Text = defaultInfo.PiecesPerPair.ToString();
                numCuttingDieQty.Text = defaultInfo.CuttingDieQty.ToString();
                numMaterialLayer.Text = defaultInfo.MaterialLayer.ToString();
                numericTotalPeicesPerPair.Text = defaultInfo.TotalPiecesPerPair.ToString();
            }

        }

        private void WebSocket_OnMessage(object data)
        {
            try
            {
                string jsonData = data as string;
                Console.WriteLine("Response from server: " + jsonData);

                if (jsonData != null)
                {
                    var scheduleResponse = JsonConvert.DeserializeObject<ScheduleResponse>(jsonData);

                    if (scheduleResponse != null)
                    {
                        switch (scheduleResponse.Action)
                        {
                            case "getSchedule":
                                HandleGetScheduleResponse(scheduleResponse);
                                break;
                            case "getOperatorDistribution":
                                if (scheduleResponse.Status == "success" && scheduleResponse.Employee != null)
                                {
                                    loadOperator(scheduleResponse.Employee[0]);
                                }
                                else {
                                    lbl_operatorName.ResetText();
                                    ShowMessage.ShowError(scheduleResponse.Message, LocalizationManager.GetString("Error"));
                                }
                                break;
                            case "saveDistributionData":
                                Console.WriteLine("Suscess");
                                break;

                            default:
                                ShowErrorNotification($"Unsupported action: {scheduleResponse.Action}");
                                break;
                        }
                    }
                    else
                    {
                        ShowErrorNotification("Invalid response format.");
                    }
                }
                else
                {
                    ShowErrorNotification("Received data is not a valid string.");
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                ShowErrorNotification($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Error processing message: {ex.Message}");
            }
        }

        private void HandleGetScheduleResponse(ScheduleResponse scheduleResponse)
        {
            if (scheduleResponse.Status == "success")
            {
                HandleReceivedSchedule(scheduleResponse.Schedule);
                DefaultInfo defaultInfo = dbHelper.getDefaultValueFromART(lblArt.Text.Split(':')[1]);
                if (defaultInfo != null)
                {
                    numPiecesPerPair.Text = defaultInfo.PiecesPerPair.ToString();
                    numCuttingDieQty.Text = defaultInfo.CuttingDieQty.ToString();
                    numMaterialLayer.Text = defaultInfo.MaterialLayer.ToString();
                    numericTotalPeicesPerPair.Text = defaultInfo.TotalPiecesPerPair.ToString();
                }
            }
            else
            {
                ShowErrorNotification(scheduleResponse.Message);
            }
        }

        private void ShowErrorNotification(string message)
        {
            ShowMessage.ShowError("Error" + message);
        }


        private async void cbxDevice_SelectedValueChanged(object sender, EventArgs e)
        {
            if (cbxDevice.SelectedIndex > 0)
            {
                string deviceID = cbxDevice.SelectedValue.ToString();
                string IpAddress = await dbHelper.GetIPAddressByDeviceIDAsync(int.Parse(deviceID));
                if (!string.IsNullOrEmpty(deviceID) || !string.IsNullOrEmpty(IpAddress))
                {
                    SendGetOpertaionAndScheduleRequestAsync(String.Empty, IpAddress, "getOperatorDistribution");
                }
            }
        }

        private void loadOperator(Employee employee)
        {
            if (employee != null)
            {
                lbl_operatorName.Visible = true;
                lbl_operatorID.Text = employee.OperatorID.ToString();
                lbl_operatorName.Text = string.Join("-", employee.OperatorName, employee.EmployeeID);
                lbl_operatorName.Font = new Font("Arial", 10, FontStyle.Bold);

            }
            else {
                lbl_operatorName.Visible = false;
            }

        }

        private async void SendGetOpertaionAndScheduleRequestAsync(string so, string IpAddress, string key)
        {
            var soInfo = new { app = Global.App, action = key, so, IpAddress};
            string jsonRequest = JsonConvert.SerializeObject(soInfo);
            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void loadDeviceDistribution()
        {
            List<Device> machines = dbHelper.getlistMachines();
            machines.Insert(0, new Device { DeviceID = 0, MachineName = "" });
            cbxDevice.DataSource = machines;
            cbxDevice.DisplayMember = "MachineName";
            cbxDevice.ValueMember = "DeviceID";
            cbxDevice.SelectedIndex = 0;
            cbxDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private List<DistributionData> getDistributionDataFromControls()
        {
            partSizeOrderIDs.Clear();
            List<DistributionData> results = new List<DistributionData>();
            userID = Global.CurrentUser != null ? Global.CurrentUser.UserID : 0;
            deviceID = int.Parse(cbxDevice.SelectedValue.ToString());
            operatorID = int.Parse(lbl_operatorID.Text);
            cuttingDieQty = int.Parse(numCuttingDieQty.Text);
            piecesPerPair = int.Parse(numPiecesPerPair.Text);
            materialLayer = int.Parse(numMaterialLayer.Text);
            totalPiecesPerPair = int.Parse(numericTotalPeicesPerPair.Text);
            int productId = dbHelper.getProductIdByArt(lblArt.Text.Split(':')[1]);
            bool isLeather = false;

            // Use a HashSet to avoid duplicate PartSizeOrderIDs
            HashSet<int> uniquePSOIDs = new HashSet<int>();
            // Clear previous results
            partSizeOrderIDs.Clear();

            if (rdLeather.Checked)
            {
                isLeather = true;
            }
            foreach (var group in productionSchedules)
            {
                foreach (var schedule in group)
                {
                    if (schedule == null) continue;

                    int partID = schedule.PartId;
                    int sizeID = schedule.SizeID;
                    int orderID = schedule.OrderID;
                    int inventory = schedule.InventoryQty;

                    int psoID = dbHelper.getPartSizeOrderId(partID, sizeID, orderID);

                    if (psoID != -1 && !uniquePSOIDs.Contains(psoID))
                    {
                        var distributionData = new DistributionData
                        {
                            DeviceID = deviceID,
                            OperatorID = operatorID,
                            UserID = userID,
                            ProductID = productId,
                            PartSizeOrderID = psoID,
                            CuttingDieQty = cuttingDieQty,
                            PiecesPerPair = piecesPerPair,
                            MaterialLayer = materialLayer,
                            InventoryQty = inventory,
                            TotalPiecesPerPair = totalPiecesPerPair,
                            IsLeather = isLeather,
                            CreatedAt = DateTime.Now,
                            IsDelete = false,
                            Status = "Pending",
                        };

                        results.Add(distributionData);
                        uniquePSOIDs.Add(psoID);
                    }
                }
            }


                //foreach (int orderID in orderIDs)
                //{
                //    if (partIDs.Count >= 20)
                //    {
                //        ShowMessage.ShowWarning("PartName no longer than 20", "Warning");
                //        break;
                //    }
                //    // 1 part has many sizes - care part
                //    if (rdRawMaterial.Checked)
                //    {
                //        // get partoderID
                //        foreach (int i in sizeIDs)
                //        {
                //            partSizeOrderIDs.Add(dbHelper.getPartSizeOrderId(partIDs.FirstOrDefault(), i, orderID));
                //        }
                //    }
                //    // 1-3 size has many parts - care size
                //    else
                //    {
                //        isLeather = true;
                //        foreach (int i in sizeIDs)
                //        {
                //            foreach (int partId in partIDs)
                //            {
                //                partSizeOrderIDs.Add(dbHelper.getPartSizeOrderId(partId, i, orderID));
                //            }
                //        }
                //    }
                //}

                //if (partSizeOrderIDs.Count != 0)
                //{
                //    // save on DB
                //    foreach (int i in partSizeOrderIDs)
                //    {
                //        var distributionData = new DistributionData
                //        {
                //            DeviceID = deviceID,
                //            OperatorID = operatorID,
                //            UserID = userID,
                //            ProductID = productId,
                //            PartSizeOrderID = i,
                //            CuttingDieQty = cuttingDieQty,
                //            PiecesPerPair = piecesPerPair,
                //            MaterialLayer = materialLayer,
                //            InventoryQty = inventory,
                //            TotalPiecesPerPair = totalPiecesPerPair,
                //            IsLeather = isLeather,
                //            CreatedAt = DateTime.Now,
                //            IsDelete = false,
                //            Status = "Pending",
                //        };
                //        results.Add(distributionData);
                //    }
                //}
            return results;
        }

        private void ApplyLocalization()
        {
            var controls = new Dictionary<Control, string>
            {
                { lblFactory, "Factory" },
                { lblLastNo, "LastNo" },
                { lblMasterWorkOrder, "MasterWorkOrder" },
                { lblSO, "SO" },
                { lblPO, "PO" },
                { lblModel, "Model" },
                { lblArt, "ART" },
                { btnSaveInventory, "Save" },
                { btnSend, "Send" },
                { lblInventoryQty, "InventoryQty" },
                {lblPeicesPerPair, "PeicesPerPair" },
                {lblCuttingDie, "CuttingDieQty" },
                {lblMaterialLayer, "MaterialLayer" },
                {lblTotalPeicesPerPair, "TotalPeicesPerPair" },
                {lblSelectMachine, "SelectMachine" },
                {lbl_Name, "NameOperator" },
            };

            foreach (var control in controls)
            {
                control.Key.Text = LocalizationManager.GetString(control.Value) + (control.Key is Label ? ":" : "");
            }

            cbxDevice.Text = LocalizationManager.GetString("SelectDevice");
            rdLeather.Text = LocalizationManager.GetString("leatherMaterial");
            rdRawMaterial.Text = LocalizationManager.GetString("rawMaterial");
        }

        private async void SendDistributionDataToServer()
        {
            try
            {
                List<DistributionData> distributionData = getDistributionDataFromControls();
                if (distributionData != null && distributionData.Count > 0)
                {
                    var request = new
                    {
                        app = Global.App,
                        action = "saveDistributionData",
                        data = distributionData
                    };

                    string jsonRequestWrapper = JsonConvert.SerializeObject(request);

                    // Gửi yêu cầu và nhận phản hồi từ máy chủ
                    string jsonResponse = await _webSocketClient.SendAsync(jsonRequestWrapper);

                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        var response = JsonConvert.DeserializeObject<Response>(jsonResponse);

                        if (response != null && response.Status == "success")
                        {
                            ShowMessage.ShowInfo(response.Message, "Sucess");
                        }
                        else
                        {
                            ShowMessage.ShowError(response.Message, "Error");
                        }
                    }
                    else
                    {
                        MessageBox.Show("No response received from the server.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while sending data: " + ex.Message);
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
            bool isAnyChecked = panelMaterialType.Controls.OfType<System.Windows.Forms.RadioButton>().Any(r => r.Checked);

            if (!isAnyChecked)
            {
                ShowMessage.ShowWarning("Please select an material type option before proceeding.", "Warning");
                return;
            }
            if (cbxDevice.Text == "") {
                ShowMessage.ShowWarning("Please select an material type option before proceeding.", "Warning");
                return;
            }
            SendDistributionDataToServer();
        }

        private void rdLeather_CheckedChanged(object sender, EventArgs e)
        {
            setControlVisibility(false, numCuttingDieQty, numMaterialLayer, numPiecesPerPair, lblCuttingDie, lblMaterialLayer, lblPeicesPerPair);
            setControlVisibility(true, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
        }

        private void rdRawMaterial_CheckedChanged(object sender, EventArgs e)
        {
            setControlVisibility(false, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
            setControlVisibility(true, numCuttingDieQty, numMaterialLayer, numPiecesPerPair, lblCuttingDie, lblMaterialLayer, lblPeicesPerPair);
        }
        private void setControlVisibility(bool isVisible, params Control[] controls)
        {
            foreach (var control in controls)
            {
                control.Visible = isVisible;
            }
        }

        private void updateUIDataGridOverView(List<ProductionSchedule> filteredSchedules)
        {
            // Ensure DataGridView settings
            dataGrid_overviewDistribution.Dock = DockStyle.Fill;
            dataGrid_overviewDistribution.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_overviewDistribution.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGrid_overviewDistribution.AllowUserToResizeRows = false;
            dataGrid_overviewDistribution.AllowUserToResizeColumns = false;

            // Set alternating row colors for better readability
            //dataGrid_overviewDistribution.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;

            // Center align content in cells
            dataGrid_overviewDistribution.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGrid_overviewDistribution.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            // Allow selection of entire rows
            dataGrid_overviewDistribution.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid_overviewDistribution.MultiSelect = false;
            dataGrid_overviewDistribution.ReadOnly = true;  // Prevent editing if needed

            if (table.Columns.Count == 0)
            {
                // Convert filteredSchedules to a DataTable for binding
                table.Columns.Add("SO", typeof(string));
                table.Columns.Add("PartName", typeof(string));
                table.Columns.Add("Size", typeof(string));
                table.Columns.Add("SizeQty", typeof(int)); // SizeQuantity column
                table.Columns.Add("InventoryQty", typeof(int));
            }

            // Dictionary to merge PartName & Size while summing SizeQuantity
            Dictionary<string, (List<string> SOs, int TotalSizeQuantity)> mergedData = new Dictionary<string, (List<string>, int)>();


            // Add an empty row as separator
            countSO++;
            string sttSO = $"SO {countSO}";
            table.Rows.Add(sttSO, "", "", DBNull.Value, DBNull.Value);
            foreach (var schedule in filteredSchedules)
            {
                string partName = schedule.PartName;
                string size = GetSizeName(schedule.SizeID);
                string key = $"{partName}|{size}";

                if (!mergedData.ContainsKey(key))
                {
                    mergedData[key] = (new List<string>(), 0);
                }

                // Add SO to the list if not already present
                if (!mergedData[key].SOs.Contains(schedule.SO))
                {
                    mergedData[key].SOs.Add(schedule.SO);
                }

                // Sum the SizeQuantity
                mergedData[key] = (mergedData[key].SOs, mergedData[key].TotalSizeQuantity + schedule.SizeQty);
            }

            // Convert merged data into DataTable
            foreach (var entry in mergedData)
            {
                string[] splitKey = entry.Key.Split('|');
                string mergedSO = string.Join(", ", entry.Value.SOs); // Merge SOs into a single string
                int inventory = 0;
                table.Rows.Add(mergedSO, splitKey[0], splitKey[1], entry.Value.TotalSizeQuantity, inventory);
            }

            // Bind data to DataGridView
            dataGrid_overviewDistribution.DataSource = table;

            // Translate headers if needed
            TranslateDataGridOverviewDistributionHeaders();
            // Automatically select the first row if available
            if (dataGrid_overviewDistribution.Rows.Count > 0)
            {
                dataGrid_overviewDistribution.ClearSelection();
            }
            // Color specific rows after binding
            foreach (DataGridViewRow row in dataGrid_overviewDistribution.Rows)
            {
                var soCell = row.Cells["SO"];
                if (soCell.Value != null && soCell.Value.ToString().StartsWith("SO"))
                {
                    row.DefaultCellStyle.BackColor = Color.LightBlue; // or any color you prefer
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
            }
        }

        private void TranslateDataGridOverviewDistributionHeaders()
        {
            foreach (DataGridViewColumn col in dataGrid_overviewDistribution.Columns)
            {
                // Use the appropriate property depending on how you identify headers
                col.HeaderText = LocalizationManager.GetString(col.Name) ?? col.Name; // Assuming you're using column Name as key
            }
        }

        private string GetSizeName(int sizeId)
        {
            var sizeData = sizeDataList.FirstOrDefault(s => s.SizeID == sizeId);
            return sizeData != null ? sizeData.Size : string.Empty;
        }
        public void ReceiveFilteredData(List<ProductionSchedule> filteredSchedules)
        {
            HandleReceivedSchedule(filteredSchedules);
            Console.WriteLine($"Received {filteredSchedules.Count} schedules.");
            // Collect unique SizeIDs and PartIDs
            sizeIDs = new HashSet<int>(filteredSchedules.Select(s => s.SizeID));
            partIDs = new HashSet<int>(filteredSchedules.Select(s => s.PartId));
            if (partIDs.Count > 20) {
               ShowMessage.ShowWarning("PartName no longer than 20", "Warning");
            }
            else
            {
                productionSchedules.Add(filteredSchedules);
                updateUIDataGridOverView(filteredSchedules);
            }

        }

        private void btnSaveInventory_Click(object sender, EventArgs e)
        {
            int inventory = int.TryParse(txtInventory.Text, out int result) ? result : 0;
            if (dataGrid_overviewDistribution.SelectedRows.Count > 0) {
                DataGridViewRow selectedRow = dataGrid_overviewDistribution.SelectedRows[0];

                string partName = selectedRow.Cells["PartName"].Value?.ToString();
                string size = selectedRow.Cells["Size"].Value?.ToString();
                int sizeQty = Convert.ToInt32(selectedRow.Cells["SizeQty"].Value ?? 0);

                if (inventory >= sizeQty) {
                    ShowMessage.ShowInfo("Number of Inventory not larger or equal to SizeQty");
                    return;
                }
                selectedRow.Cells["InventoryQty"].Value = inventory;
                string normalizedSize = size?.Trim();

                // Update InventoryQty in productionSchedules
                foreach (var group in productionSchedules)
                {
                    foreach (var schedule in group)
                    {
                        string scheduleSize = GetSizeName(schedule.SizeID);

                        if (string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(scheduleSize, normalizedSize, StringComparison.OrdinalIgnoreCase))
                        {
                            schedule.InventoryQty = inventory;
                        }
                    }
                }
                // Log the results
                Console.WriteLine($"Selected PartName: {partName}");
                Console.WriteLine($"Selected Size: {normalizedSize}");
            }
        }
    }
}