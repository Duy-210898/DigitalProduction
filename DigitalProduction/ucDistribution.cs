using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
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
        private HashSet<int> sizeIDs = new HashSet<int>();
        private HashSet<int> partIDs = new HashSet<int>();
        private HashSet<int> partSizeOrderIDs = new HashSet<int>();
        private List<SizeData> sizeDataList = new List<SizeData>();
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
            txtInventory.KeyPress += TxtInventory_KeyPress;
            txt_targetInDay.KeyPress += TxtInventory_KeyPress;
            rdRawMaterial.CheckedChanged += MaterialFilterChanged;
            setControlVisibility(false, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
        }
        private void TxtInventory_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Chỉ cho phép ký tự số và phím điều hướng như backspace
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Ngăn ký tự không hợp lệ
            }
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

            lblFactory.Text = $"{LocalizationManager.GetString("Factory")}: {factoryName}";
            lblLastNo.Text = $"{LocalizationManager.GetString("LastNo")}: {schedules.FirstOrDefault()?.LastNo ?? string.Empty}";
            lblMasterWorkOrder.Text = $"{LocalizationManager.GetString("MasterWorkOrder")}: {string.Join(", ", schedules.Select(s => s.MasterWorkOrder).Distinct())}";
            lblSO.Text = $"{LocalizationManager.GetString("SO")}: {schedules.FirstOrDefault()?.SO ?? string.Empty}";
            lblPO.Text = $"{LocalizationManager.GetString("PO")}: {schedules.FirstOrDefault()?.PO ?? string.Empty}";
            lblModel.Text = $"{LocalizationManager.GetString("Model")}: {schedules.FirstOrDefault()?.Model ?? string.Empty}";
            lblArt.Text = $"{LocalizationManager.GetString("ART")}: {schedules.FirstOrDefault()?.ART ?? string.Empty}";

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
            List<Device> machines = DbHelper.getlistMachines();
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
            //cuttingDieQty = int.Parse(numCuttingDieQty.Text);
            //piecesPerPair = int.Parse(numPiecesPerPair.Text);
            //materialLayer = int.Parse(numMaterialLayer.Text);
            //totalPiecesPerPair = int.Parse(numericTotalPeicesPerPair.Text);

          //  int productId = dbHelper.getProductIdByArt(lblArt.Text.Split(':')[1]);
            bool isLeather = false;

            // Use a HashSet to avoid duplicate PartSizeOrderIDs
            HashSet<int> uniquePSOIDs = new HashSet<int>();
            // Clear previous results
            partSizeOrderIDs.Clear();

            if (productionSchedules.Count != 0)
            {
                if (rdLeather.Checked)
                {
                    isLeather = true;
                }
                foreach (var (group, index) in productionSchedules.Select((g, i) => (g, i)))
                {
                    foreach (var schedule in group)
                    {
                        if (schedule == null) continue;

                        int partID = schedule.PartId;
                        int sizeID = schedule.SizeID;
                        int orderID = schedule.OrderID;
                        string model = schedule.Model;
                        int inventory = schedule.InventoryQty;
                        int cuttingDieQty = schedule.CuttingDieQty;
                        int piecesPerPair = schedule.PeicesPerPair;
                        int materialLayer = schedule.MaterialLayer;
                        int TotalPiecesPerPair = schedule.TotalPiecesPerPair;
                        if (rdRawMaterial.Checked)
                        {
                            if (cuttingDieQty == 0 || piecesPerPair == 0 || materialLayer == 0)
                            {
                                ShowMessage.ShowInfo(LocalizationManager.GetString("RequiredDataCutting"));
                                return null;
                            }
                        }
                        if (rdLeather.Checked) {
                            if (TotalPiecesPerPair == 0)
                            {
                                ShowMessage.ShowInfo(LocalizationManager.GetString("RequiredDataCutting"));
                                return null;
                            }
                        }
                        int productId = dbHelper.getProductIdByArt(schedule.ART);

                        int psoID = dbHelper.getPartSizeOrderId(partID, sizeID, orderID);

                        if (psoID != -1 && !uniquePSOIDs.Contains(psoID))
                        {
                            var distributionData = new DistributionData
                            {
                                DeviceID = deviceID,
                                OperatorID = operatorID,
                                UserID = userID,
                                ProductID = productId,
                                PartID = partID,
                                Model = model,
                                PartSizeOrderID = psoID,
                                CuttingDieQty = cuttingDieQty,
                                PiecesPerPair = piecesPerPair,
                                MaterialLayer = materialLayer,
                                InventoryQty = inventory,
                                TotalPiecesPerPair = TotalPiecesPerPair,
                                IsLeather = isLeather,
                                CreatedAt = DateTime.Now.AddSeconds(index),
                                IsDelete = false,
                                Status = "Pending",
                            };

                            results.Add(distributionData);
                            uniquePSOIDs.Add(psoID);
                        }
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
                { btnDeleteData, "Refresh" },
                { lblInventoryQty, "InventoryQty" },
                {lblPeicesPerPair, "PeicesPerPair" },
                {lblTargetInDay, "TargetInDay" },
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
                            ResetSendDistribution();
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
                        ConnectionManager.Instance.IsReconnecting = true;
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
            bool isAnyChecked = panelMaterialType.Controls.OfType<RadioButton>().Any(r => r.Checked);

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

        private void UpdateUIGridViewOverview(List<List<ProductionSchedule>> filteredSchedulesGroups)
        {
            gridControlOverview.Dock = DockStyle.Fill;
            gridViewOverview.OptionsView.ColumnAutoWidth = true;
            gridViewOverview.OptionsView.RowAutoHeight = true;
            gridViewOverview.OptionsView.ShowGroupPanel = false;
            gridViewOverview.OptionsBehavior.Editable = true;
            gridViewOverview.OptionsSelection.MultiSelect = false;
            gridViewOverview.Appearance.Row.TextOptions.HAlignment = HorzAlignment.Center;
            gridViewOverview.Appearance.HeaderPanel.TextOptions.HAlignment = HorzAlignment.Center;
            gridViewOverview.Appearance.Row.Font = new Font("Segoe UI", 10);

            //    DataTable table = new DataTable();
          
            table = new DataTable();
            table.Columns.Add("GroupSO", typeof(string));
            table.Columns.Add("SO", typeof(string));
            table.Columns.Add("PartName", typeof(string));
            table.Columns.Add("Size", typeof(string));
            table.Columns.Add("SizeQty", typeof(int));
            table.Columns.Add("PeicesPerPair", typeof(int));
            table.Columns.Add("CuttingDieQty", typeof(int));
            table.Columns.Add("MaterialLayer", typeof(int));
            table.Columns.Add("TotalPiecesPerPair", typeof(int));
            table.Columns.Add("InventoryQty", typeof(int));


            gridControlOverview.DataSource = null;
            foreach (var (scheduleGroup, index) in filteredSchedulesGroups.Select((g, i) => (g, i)))
               // foreach (var scheduleGroup in filteredSchedulesGroups)
            {
                if (scheduleGroup.Count == 0)
                    continue;

                countSO++;
                string groupSO = $"{countSO}";

                var mergedData = new Dictionary<string, (List<string> SOs, int TotalSizeQty)>();

                foreach (var schedule in scheduleGroup)
                {
                    string partName = schedule.PartName;
                    string size = GetSizeName(schedule.SizeID);
                    string key = $"{partName}|{size}";

                    schedule.GroupSO = groupSO;
                    if (rdRawMaterial.Checked)
                    {
                        schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                        schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                        schedule.MaterialLayer = (int)numMaterialLayer.Value;
                    }
                    if (rdLeather.Checked)
                    {
                        schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                    }
                    if (size.Equals(""))
                    {
                        continue;
                    }

                    if (!mergedData.ContainsKey(key))
                        mergedData[key] = (new List<string>(), 0);

                    if (!mergedData[key].SOs.Contains(schedule.SO))
                        mergedData[key].SOs.Add(schedule.SO);

                    mergedData[key] = (mergedData[key].SOs, mergedData[key].TotalSizeQty + schedule.SizeQty);
                }

                foreach (var entry in mergedData)
                {
                    string[] splitKey = entry.Key.Split('|');
                    string mergedSO = string.Join(", ", entry.Value.SOs);
                    table.Rows.Add(groupSO, mergedSO, splitKey[0], splitKey[1], entry.Value.TotalSizeQty, 0, 0, 0, 0, 0);
                }
            }

            gridControlOverview.DataSource = table;

            TranslateGridControlOverviewHeaders();
            gridViewOverview.ClearSelection();

            // Group by GroupSO column
            //gridViewOverview.GroupCount = 1;
            gridViewOverview.Columns["GroupSO"].GroupIndex = 0;
            gridViewOverview.OptionsSelection.MultiSelect = true;
            gridViewOverview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;

            // gridViewOverview.Columns["GroupSO"].GroupIndex = 0;
            gridViewOverview.ExpandAllGroups();
        }

        private void TranslateGridControlOverviewHeaders()
        {
            foreach (GridColumn col in gridViewOverview.Columns)
            {
                string localizedHeader = LocalizationManager.GetString(col.FieldName);
                if (!string.IsNullOrEmpty(localizedHeader))
                {
                    col.Caption = localizedHeader;
                }
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
                countSO = 0;
                foreach (var group in filteredSchedules)
                {
                    group.GroupSO = countSO.ToString();
                }
                bool exists = false;

                foreach (var ps in productionSchedules)
                {
                    if (AreSchedulesEqual(ps, filteredSchedules))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    for (int i = filteredSchedules.Count - 1; i >= 0; i--) // Loop backwards to avoid skipping items after removal
                    {
                        var productionSchedule = filteredSchedules[i];

                        // Skip the check if productionSchedules is empty
                        if (productionSchedules.Count == 0)
                            continue;

                        bool existed = false;

                        // Check if productionSchedule matches any list in productionSchedules
                        foreach (var scheduleGroup in productionSchedules)
                        {
                            if (scheduleGroup.Contains(productionSchedule))
                            {
                                existed = true;
                                break; // Found a match, no need to check further
                            }
                        }

                        if (existed)
                        {
                            filteredSchedules.RemoveAt(i); // Safely remove the item from filteredSchedules
                        }
                    }

                    if (filteredSchedules.Count != 0)
                    {
                        productionSchedules.Add(filteredSchedules);
                    }
                }
            }

            UpdateUIGridViewOverview(productionSchedules);

        }

        bool AreSchedulesEqual(List<ProductionSchedule> list1, List<ProductionSchedule> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // Create dictionaries to count identical items
            var dict1 = list1.GroupBy(s => $"{s.OrderID}|{s.PartName}|{s.SizeID}|{s.SizeQty}")
                             .ToDictionary(g => g.Key, g => g.Count());

            var dict2 = list2.GroupBy(s => $"{s.OrderID}|{s.PartName}|{s.SizeID}|{s.SizeQty}")
                             .ToDictionary(g => g.Key, g => g.Count());

            // Compare dictionaries
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.ContainsKey(kvp.Key) || dict2[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;

        }
        private void btnSaveInventory_Click(object sender, EventArgs e)
        {
            // Refresh the grid to ensure it reflects the latest changes
            gridViewOverview.RefreshData();

            // Parse the input inventory value
            int inventoryInput = string.IsNullOrEmpty(txtInventory.Text) ? 0 : int.Parse(txtInventory.Text);

            int[] selectedRows = gridViewOverview.GetSelectedRows();

            if (selectedRows.Length > 0)
            {
                int inventory = int.TryParse(txtInventory.Text, out int result) ? result : 0;

                int selectedRowHandle = selectedRows[0];
                string partName = gridViewOverview.GetRowCellValue(selectedRowHandle, "PartName")?.ToString();
                string size = gridViewOverview.GetRowCellValue(selectedRowHandle, "Size")?.ToString();

                object sizeQtyObj = gridViewOverview.GetRowCellValue(selectedRowHandle, "SizeQty");
                int sizeQty = sizeQtyObj != DBNull.Value ? Convert.ToInt32(sizeQtyObj) : 0;

                if (sizeQty != 0 && inventory > sizeQty)
                {
                    ShowMessage.ShowInfo("Number of Inventory must be less than SizeQty");
                    return;
                }

                gridViewOverview.SetRowCellValue(selectedRowHandle, "InventoryQty", inventory);

                string normalizedSize = size?.Trim();
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

            // Iterate through the selected rows
            foreach (int row in selectedRows)
            {
                if (gridViewOverview.IsGroupRow(row))
                {
                    // This is a group row, you can handle the logic if row
                    string groupSOHeader = gridViewOverview.GetGroupRowValue(row)?.ToString();


                    // Iterate through all the rows in the grid
                    for (int i = 0; i < gridViewOverview.RowCount; i++)
                    {
                        int rowHandle = gridViewOverview.GetVisibleRowHandle(i);

                        // Skip non-data rows
                        if (!gridViewOverview.IsDataRow(rowHandle)) continue;

                        // Get GroupSO value for the current row
                        string groupSO = gridViewOverview.GetRowCellValue(rowHandle, "GroupSO")?.ToString();
                        if (!string.Equals(groupSO, groupSOHeader, StringComparison.OrdinalIgnoreCase))
                            continue; // Only process rows that belong to Group SO 1

                        // Get other values from the current row
                        string partName = gridViewOverview.GetRowCellValue(rowHandle, "PartName")?.ToString();
                        string size = gridViewOverview.GetRowCellValue(rowHandle, "Size")?.ToString()?.Trim();

                        //   int inventory = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "InventoryQty") ?? 0);
                        int sizeQty = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "SizeQty") ?? 0);
                        int piecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "PeicesPerPair") ?? 0);
                        int cuttingDieQty = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "CuttingDieQty") ?? 0);
                        int materialLayer = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "MaterialLayer") ?? 0);
                        int totalPiecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "TotalPiecesPerPair") ?? 0);


                        // Loop through production schedules and find the matching schedule
                        foreach (var group in productionSchedules)
                        {
                            foreach (var schedule in group)
                            {
                                string scheduleSize = GetSizeName(schedule.SizeID);

                                // Check if the schedule matches the current row's partName and size
                                if (string.Equals(schedule.GroupSO, groupSOHeader, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(scheduleSize, size, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Check if values are different before updating
                                    if ((int)numPiecesPerPair.Value != piecesPerPair ||
                                        (int)numCuttingDieQty.Value != cuttingDieQty ||
                                        (int)numMaterialLayer.Value != materialLayer ||
                                        (int)numericTotalPeicesPerPair.Value != totalPiecesPerPair)
                                    {
                                        // Update the schedule with new values
                                        //  schedule.InventoryQty = inventoryInput;
                                        if (rdRawMaterial.Checked)
                                        {
                                            schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                                            schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                                            schedule.MaterialLayer = (int)numMaterialLayer.Value;

                                            // Now, directly update the grid's cells for this row
                                            //     gridViewOverview.SetRowCellValue(rowHandle, "InventoryQty", schedule.InventoryQty);
                                            gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", schedule.PeicesPerPair);
                                            gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", schedule.CuttingDieQty);
                                            gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", schedule.MaterialLayer);
                                        }
                                        if (rdLeather.Checked) {
                                            schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                                            // Now, directly update the grid's cells for this row
                                            gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", schedule.TotalPiecesPerPair);
                                        }
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"[Updated] Group SO 1: {partName} - {size} =>  Pairs: {numPiecesPerPair.Value.ToString()}, Die: {numCuttingDieQty.Value.ToString()}, Layer: {numMaterialLayer.Value.ToString()} TotalPiecesPerPair: {numericTotalPeicesPerPair.Value.ToString()}");
                    }

                    // Refresh the grid to reflect all changes
                    gridViewOverview.RefreshData();

                    // Show success message
                    ShowMessage.ShowInfo($"Group SO {groupSOHeader} data updated successfully.");
                }
            }
        }

        private void btnDeleteData_Click(object sender, EventArgs e)
        {
            ResetSendDistribution();
        }


        private void MaterialFilterChanged(object sender, EventArgs e)
        {
            ApplyMaterialFilterAndMonth(); // Combine with your existing month filter
        }

        private void ApplyMaterialFilterAndMonth()
        {
            gridViewOverview.RefreshData();

            int inventory = int.TryParse(txtInventory.Text, out int result) ? result : 0;

            for (int i = 0; i < gridViewOverview.RowCount; i++)
            {
                int rowHandle = gridViewOverview.GetVisibleRowHandle(i);
                if (!gridViewOverview.IsDataRow(rowHandle)) continue;

                string partName = gridViewOverview.GetRowCellValue(rowHandle, "PartName")?.ToString();
                string size = gridViewOverview.GetRowCellValue(rowHandle, "Size")?.ToString()?.Trim();
                string groupSO = gridViewOverview.GetRowCellValue(rowHandle, "GroupSO")?.ToString();

                int sizeQty = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "SizeQty") ?? 0);
                int piecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "PeicesPerPair") ?? 0);
                int cuttingDieQty = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "CuttingDieQty") ?? 0);
                int materialLayer = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "MaterialLayer") ?? 0);
                int totalPiecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "TotalPiecesPerPair") ?? 0);

                if (inventory >= sizeQty)
                {
                    ShowMessage.ShowInfo("Inventory must be less than SizeQty.");
                    continue;
                }

                // Update InventoryQty in GridView
                gridViewOverview.SetRowCellValue(rowHandle, "InventoryQty", inventory);

                foreach (var group in productionSchedules)
                {
                    foreach (var schedule in group)
                    {
                        string scheduleSize = GetSizeName(schedule.SizeID);
                        if (string.Equals(schedule.GroupSO, groupSO, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(scheduleSize, size, StringComparison.OrdinalIgnoreCase))
                        {
                            schedule.InventoryQty = inventory;

                            if (rdRawMaterial.Checked)
                            {
                                schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                                schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                                schedule.MaterialLayer = (int)numMaterialLayer.Value;

                                gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", schedule.PeicesPerPair);
                                gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", schedule.CuttingDieQty);
                                gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", schedule.MaterialLayer);

                                gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", 0);
                            }

                            if (rdLeather.Checked)
                            {
                                gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", 0);
                                gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", 0);
                                gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", 0);

                                schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                                gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", schedule.TotalPiecesPerPair);
                            }
                        }
                    }
                }
            }

            gridViewOverview.RefreshData();
            ShowMessage.ShowInfo("All rows updated successfully.");
        }
        private void ResetSendDistribution()
        {
            loadDeviceDistribution();
            lbl_operatorID.ResetText();
            lbl_operatorName.ResetText();
            countSO = 0;
            table.Clear();
            productionSchedules.Clear();
            gridControlOverview.DataSource = null;
            sizeDataList = new List<SizeData>();
        }
    }
}