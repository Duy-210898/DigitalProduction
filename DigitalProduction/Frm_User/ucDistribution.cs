using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
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
        private HashSet<int> sizeIDs = new HashSet<int>();
        private HashSet<int> partIDs = new HashSet<int>();
        private HashSet<int> partSizeOrderIDs = new HashSet<int>();
        private List<SizeData> sizeDataList = new List<SizeData>();
        private List<List<ProductionSchedule>> productionSchedules = new List<List<ProductionSchedule>>();
        private DataTable table = new DataTable();
        private DataTable subDistributionDataSource = new DataTable();
        private bool isDeviceData, isLeather = false;
        private int countSO = 0;

        public ucDistribution()
        {
            dbHelper = new DbHelper();
            InitializeComponent();
            ApplyLocalization();

            loadOperatorDistribution();
            loadDeviceDistribution();
            txtInventory.KeyPress += TxtInventory_KeyPress;
         //   rdRawMaterial.CheckedChanged += MaterialFilterChanged;
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

        private async void gridLookUpDevice_EditValueChanged(object sender, EventArgs e)
        {
            if (gridLookUpDevice.EditValue == null)
                return;

            // Get selected DeviceID
            if (int.TryParse(gridLookUpDevice.EditValue.ToString(), out int deviceID) && deviceID > 0)
            {
                string ipAddress = await dbHelper.GetIPAddressByDeviceIDAsync(deviceID);

                if (!string.IsNullOrEmpty(ipAddress))
                {
                     SendGetOpertaionAndScheduleRequestAsync(string.Empty, ipAddress, "getOperatorDistribution");
                }
                else
                {
                    MessageBox.Show("⚠️ No IP address found for the selected device.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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

            gridLookUpDevice.Properties.DataSource = machines;
            gridLookUpDevice.Properties.DisplayMember = "MachineName";
            gridLookUpDevice.Properties.ValueMember = "DeviceID";

            // Optional: Disable typing if you want DropDownList style
            gridLookUpDevice.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            // Hide all columns except "MachineName"
            gridLookUpDevice.Properties.View.Columns.Clear();
            gridLookUpDevice.Properties.PopulateViewColumns();
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in gridLookUpDevice.Properties.View.Columns)
            {
                column.Visible = column.FieldName == "MachineName";
            }
            // Enable autocomplete & search
            gridLookUpDevice.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            gridLookUpDevice.Properties.AutoComplete = true;

            // Enable incremental search
            gridLookUpDevice.Properties.ImmediatePopup = true;
            gridLookUpDevice.Properties.PopupFilterMode = DevExpress.XtraEditors.PopupFilterMode.Contains;
            gridLookUpDevice.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            gridLookUpDevice.Properties.NullText = "";

            // Filter mode
            gridLookUpDevice.Properties.View.OptionsView.ShowAutoFilterRow = true;
            gridLookUpDevice.Properties.View.ActiveFilterEnabled = true;
        }
        private void loadOperatorDistribution()
        {
            List<Employee> employees= DbHelper.getOperatorsByDepartment();

            gridLookUpOperator.Properties.DataSource = employees;
            gridLookUpOperator.Properties.DisplayMember = "OperatorName";
            gridLookUpOperator.Properties.ValueMember = "EmployeeID";

            // Optional: Disable typing if you want DropDownList style
            gridLookUpOperator.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            // Hide all columns except "MachineName"
            gridLookUpOperator.Properties.View.Columns.Clear();
            gridLookUpOperator.Properties.PopulateViewColumns();
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in gridLookUpOperator.Properties.View.Columns)
            {
                column.Visible = column.FieldName == "OperatorName";
            }
            // Enable autocomplete & search
            gridLookUpOperator.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            gridLookUpOperator.Properties.AutoComplete = true;

            // Enable incremental search
            gridLookUpOperator.Properties.ImmediatePopup = true;
            gridLookUpOperator.Properties.PopupFilterMode = DevExpress.XtraEditors.PopupFilterMode.Contains;
            gridLookUpOperator.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            gridLookUpOperator.Properties.NullText = "";

            // Filter mode
            gridLookUpOperator.Properties.View.OptionsView.ShowAutoFilterRow = true;
            gridLookUpOperator.Properties.View.ActiveFilterEnabled = true;
        }

        private DistributionPayload getDistributionDataFromControls(bool isDeviceData)
        {
            partSizeOrderIDs.Clear();
            var payload = new DistributionPayload();
            var uniquePSOIDs = new HashSet<int>();

            int userID = Global.CurrentUser?.UserID ?? 0;
            int departmentID = Global.CurrentUser?.DepartmentID ?? 0;
            int deviceID = 0;
            int operatorID = 0;
            if (!isDeviceData)
            {
                deviceID = int.TryParse(gridLookUpDevice.EditValue?.ToString(), out int devID) ? devID : 0;
                operatorID = int.TryParse(gridLookUpOperator.EditValue?.ToString(), out int operID) ? operID : 0;
            }

            if (productionSchedules.Count == 0)
                return payload;

            foreach (var (group, index) in productionSchedules.Select((g, i) => (g, i)))
            {
                foreach (var schedule in group)
                {
                    if (schedule == null) continue;

                    int partID = schedule.PartId;
                    int sizeID = schedule.SizeID;
                    int orderID = schedule.OrderID;
                    string model = schedule.Model ?? "Unknown";
                    int inventory = schedule.InventoryQty ?? 0;
                    int cuttingDieQty = schedule.CuttingDieQty ?? 0;
                    int piecesPerPair = schedule.PeicesPerPair ?? 0;
                    int materialLayer = schedule.MaterialLayer ?? 0;
                    int totalPiecesPerPair = schedule.TotalPiecesPerPair ?? 0;

                    int productId = dbHelper.getProductIdByArt(schedule.ART);
                    int? psoID = dbHelper.getPartSizeOrderId(partID, sizeID, orderID);
                    if (psoID == -1 || uniquePSOIDs.Contains(psoID.Value)) continue;
                    DateTime dt = DateTime.Now.AddSeconds(index);
                    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
                    var dist = new DistributionData
                    {
                        DeviceID = isDeviceData ? schedule.DeviceID : deviceID,
                        OperatorID = isDeviceData ? schedule.OperatorID : operatorID,
                        UserID = userID,
                        ProductID = productId,
                        PartID = partID,
                        Model = model,
                        PartSizeOrderID = psoID.Value,
                        CuttingDieQty = cuttingDieQty,
                        PiecesPerPair = piecesPerPair,
                        MaterialLayer = materialLayer,
                        InventoryQty = inventory,
                        TotalPiecesPerPair = totalPiecesPerPair,
                        IsLeather = isLeather,
                        CreatedAt = dt,
                        IsDelete = false,
                        Status = "Pending"
                    };

                    payload.Distributions.Add(dist);
                    uniquePSOIDs.Add(psoID.Value);

                    if (isDeviceData && schedule.AssignedOperators != null && schedule.AssignedOperators.Count > 0)
                    {
                        foreach (var operatorInfo in schedule.AssignedOperators)
                        {
                            payload.SubDistributions.Add(new OperatorInfo
                            {
                                DeviceID = operatorInfo.DeviceID,
                                OperatorID = operatorInfo.OperatorID,
                                InventoryQty = operatorInfo.InventoryQty,
                                SizeQty = operatorInfo.SizeQty
                            });
                        }
                    }
                }
            }

            return payload;
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
                {lblCuttingDie, "CuttingDieQty" },
                {lblMaterialLayer, "MaterialLayer" },
                {lblTotalPeicesPerPair, "TotalPeicesPerPair" },
                {lblSelectMachine, "SelectMachine" },
                {lbl_Name, "NameOperator" },
                {btnSync, "Sync" },
            };

            foreach (var control in controls)
            {
                control.Key.Text = LocalizationManager.GetString(control.Value) + (control.Key is Label ? ":" : "");
            }

            gridLookUpDevice.EditValue = LocalizationManager.GetString("SelectDevice");
        }

        private async void SendDistributionDataToServer()
        {
            try
            {
                DistributionPayload payload = getDistributionDataFromControls(isDeviceData);
                if (payload == null || payload.Distributions.Count == 0)
                    return;

                ResetSendDistribution();

                var request = new
                {
                    app = Global.App,
                    action = "saveDistributionData",
                    data = payload
                };
                string jsonRequestWrapper = JsonConvert.SerializeObject(request);

                string jsonResponse = await _webSocketClient.SendAsync(jsonRequestWrapper);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    var response = JsonConvert.DeserializeObject<Response>(jsonResponse);

                    if (response != null && response.Status == "success")
                    {
                        ResetSendDistribution();
                        ShowMessage.ShowInfo(response.Message, "Success");
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
            catch (Exception ex)
            {
                ConnectionManager.Instance.IsReconnecting = true;
                MessageBox.Show("An error occurred while sending data: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!isDeviceData)
            {
                if (gridLookUpDevice.EditValue.ToString() == "")
                {
                    ShowMessage.ShowWarning("Please select an device before proceeding.", "Warning");
                    return;
                }
                if (gridLookUpOperator.EditValue.ToString() == "")
                {
                    ShowMessage.ShowWarning("Please select an operator before proceeding.", "Warning");
                    return;
                }
            }
            SendDistributionDataToServer();
        }
        private void rdLeather_Checked()
        {
            setControlVisibility(false, numCuttingDieQty, numMaterialLayer, numPiecesPerPair, lblCuttingDie, lblMaterialLayer, lblPeicesPerPair);
            setControlVisibility(true, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
        }
        private void rdRawMaterial_Checked()
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
        private void UpdateUIGridViewOverview(List<List<ProductionSchedule>> filteredSchedulesGroups, bool isDeviceData)
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

            // ✅ Clear the child table before the parent table to prevent constraint errors
            subDistributionDataSource?.Clear();

            table?.Clear();

            table = new DataTable("DistributionData");
            table.Columns.Add("GroupSO", typeof(string));
            table.Columns.Add("SO", typeof(string));
            table.Columns.Add("PartName", typeof(string));
            table.Columns.Add("Size", typeof(string));
            table.Columns.Add("SizeQty", typeof(int));
            table.Columns.Add("PeicesPerPair", typeof(int));
            table.Columns.Add("CuttingDieQty", typeof(int));
            table.Columns.Add("MaterialLayer", typeof(int));
            table.Columns.Add("UnitUsage", typeof(string));
            table.Columns.Add("TotalPiecesPerPair", typeof(int));
            table.Columns.Add("InventoryQty", typeof(int));
            table.Columns.Add("UnitUsagePerPair", typeof(double));

            // create sub table 
            subDistributionDataSource = new DataTable("SubDistributions");
            subDistributionDataSource.Columns.Add("SubDistributionID", typeof(int));
            subDistributionDataSource.Columns.Add("SO", typeof(string)); // Foreign Key
            subDistributionDataSource.Columns.Add("PartName", typeof(string));
            subDistributionDataSource.Columns.Add("Size", typeof(string));
            subDistributionDataSource.Columns.Add("SizeQty", typeof(int));
            subDistributionDataSource.Columns.Add("InventoryQty", typeof(int));
            subDistributionDataSource.Columns.Add("MachineName", typeof(string));
            subDistributionDataSource.Columns.Add("DeviceID", typeof(int));
            subDistributionDataSource.Columns.Add("EmployeeID", typeof(int));
            subDistributionDataSource.Columns.Add("OperatorName", typeof(string));

            gridControlOverview.DataSource = null;
            foreach (var (scheduleGroup, index) in filteredSchedulesGroups.Select((g, i) => (g, i)))
            {
                if (scheduleGroup.Count == 0)
                    continue;

                countSO++;
                string groupSO = $"{countSO}";

                var mergedData = new Dictionary<string, (List<string> SOs, int TotalSizeQty, string UnitUsage)>();
                var totalSizePerSize = new Dictionary<string, int>();
                var totalUnitUsagePerSize = new Dictionary<string, float>();

                foreach (var schedule in scheduleGroup)
                {
                    string partName = schedule.PartName;
                    string size = GetSizeName(schedule.SizeID);
                    string key = $"{partName}|{size}";

                    schedule.GroupSO = groupSO;

                    if (isLeather)
                    {
                        schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                    }
                    else
                    {
                        schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                        schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                        schedule.MaterialLayer = (int)numMaterialLayer.Value;
                    }

                    if (string.IsNullOrWhiteSpace(size))
                        continue;

                    // ✅ For per-partName+size grouping
                    if (!mergedData.ContainsKey(key))
                    {
                        mergedData[key] = (new List<string>(), 0, schedule.UnitUsage.ToString());
                    }

                    if (!mergedData[key].SOs.Contains(schedule.SO))
                    {
                        mergedData[key].SOs.Add(schedule.SO);
                    }

                    var current = mergedData[key];
                    mergedData[key] = (current.SOs, current.TotalSizeQty + schedule.SizeQty, current.UnitUsage);

                    // ✅ Accumulate total per-size for UnitUsagePerPair
                    if (!totalSizePerSize.ContainsKey(size))
                    {
                        totalSizePerSize[size] = 0;
                        totalUnitUsagePerSize[size] = 0;
                    }

                    totalSizePerSize[size] += schedule.SizeQty;
                    totalUnitUsagePerSize[size] += schedule.UnitUsage;
                }

                // ✅ Add merged data to table
                // Step 1: Compute unit usage per pair per size ONCE
                var unitUsagePerPairPerSize = new Dictionary<string, float>();

                foreach (var sizeKey in totalUnitUsagePerSize.Keys)
                {
                    if (totalSizePerSize.TryGetValue(sizeKey, out int totalSizeQty))
                    {
                        float usagePerPair = totalUnitUsagePerSize[sizeKey];
                        unitUsagePerPairPerSize[sizeKey] = usagePerPair;
                    }
                }

                // Step 2: Build string to show in label (also ONCE)
                var sb = new StringBuilder();

                float totalUnitUsagePerPair = unitUsagePerPairPerSize.Values.Sum();
                sb.AppendLine($" {LocalizationManager.GetString("UnitUsagePerPair")}: {Math.Round(totalUnitUsagePerPair, 6).ToString("N3",new CultureInfo("de-DE"))}");

                // Step 3: Display in label
                lblUnitUsagePerPair.Text = sb.ToString();

                // Step 4: Loop through entries and build table
                foreach (var entry in mergedData)
                {
                    string[] splitKey = entry.Key.Split('|');
                    string partName = splitKey[0];
                    string entrySize = splitKey[1];
                    string mergedSO = string.Join(", ", entry.Value.SOs);
                    string unitUsage = entry.Value.UnitUsage;

                    // If total unit usage > 1, override unitUsage with rounded float
                    if (totalUnitUsagePerSize.TryGetValue(entrySize, out var totalUsage) && totalUsage > 1)
                    {
                        unitUsage = Math.Round(totalUsage, 3).ToString("N3", new CultureInfo("de-DE"));
                    }

                    // Calculate per-row unitUsagePerPair for this specific size
                    double unitUsagePerPair = 0;
                    if (totalSizePerSize.TryGetValue(entrySize, out int totalQty) && totalQty > 0)
                    {
                        unitUsagePerPair = totalUnitUsagePerSize[entrySize] / totalQty;
                    }

                    // Add row to table
                    table.Rows.Add(
                         groupSO,
                         mergedSO,
                         partName,
                         entrySize,
                         entry.Value.TotalSizeQty,
                         0, 0, 0,
                         unitUsage,
                         0,
                         0,
                         Math.Round(unitUsagePerPair, 6).ToString(CultureInfo.InvariantCulture)
                    );
                }
            }

                gridControlOverview.DataSource = table;

            TranslateGridControlOverviewHeaders(gridViewOverview);
            gridViewOverview.ClearSelection();

            // Group by GroupSO
            gridViewOverview.Columns["GroupSO"].GroupIndex = 0;
            gridViewOverview.OptionsSelection.MultiSelect = true;
            gridViewOverview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;
            gridViewOverview.ExpandAllGroups();

            // Device-specific UI additions
            if (isDeviceData)
            {
                // Add Action column if not present
                if (gridViewOverview.Columns["Action"] == null)
                {
                    var actionButtonEdit = new RepositoryItemButtonEdit
                    {
                        TextEditStyle = TextEditStyles.HideTextEditor
                    };

                    actionButtonEdit.Buttons.Clear();
                    actionButtonEdit.Buttons.Add(new EditorButton(ButtonPredefines.Glyph)
                    {
                        ImageOptions = { Image = Properties.Resources.icon_Add },
                        ToolTip = "Duplicate",
                        Tag = "Duplicate"
                    });

                    actionButtonEdit.ButtonClick += gridViewOverview_ActionButtonClick;

                    if (!gridControlOverview.RepositoryItems.Contains(actionButtonEdit))
                        gridControlOverview.RepositoryItems.Add(actionButtonEdit);

                    gridViewOverview.Columns.Add(new GridColumn
                    {
                        Caption = LocalizationManager.GetString("Action"),
                        Name = "colActionButtons",
                        FieldName = "Action",
                        ColumnEdit = actionButtonEdit,
                        Visible = true,
                        Width = 80
                    });

                    // Add cursor hover effect
                    gridViewOverview.MouseMove += (s, e) =>
                    {
                        var hitInfo = gridViewOverview.CalcHitInfo(e.Location);
                        gridControlOverview.Cursor = (hitInfo.InRowCell && hitInfo.Column?.FieldName == "Action")
                            ? Cursors.Hand
                            : Cursors.Default;
                    };
                }

                // Attach only once (you can wrap this if needed to avoid multiple binds)
                gridViewOverview.ShowingEditor += gridViewOverview_ShowingEditor;
                // Add CompositeKey column
                table.Columns.Add("CompositeKey", typeof(string));
                subDistributionDataSource.Columns.Add("CompositeKey", typeof(string));

                // Fill CompositeKey in parent
                foreach (DataRow row in table.Rows)
                {
                    row["CompositeKey"] = $"{row["SO"]}_{row["PartName"]}_{row["Size"]}";
                }

                // Fill CompositeKey in child
                foreach (DataRow row in subDistributionDataSource.Rows)
                {
                    row["CompositeKey"] = $"{row["SO"]}_{row["PartName"]}_{row["Size"]}";
                }

                // IMPORTANT: Add both tables to the same DataSet
                DataSet dataSet = new DataSet();
                dataSet.Tables.Add(table);
                dataSet.Tables.Add(subDistributionDataSource);

                // Now create the relation
                dataSet.Relations.Add(LocalizationManager.GetString("SubDistributions"),
                    table.Columns["CompositeKey"],
                    subDistributionDataSource.Columns["CompositeKey"]);



                gridControlOverview.DataSource = dataSet;
                gridControlOverview.DataMember = "DistributionData"; // parent table

                GridView detailView = new GridView(gridControlOverview);
                gridControlOverview.LevelTree.Nodes.Add(LocalizationManager.GetString("SubDistributions"), detailView);

                detailView.OptionsView.ShowGroupPanel = false;
                detailView.OptionsBehavior.Editable = true;

                detailView.Columns.AddVisible("Size", "Size");
                detailView.Columns.AddVisible("SizeQty", "SizeQty");
                detailView.Columns.AddVisible("PartName", "PartName");
                detailView.Columns.AddVisible("InventoryQty", "InventoryQty");

                List<Employee> employees = DbHelper.getOperatorsByDepartment();
                List<Device> machines = DbHelper.getlistMachines();
                // create loop up edit
                RepositoryItemGridLookUpEdit deviceLookup = new RepositoryItemGridLookUpEdit
                {
                    DataSource = machines,
                    DisplayMember = "MachineName",  // or "DeviceName"
                    ValueMember = "DeviceID",    // or "DeviceID"
                    NullText = "",
                    TextEditStyle = TextEditStyles.Standard, // Allows typing & autocomplete
                    AutoComplete = true,
                    ImmediatePopup = true,
                    PopupFilterMode = PopupFilterMode.Contains,
                    AllowNullInput = DefaultBoolean.True
                };

                // Optional: Only show "MachineName" column
                deviceLookup.PopulateViewColumns();
                foreach (GridColumn column in deviceLookup.View.Columns)
                {
                    column.Visible = column.FieldName == "MachineName";
                }

                // Show auto-filter row (search box)
                deviceLookup.View.OptionsView.ShowAutoFilterRow = true;
                deviceLookup.View.ActiveFilterEnabled = true;

                RepositoryItemGridLookUpEdit operatorLookup = new RepositoryItemGridLookUpEdit
                {
                    DataSource = employees,
                    DisplayMember = "OperatorName",
                    ValueMember = "EmployeeID",
                    NullText = "",
                    TextEditStyle = TextEditStyles.Standard,
                    AutoComplete = true,
                    ImmediatePopup = true,
                    PopupFilterMode = PopupFilterMode.Contains,
                    AllowNullInput = DevExpress.Utils.DefaultBoolean.True
                };

                // Optional: Only show "OperatorName" column
                operatorLookup.PopulateViewColumns();
                foreach (GridColumn column in operatorLookup.View.Columns)
                {
                    column.Visible = column.FieldName == "OperatorName";
                }

                // Show auto-filter row (search box)
                operatorLookup.View.OptionsView.ShowAutoFilterRow = true;
                operatorLookup.View.ActiveFilterEnabled = true;


                // Add to repository
                gridControlOverview.RepositoryItems.Add(deviceLookup);
                gridControlOverview.RepositoryItems.Add(operatorLookup);

                // Add to detailView columns
                var colDevice = detailView.Columns.AddField("MachineName");
                colDevice.Caption = "Device";
                colDevice.Visible = true;
                colDevice.ColumnEdit = deviceLookup;

                var colOperator = detailView.Columns.AddField("OperatorName");
                colOperator.Caption = "Operator";
                colOperator.Visible = true;
                colOperator.ColumnEdit = operatorLookup;

                // save operator and device in edit
                deviceLookup.EditValueChanged += (s, e) =>
                {
                    GridLookUpEdit editor = s as GridLookUpEdit;
                    if (editor == null) return;

                    // Get current value (DeviceID)
                    if (int.TryParse(editor.EditValue?.ToString(), out int selectedDeviceID))
                    {
                        // Get the DetailView and row handle
                        detailView = (editor.Parent as GridControl)?.FocusedView as GridView;
                        if (detailView == null) return;

                        int rowHandle = detailView.FocusedRowHandle;
                        if (!detailView.IsValidRowHandle(rowHandle)) return;

                        // Update the DataTable (subDistributionDataSource)
                        detailView.SetRowCellValue(rowHandle, "DeviceID", selectedDeviceID);

                        Console.WriteLine($"✅ DeviceID saved: {selectedDeviceID}");
                    }
                };
                operatorLookup.EditValueChanged += (s, e) =>
                {
                    GridLookUpEdit editor = s as GridLookUpEdit;
                    if (editor == null) return;

                    // Get selected EmployeeID
                    if (int.TryParse(editor.EditValue?.ToString(), out int selectedOperatorID))
                    {
                        // Get the DetailView and row handle
                        detailView = (editor.Parent as GridControl)?.FocusedView as GridView;
                        if (detailView == null) return;

                        int rowHandle = detailView.FocusedRowHandle;
                        if (!detailView.IsValidRowHandle(rowHandle)) return;

                        // Save to the dataset
                        detailView.SetRowCellValue(rowHandle, "EmployeeID", selectedOperatorID);

                        Console.WriteLine($"✅ OperatorID saved: {selectedOperatorID}");
                    }
                };


                // Create Delete button for detail rows
                RepositoryItemButtonEdit childDeleteButton = new RepositoryItemButtonEdit
                {
                    TextEditStyle = TextEditStyles.HideTextEditor
                };
                childDeleteButton.Buttons.Clear(); // Ensure no duplicates
                childDeleteButton.Buttons.Add(new EditorButton(ButtonPredefines.Delete)
                {
                    ToolTip = "Delete child row"
                });

                // Register once only
                if (!gridControlOverview.RepositoryItems.Contains(childDeleteButton))
                    gridControlOverview.RepositoryItems.Add(childDeleteButton);

                // Create button column (use dummy FieldName)
                if (detailView.Columns["Action"] == null) // Prevent double-add
                {
                    GridColumn deleteCol = new GridColumn
                    {
                        Caption = "Delete",
                        FieldName = "Action", // dummy, not in DataTable
                        ColumnEdit = childDeleteButton,
                        Visible = true,
                        Width = 60,
                        UnboundType = DevExpress.Data.UnboundColumnType.String // required since it's not in the DataTable
                    };

                    detailView.Columns.Add(deleteCol);
                }
                childDeleteButton.ButtonClick += (s, eArgs) =>
                {
                    // Get the active editor (grid control will provide this context)
                    ButtonEdit editor = s as ButtonEdit;
                    if (editor == null) return;

                    // Get the GridControl via editor’s parent
                    GridControl grid = editor.Parent as GridControl;
                    if (grid == null) return;

                    // Now get the view that contains this editor (detailView)
                    GridView view = grid.FocusedView as GridView;
                    if (view == null) return;

                    int rowHandle = view.FocusedRowHandle;
                    if (view.IsValidRowHandle(rowHandle))
                    {
                        view.DeleteRow(rowHandle);
                    }
                };
                detailView.CellValueChanged += DetailView_CellValueChanged;
                gridViewOverview.BeginUpdate();
                try
                {
                    for (int i = 0; i < gridViewOverview.DataRowCount; i++)
                    {
                        int rowHandle = gridViewOverview.GetVisibleRowHandle(i);
                        if (gridViewOverview.IsGroupRow(rowHandle))
                        {
                            gridViewOverview.ExpandGroupRow(rowHandle);
                        }
                    }
                }
                finally
                {
                    gridViewOverview.EndUpdate();
                }
                TranslateGridControlOverviewHeaders(detailView);
            }
            else
            {
                foreach (var name in new[] { "MachineName", "DeviceID", "OperatorName", "EmployeeID", "Action" +
                    "" })
                {
                    var column = gridViewOverview.Columns[name];
                    if (column != null)
                        gridViewOverview.Columns.Remove(column);
                }

                TranslateGridControlOverviewHeaders(gridViewOverview);
            }

            gridViewOverview.RefreshData();
        }
        private void gridViewOverview_ShowingEditor(object sender, CancelEventArgs e)
        {
            GridView view = sender as GridView;
            int rowHandle = view.FocusedRowHandle;
            GridColumn column = view.FocusedColumn;

            if (!view.IsValidRowHandle(rowHandle) || column == null)
                return;

            // Always allow "Action" column to be edited
            if (column.FieldName == "Action")
                return;

            // Get the current row's GroupSO
            string currentGroupSO = view.GetRowCellValue(rowHandle, "GroupSO")?.ToString();
            if (string.IsNullOrEmpty(currentGroupSO)) return;

            // Find the first visible row in this GroupSO → it's the parent
            for (int i = 0; i < view.RowCount; i++)
            {
                int handle = view.GetVisibleRowHandle(i);
                if (!view.IsValidRowHandle(handle)) continue;

                string groupSO = view.GetRowCellValue(handle, "GroupSO")?.ToString();
                if (groupSO == currentGroupSO)
                {
                    // If the current row is the parent and not "Action" column → block edit
                    if (handle == rowHandle)
                    {
                        e.Cancel = true;
                    }
                    break;
                }
            }
        }

        private void DetailView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName != "SizeQty") return;

            GridView detailView = sender as GridView;
            if (detailView == null || e.RowHandle < 0) return;

            DataRow childRow = detailView.GetDataRow(e.RowHandle);
            if (childRow == null) return;

            string parentSO = childRow["CompositeKey"].ToString();

            // Get parent row
            DataRow parentRow = table.AsEnumerable()
                .FirstOrDefault(r => r["CompositeKey"].ToString() == parentSO);

            if (parentRow == null) return;

            int parentSizeQty = parentRow["SizeQty"] != DBNull.Value ? Convert.ToInt32(parentRow["SizeQty"]) : 0;

            // Sum SizeQty of all child rows with this DistributionID
            int totalChildQty = subDistributionDataSource.AsEnumerable()
                 .Where(r => r.RowState != DataRowState.Deleted && r["CompositeKey"].ToString() == parentSO)
                 .Sum(r => TryParseInt(r["SizeQty"]));

            if (totalChildQty > parentSizeQty)
            {
                // Show warning and revert change
                detailView.SetRowCellValue(e.RowHandle, e.Column, 0);

                MessageBox.Show(
                    $"❌ Total of child SizeQty ({totalChildQty}) exceeds the parent's SizeQty ({parentSizeQty}).\nResetting this value.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
            }
        }

        private void gridViewOverview_ActionButtonClick(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.ToolTip != "Duplicate") return;

            int rowHandle = gridViewOverview.FocusedRowHandle;
            if (rowHandle < 0) return;

            var rowView = gridViewOverview.GetRow(rowHandle) as DataRowView;
            if (rowView == null) return;

            string parentSO = rowView["CompositeKey"].ToString();
            string parentSize = rowView["Size"].ToString();
            string parentPartName = rowView["PartName"].ToString();
            int parentSizeQty = TryParseInt(rowView["SizeQty"]);

            // Sum all existing SizeQty in child rows of this parent
            int totalChildQty = subDistributionDataSource.AsEnumerable()
              .Where(r => r.RowState != DataRowState.Deleted && r["CompositeKey"].ToString() == parentSO)
              .Sum(r => TryParseInt(r["SizeQty"]));

            // Case 2: Block if already full
            if (totalChildQty >= parentSizeQty)
            {
                MessageBox.Show($"❌ Cannot add more rows. Child total SizeQty ({totalChildQty}) already reached parent SizeQty ({parentSizeQty}).",
                    "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Case 1: Auto-fill SizeQty if exactly one row left
            int remainingQty = parentSizeQty - totalChildQty;

            DataRow child = subDistributionDataSource.NewRow();
            child["CompositeKey"] = parentSO;
            child["PartName"] = parentPartName;
            child["Size"] = parentSize;
            child["SizeQty"] = remainingQty;  // auto fill the rest
            child["InventoryQty"] = 0;
          
            subDistributionDataSource.Rows.Add(child);
            gridViewOverview.SetMasterRowExpanded(rowHandle, true);
        }
        private int TryParseInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;

            if (value is int i) return i;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return 0;
        }

        private void TranslateGridControlOverviewHeaders(GridView gridView)
        {
            foreach (GridColumn col in gridView.Columns)
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
        public void ReceiveFilteredData(List<ProductionSchedule> filteredSchedules, bool isDeviceData, bool isLeather)
        {
            this.isDeviceData = isDeviceData;
            this.isLeather = isLeather;

            // check enable and disable if isDeviceData or not
            gridLookUpDevice.Enabled = !isDeviceData;
            gridLookUpOperator.Enabled = !isDeviceData;
            btnSend.Enabled = false;

            tableLayoutMainDistribution.Enabled = true;
            ApplyMaterialFilterAndMonth();
            HandleReceivedSchedule(filteredSchedules);
            // show hide input cutting number here
            Action action = isLeather ? (Action)rdLeather_Checked : rdRawMaterial_Checked;
            action();
            Console.WriteLine($"Received {filteredSchedules.Count} schedules.");
            // Collect unique SizeIDs and PartIDs
            sizeIDs = new HashSet<int>(filteredSchedules.Select(s => s.SizeID));
            partIDs = new HashSet<int>(filteredSchedules.Select(s => s.PartId));
            if (partIDs.Count > 20)
            {
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
                UpdateUIGridViewOverview(productionSchedules, isDeviceData);
            }
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
            if (!isDeviceData)
            {
                if (gridLookUpDevice.EditValue == null || gridLookUpOperator.EditValue == null)
                {
                    ShowMessage.ShowInfo(LocalizationManager.GetString("RequiredDeviceAndOperator"));
                    return;
                }
            }
            // Refresh the grid to ensure it reflects the latest changes
            gridViewOverview.RefreshData();

            // Parse the input inventory value
            int inventoryInput = string.IsNullOrEmpty(txtInventory.Text) ? 0 : int.Parse(txtInventory.Text);

            int[] selectedRows = gridViewOverview.GetSelectedRows();


            if (selectedRows.Length < 1)
            {
                // No rows selected — update all visible rows
                for (int i = 0; i < gridViewOverview.RowCount; i++)
                {
                    int rowHandle = gridViewOverview.GetVisibleRowHandle(i);
                    if (!gridViewOverview.IsDataRow(rowHandle)) continue;

                    string partName = gridViewOverview.GetRowCellValue(rowHandle, "PartName")?.ToString();
                    string size = gridViewOverview.GetRowCellValue(rowHandle, "Size")?.ToString()?.Trim();
                    string normalizedSize = size?.Trim();

                    int inventory = int.TryParse(txtInventory.Text, out int inv) ? inv : 0;
                    object sizeQtyObj = gridViewOverview.GetRowCellValue(rowHandle, "SizeQty");
                    int sizeQty = sizeQtyObj != DBNull.Value ? Convert.ToInt32(sizeQtyObj) : 0;

                    if (sizeQty != 0 && inventory > sizeQty)
                    {
                        ShowMessage.ShowInfo($"Inventory must be ≤ SizeQty for: {partName} - {normalizedSize}");
                        continue;
                    }

                    gridViewOverview.SetRowCellValue(rowHandle, "InventoryQty", inventory);

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

                    // Update all values
                    int piecesPerPair = (int)numPiecesPerPair.Value;
                    int cuttingDieQty = (int)numCuttingDieQty.Value;
                    int materialLayer = (int)numMaterialLayer.Value;
                    int totalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;

                    foreach (var group in productionSchedules)
                    {
                        foreach (var schedule in group)
                        {
                            string scheduleSize = GetSizeName(schedule.SizeID);
                            if (string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(scheduleSize, normalizedSize, StringComparison.OrdinalIgnoreCase))
                            {
                                if (isLeather)
                                {
                                    schedule.TotalPiecesPerPair = totalPiecesPerPair;
                                    schedule.PeicesPerPair = 0;
                                    schedule.CuttingDieQty = 0;
                                    schedule.MaterialLayer = 0;
                                    gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", schedule.TotalPiecesPerPair);
                                }
                                else
                                {
                                    schedule.PeicesPerPair = piecesPerPair;
                                    schedule.CuttingDieQty = cuttingDieQty;
                                    schedule.MaterialLayer = materialLayer;
                                    schedule.TotalPiecesPerPair = 0;
                                    gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", schedule.PeicesPerPair);
                                    gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", schedule.CuttingDieQty);
                                    gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", schedule.MaterialLayer);
                                }
                            }
                        }
                    }

                    Console.WriteLine($"[Updated All] {partName} - {normalizedSize} => Inventory: {inventory}");
                }
                if (isDeviceData) {

                    foreach (DataRow parentRow in table.Rows)
                    {
                        if (parentRow.RowState == DataRowState.Deleted) continue;

                        int soID = Convert.ToInt32(parentRow["SO"]);

                        // Find matching child rows for this DistributionID
                        var matchingChildRows = subDistributionDataSource.AsEnumerable()
                            .Where(r => r.RowState != DataRowState.Deleted && Convert.ToInt32(r["SO"]) == soID);

                        foreach (DataRow child in matchingChildRows)
                        {
                            int? deviceID = (child.ItemArray[6] == null || child.ItemArray[6] == DBNull.Value)
                                ? (int?)null
                                : Convert.ToInt32(child.ItemArray[6]);

                            int? operatorID = (child.ItemArray[9] == null || child.ItemArray[9] == DBNull.Value)
                                ? (int?)null
                                : Convert.ToInt32(child.ItemArray[9]);

                            int inventoryQty = child["InventoryQty"] != DBNull.Value ? Convert.ToInt32(child["InventoryQty"]) : 0;
                            int childSizeQty = child["SizeQty"] != DBNull.Value ? Convert.ToInt32(child["SizeQty"]) : 0;

                            // Now apply updates to productionSchedules
                            foreach (var group in productionSchedules)
                            {
                                foreach (var schedule in group)
                                {
                                    schedule.AssignedOperators.RemoveAll(op => op.OperatorID == operatorID);

                                    schedule.AssignedOperators.Add(new OperatorInfo
                                    {
                                        OperatorID = operatorID,
                                        DeviceID = deviceID,
                                        InventoryQty = inventoryQty,
                                        SizeQty = childSizeQty
                                    });
                                }
                            }
                        }
                    }
                }

                //ShowMessage.ShowInfo("All rows updated successfully.");
            }
            if (selectedRows.Length > 0)
            {
                int inventory = int.TryParse(txtInventory.Text, out int result) ? result : 0;

                int selectedRowHandle = selectedRows[0];
                string partName = gridViewOverview.GetRowCellValue(selectedRowHandle, "PartName")?.ToString();
                string size = gridViewOverview.GetRowCellValue(selectedRowHandle, "Size")?.ToString();

                // Update productionSchedules
                string normalizedSize = size?.Trim();

                object sizeQtyObj = gridViewOverview.GetRowCellValue(selectedRowHandle, "SizeQty");
                int sizeQty = sizeQtyObj != DBNull.Value ? Convert.ToInt32(sizeQtyObj) : 0;

                if (sizeQty != 0 && inventory > sizeQty)
                {
                    ShowMessage.ShowInfo("Number of Inventory must be less than SizeQty");
                    return;
                }

                if (!isDeviceData)
                {

                    // fill cell OperatorName, Device and InventoryQty
                    string selectedOperatorName = gridLookUpOperator.Text.ToString().Trim();
                    int? selectedOperatorID = null;
                    if (int.TryParse(gridLookUpOperator.EditValue?.ToString().Trim(), out int tempOperatorID))
                    {
                        selectedOperatorID = tempOperatorID;
                    }

                    int? selectedDeviceID = null;
                    if (int.TryParse(gridLookUpDevice.EditValue?.ToString().Trim(), out int tempDeviceID))
                    {
                        selectedDeviceID = tempDeviceID;
                    }

                    gridViewOverview.SetRowCellValue(selectedRowHandle, "InventoryQty", inventory);
                    gridViewOverview.RefreshRow(selectedRowHandle);

                    foreach (var group in productionSchedules)
                    {
                        foreach (var schedule in group)
                        {
                            string scheduleSize = GetSizeName(schedule.SizeID);

                            if (string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(scheduleSize, normalizedSize, StringComparison.OrdinalIgnoreCase))
                            {
                                schedule.OperatorID = (int)selectedOperatorID;
                                schedule.DeviceID = (int)selectedDeviceID;
                                schedule.InventoryQty = inventory;
                            }
                        }
                    }
                }
                else
                {
                    foreach (DataRow parentRow in table.Rows)
                    {
                        if (parentRow.RowState == DataRowState.Deleted) continue;

                        string soID = parentRow["CompositeKey"].ToString();

                        // Find matching child rows for this DistributionID
                        var matchingChildRows = subDistributionDataSource.AsEnumerable()
                            .Where(r => r.RowState != DataRowState.Deleted && r["CompositeKey"].ToString() == soID);

                        foreach (DataRow child in matchingChildRows)
                        {
                            int? deviceID = (child.ItemArray[6] == null || child.ItemArray[6] == DBNull.Value)
                                ? (int?)null
                                : Convert.ToInt32(child.ItemArray[6]);

                            int? operatorID = (child.ItemArray[9] == null || child.ItemArray[9] == DBNull.Value)
                                ? (int?)null
                                : Convert.ToInt32(child.ItemArray[9]);

                            int inventoryQty = child["InventoryQty"] != DBNull.Value ? Convert.ToInt32(child["InventoryQty"]) : 0;
                            int childSizeQty = child["SizeQty"] != DBNull.Value ? Convert.ToInt32(child["SizeQty"]) : 0;

                            // Now apply updates to productionSchedules
                            foreach (var group in productionSchedules)
                            {
                                foreach (var schedule in group)
                                {
                                    schedule.AssignedOperators.RemoveAll(op => op.OperatorID == operatorID);

                                    schedule.AssignedOperators.Add(new OperatorInfo
                                    {
                                        OperatorID = operatorID,
                                        DeviceID = deviceID,
                                        InventoryQty = inventoryQty,
                                        SizeQty = childSizeQty
                                    });
                                }
                            }
                        }
                    }
                }

                // Log the results
                Console.WriteLine($"Selected PartName: {partName}");
                Console.WriteLine($"Selected Size: {normalizedSize}");
            }

            // Iterate through the selected rows
            bool hasGroupSelected = selectedRows.Any(r => gridViewOverview.IsGroupRow(r));
            if (!hasGroupSelected)
            {
                // No group selected → update all visible data rows directly
                for (int i = 0; i < gridViewOverview.RowCount; i++)
                {
                    int rowHandle = gridViewOverview.GetVisibleRowHandle(i);
                    if (!gridViewOverview.IsDataRow(rowHandle)) continue;

                    UpdateRow(rowHandle);
                }

                gridViewOverview.RefreshData();
                ShowMessage.ShowInfo(LocalizationManager.GetString("RowUpdated"), LocalizationManager.GetString("Information"));
            }
            else
            {
                // Group row(s) selected → update rows based on group
                foreach (int row in selectedRows)
                {
                    if (!gridViewOverview.IsGroupRow(row)) continue;

                    string groupSOHeader = gridViewOverview.GetGroupRowValue(row)?.ToString();
                    for (int i = 0; i < gridViewOverview.RowCount; i++)
                    {
                        int rowHandle = gridViewOverview.GetVisibleRowHandle(i);
                        if (!gridViewOverview.IsDataRow(rowHandle)) continue;

                        string groupSO = gridViewOverview.GetRowCellValue(rowHandle, "GroupSO")?.ToString();
                        if (!string.Equals(groupSO, groupSOHeader, StringComparison.OrdinalIgnoreCase)) continue;

                        UpdateRow(rowHandle);
                    }

                    ShowMessage.ShowInfo($"Group SO {groupSOHeader} data updated successfully.");
                }
            }
            gridViewOverview.RefreshData();
            btnSend.Enabled = AreAllRowsComplete(gridViewOverview);
        }
        private bool AreAllRowsComplete(GridView gridView)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                int rowHandle = gridView.GetVisibleRowHandle(i);
                if (!gridView.IsDataRow(rowHandle)) continue;

                // ❗ Check each row and return false if any are incomplete
                if (!CheckIsLeatherNullOrEmpty(gridView, rowHandle))
                    return false;
            }
            return true;
        }
        private bool CheckIsLeatherNullOrEmpty(GridView gridView, int rowHandle)
        {
            if (isLeather)
            {
                int totalPiecesPerPair = Convert.ToInt32(gridView.GetRowCellValue(rowHandle, "TotalPiecesPerPair") ?? 0);
                if (totalPiecesPerPair == 0)
                    return false;
            }
            else
            {
                int piecesPerPair = Convert.ToInt32(gridView.GetRowCellValue(rowHandle, "PeicesPerPair") ?? 0);
                int cuttingDieQty = Convert.ToInt32(gridView.GetRowCellValue(rowHandle, "CuttingDieQty") ?? 0);
                int materialLayer = Convert.ToInt32(gridView.GetRowCellValue(rowHandle, "MaterialLayer") ?? 0);

                if (piecesPerPair == 0 || cuttingDieQty == 0 || materialLayer == 0)
                    return false;
                if (!isDeviceData)
                {
                    if (gridLookUpDevice.EditValue == null || gridLookUpOperator.EditValue == null)
                        return false;
                }
            }
            return true;
        }
        private void UpdateRow(int rowHandle)
        {
            string partName = gridViewOverview.GetRowCellValue(rowHandle, "PartName")?.ToString();
            string size = gridViewOverview.GetRowCellValue(rowHandle, "Size")?.ToString()?.Trim();

            int piecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "PeicesPerPair") ?? 0);
            int cuttingDieQty = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "CuttingDieQty") ?? 0);
            int materialLayer = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "MaterialLayer") ?? 0);
            int totalPiecesPerPair = Convert.ToInt32(gridViewOverview.GetRowCellValue(rowHandle, "TotalPiecesPerPair") ?? 0);

            string groupSO = gridViewOverview.GetRowCellValue(rowHandle, "GroupSO")?.ToString();

            foreach (var group in productionSchedules)
            {
                foreach (var schedule in group)
                {
                    string scheduleSize = GetSizeName(schedule.SizeID);

                    if (string.Equals(schedule.GroupSO, groupSO, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(schedule.PartName, partName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(scheduleSize, size, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((int)numPiecesPerPair.Value != piecesPerPair ||
                            (int)numCuttingDieQty.Value != cuttingDieQty ||
                            (int)numMaterialLayer.Value != materialLayer ||
                            (int)numericTotalPeicesPerPair.Value != totalPiecesPerPair)
                        {
                            if (isLeather)
                            {
                                schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                                gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", schedule.TotalPiecesPerPair);
                            }
                            else
                            {
                                schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                                schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                                schedule.MaterialLayer = (int)numMaterialLayer.Value;

                                gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", schedule.PeicesPerPair);
                                gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", schedule.CuttingDieQty);
                                gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", schedule.MaterialLayer);
                            }
                        }
                    }
                }
            }
        }

        private void btnDeleteData_Click(object sender, EventArgs e)
        {
            ResetSendDistribution();
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

                            if (isLeather)
                            {
                                gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", 0);
                                gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", 0);
                                gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", 0);

                                schedule.TotalPiecesPerPair = (int)numericTotalPeicesPerPair.Value;
                                gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", schedule.TotalPiecesPerPair);
                            }
                            else
                            {
                                schedule.PeicesPerPair = (int)numPiecesPerPair.Value;
                                schedule.CuttingDieQty = (int)numCuttingDieQty.Value;
                                schedule.MaterialLayer = (int)numMaterialLayer.Value;

                                gridViewOverview.SetRowCellValue(rowHandle, "PeicesPerPair", schedule.PeicesPerPair);
                                gridViewOverview.SetRowCellValue(rowHandle, "CuttingDieQty", schedule.CuttingDieQty);
                                gridViewOverview.SetRowCellValue(rowHandle, "MaterialLayer", schedule.MaterialLayer);

                                gridViewOverview.SetRowCellValue(rowHandle, "TotalPiecesPerPair", 0);
                            }
                        }
                    }
                }
            }

            gridViewOverview.RefreshData();
        }
        private void ResetSendDistribution()
        {
            loadDeviceDistribution();
            loadOperatorDistribution();

            countSO = 0;

            // ✅ Clear the child table before the parent table to prevent constraint errors
            if (subDistributionDataSource != null)
                subDistributionDataSource.Clear();

            if (table != null)
                table.Clear();

            productionSchedules.Clear();
            gridControlOverview.DataSource = null;
            sizeDataList = new List<SizeData>();
            tableLayoutMainDistribution.Enabled = false;
            gridLookUpDevice.Clear();
            gridLookUpOperator.Clear();
        }
        public class DistributionPayload
        {
            public List<DistributionData> Distributions { get; set; } = new List<DistributionData>();
            public List<OperatorInfo> SubDistributions { get; set; } = new List<OperatorInfo>();
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            loadDeviceDistribution();
        }

        public class OperatorInfo
        {
            public int? OperatorID { get; set; }
            public int? DeviceID { get; set; }
            public int InventoryQty { get; set; }
            public int SizeQty { get; set; }
        }

    }
}