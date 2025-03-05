using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
        private int orderID = 0;
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

        public ucDistribution()
        {
            dbHelper = new DbHelper();
            InitializeComponent();
            ApplyLocalization();
            SetupSearchSOLookup();
            cbxSO.Focus();
            lbl_operatorName.Visible = false;
            // Subscribe to the CellValueChanged event
            dgvSize.CellContentClick += dgvSize_CellContentClick;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        private List<string> soList = new List<string>();

        private void SetupSearchSOLookup()
        {
            soList = DbHelper.GetSOList();

            cbxSO.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cbxSO.Properties.ImmediatePopup = true;
            cbxSO.Properties.Items.AddRange(soList);

            cbxSO.EditValueChanged += CbxSO_EditValueChanged;
        }

        private void CbxSO_EditValueChanged(object sender, EventArgs e)
        {
            ComboBoxEdit combo = sender as ComboBoxEdit;
            if (combo == null) return;

            string inputText = combo.Text.Trim();

            if (string.IsNullOrEmpty(inputText))
            {
                combo.Properties.Items.Clear();
                combo.Properties.Items.AddRange(soList);
                return;
            }

            var filteredList = soList.Where(so => so.StartsWith(inputText, StringComparison.OrdinalIgnoreCase)).ToList();

            combo.Properties.Items.Clear();
            combo.Properties.Items.AddRange(filteredList);

            if (filteredList.Count > 0)
                combo.ShowPopup();
            else
                combo.ClosePopup();
        }
        //private DataTable TransformSizeData(DataTable originalTable)
        //{
        //    DataTable transformedTable = new DataTable();

        //    transformedTable.Columns.Add("Field");

        //    foreach (DataRow row in originalTable.Rows)
        //    {
        //        transformedTable.Columns.Add(row["Size"].ToString());
        //    }

        //    // Lấy danh sách các tiêu đề cột (bỏ qua cột "Size")
        //    var columnNames = originalTable.Columns.Cast<DataColumn>()
        //                       .Where(c => c.ColumnName != "Size")
        //                       .Select(c => c.ColumnName)
        //                       .ToList();

        //    // Thêm dữ liệu vào bảng mới
        //    foreach (var columnName in columnNames)
        //    {
        //        DataRow newRow = transformedTable.NewRow();
        //        newRow["Field"] = columnName;

        //        foreach (DataRow row in originalTable.Rows)
        //        {
        //            newRow[row["Size"].ToString()] = row[columnName];
        //        }

        //        transformedTable.Rows.Add(newRow);
        //    }

        //    return transformedTable;
        //}
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
        private void ConfigureDataGridView()
        {
            // Set DataGridView properties similar to DevExpress GridView
            dgvSize.AllowUserToAddRows = false;
            dgvSize.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSize.MultiSelect = true;
            dgvSize.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSize.ColumnHeadersHeight = 35;

            // Header appearance
            dgvSize.EnableHeadersVisualStyles = false;
            dgvSize.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                //BackColor = Color.LightSteelBlue,
                ForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // Center align text in all cells
            foreach (DataGridViewColumn column in dgvSize.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Make specific columns read-only
            if (dgvSize.Columns.Count > 4)
            {
                dgvSize.Columns[0].Visible = false;
                dgvSize.Columns[1].ReadOnly = true;
                dgvSize.Columns[2].ReadOnly = true;
                dgvSize.Columns[3].ReadOnly = true;
                dgvSize.Columns[4].ReadOnly = true;
            }

            // Set row height dynamically
            int gridHeight = dgvSize.Height;
            int rowHeight = gridHeight / 6;
            dgvSize.RowTemplate.Height = rowHeight;

            // Auto-size columns to fit content
            dgvSize.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Add checkbox column (Equivalent to `RepositoryItemCheckEdit` in DevExpress)
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "SelectSize",
                HeaderText = "Select",
                Width = 50,
                TrueValue = true,
                FalseValue = false
            };

            if (dgvSize.Columns["SelectSize"] == null)
            {
                dgvSize.Columns.Insert(0, checkColumn);
            }
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
            orderID = schedules[0].OrderID;

            pnlSize.Visible = true;

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

            dgvSize.DataSource = ConvertDataTableToList(originalTable); ;

            // Cấu hình hiển thị của gridView_Size
            ConfigureDataGridView();
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
            lblMasterWorkOrder.Text = $"Master Work Order: {schedules.FirstOrDefault()?.MasterWorkOrder ?? string.Empty}";
            lblSO.Text = $"SO: {schedules.FirstOrDefault()?.SO ?? string.Empty}";
            lblPO.Text = $"PO: {schedules.FirstOrDefault()?.PO ?? string.Empty}";
            lblModel.Text = $"Model: {schedules.FirstOrDefault()?.Model ?? string.Empty}";
            lblArt.Text = $"ART: {schedules.FirstOrDefault()?.ART ?? string.Empty}";
            cbxPart.Properties.NullText = LocalizationManager.GetString("SelectPart");

            cbxPart.Properties.DataSource = materialDataList;

            FormatGridLookUpEdit();
        }
        private void FormatGridLookUpEdit()
        {
            cbxPart.Properties.DisplayMember = "PartName";
            cbxPart.Properties.ValueMember = "PartID";

            GridView view = cbxPart.Properties.View;
            view.Columns.Clear();

            // Thêm các cột
            view.Columns.AddVisible("PartCode", "Part Code");
            view.Columns.AddVisible("PartName", "Part Name");
            view.Columns.AddVisible("VietnameseName", "Vietnamese Name");
            view.Columns.AddVisible("MaterialCode", "Material Code");
            view.Columns.AddVisible("MaterialName", "Material Name");

            // 🔹 Định dạng tiêu đề cột (Header)
            foreach (GridColumn col in view.Columns)
            {
                col.AppearanceHeader.Font = new Font("Tahoma", 9F, FontStyle.Bold);
                col.AppearanceHeader.Options.UseFont = true;
                col.AppearanceHeader.Options.UseTextOptions = true;
                col.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

                // Không cho chỉnh sửa nội dung
                col.OptionsColumn.AllowEdit = false;
                col.OptionsColumn.ReadOnly = true;
            }

            if (!string.IsNullOrEmpty(cbxSO.Text))
            {
                // SO is empty, set rdRawMaterial as checked
                rdRawMaterial.Checked = true;
            }

            // 🔹 Enable Multi-Selection with Checkboxes

            view.OptionsSelection.EnableAppearanceFocusedCell = true;
            view.FocusRectStyle = DrawFocusRectStyle.CellFocus;

            // 🔹 Giữ chế độ chọn dòng bằng checkbox nhưng cho phép chọn ô riêng lẻ
            view.OptionsBehavior.Editable = false;
            view.OptionsBehavior.AllowIncrementalSearch = true;

            // 🔹 Bật filter tìm kiếm
            view.OptionsView.ShowAutoFilterRow = true;

            // 🔹 Highlight hàng khi di chuột
            view.Appearance.FocusedRow.BackColor = Color.LightSkyBlue;
            view.Appearance.FocusedRow.Options.UseBackColor = true;

            // 🔹 Bật chế độ auto-fit cho cột
            view.OptionsView.ColumnAutoWidth = false;

            // Đặt chiều rộng popup bằng với kích thước control chứa nó
            int popupWidth = this.Width - 50;
            cbxPart.Properties.PopupFormSize = new Size(popupWidth, 300);

            // 🔹 Tô màu xen kẽ các dòng
            view.OptionsView.EnableAppearanceEvenRow = true;
            view.OptionsView.EnableAppearanceOddRow = true;
            view.Appearance.OddRow.BackColor = Color.AliceBlue;
            view.Appearance.EvenRow.BackColor = Color.White;

            // 🔹 Đặt tỷ lệ % cho từng cột
            view.Columns["PartCode"].Width = (int)(popupWidth * 0.05);
            view.Columns["PartName"].Width = (int)(popupWidth * 0.20);
            view.Columns["VietnameseName"].Width = (int)(popupWidth * 0.25);
            view.Columns["MaterialCode"].Width = (int)(popupWidth * 0.05);
            view.Columns["MaterialName"].Width = (int)(popupWidth * 0.45);

            // 🔹 Định dạng lại cột checkbox để nhỏ lại
            view.Columns[0].Width = 30; // Thu nhỏ cột chứa checkbox

            // Áp dụng view vào GridLookUpEdit
            cbxPart.Properties.PopupView = view;
            // Subscribe to Popup Opened event
            cbxPart.QueryPopUp += (s, e) =>
            {
                view.ClearSelection();
                foreach (int id in partIDs)
                {
                    int rowHandle = view.LocateByValue("PartID", id);
                    if (rowHandle >= 0)
                    {
                        view.SelectRow(rowHandle);
                    }
                }
            };

            // Subscribe to SelectionChanged to update value based on selections
            view.SelectionChanged += (sender, e) =>
            {
                int rowHandle = e.ControllerRow;
                if (rowHandle >= 0)
                {
                    object partIdObj = view.GetRowCellValue(rowHandle, "PartID");
                    if (partIdObj != null)
                    {
                        int partId = Convert.ToInt32(partIdObj);

                        if (view.IsRowSelected(rowHandle))  // Checked
                        {
                            if (!partIDs.Contains(partId))
                                partIDs.Add(partId);
                        }
                        else  // Unchecked
                        {
                            partIDs.Remove(partId);
                        }
                    }
                }

                // Update ComboBox Display
                string selectedParts = string.Join(", ", partIDs.Select(id => view.GetRowCellValue(view.LocateByValue("PartID", id), "PartName")));
                lblPartValue.Text = selectedParts;
                lblPartValue.ForeColor = Color.Green;
            };

            // Detach event handler when the popup closes
            cbxPart.CloseUp += (s, e) =>
            {
                view.SelectionChanged -= (sender, ee) => { /* Your logic */ };
            };
        }

        private void tableLayoutPanel1_SizeChanged(object sender, EventArgs e)
        {
            int popupWidth = this.Width - 50; // Adjust for any padding or margins
            cbxPart.Properties.PopupFormSize = new Size(popupWidth, 300);

            GridView view = cbxPart.Properties.View;
            if (view != null && view.Columns.Count < 0)
            {
                // Set specific widths for other columns
                view.Columns["PartCode"].Width = (int)(popupWidth * 0.05); // 5% for PartCode
                view.Columns["PartName"].Width = (int)(popupWidth * 0.20); // 20% for PartName
                view.Columns["VietnameseName"].Width = (int)(popupWidth * 0.25); // 25% for VietnameseName
                view.Columns["MaterialCode"].Width = (int)(popupWidth * 0.05); // 5% for MaterialCode

                // Make the MaterialName column take up the remaining space
                view.Columns["MaterialName"].Width = popupWidth -
                    (view.Columns["PartCode"].Width +
                     view.Columns["PartName"].Width +
                     view.Columns["VietnameseName"].Width +
                     view.Columns["MaterialCode"].Width + 30); // Adjust for padding or spacing
            }
        }



        private void ShowMessage(string title, string message, MessageBoxIcon icon)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }
        private void WebSocket_OnResponseReceived(string data)
        {
            Console.WriteLine("Response from server: " + data);
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
                                loadOperator(scheduleResponse.Employee[0]);
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
            ShowMessage("Error", message, MessageBoxIcon.Error);
        }

        private void cbxSO_SelectedIndexChanged(object sender, EventArgs e)
        {
            string so = cbxSO.SelectedItem.ToString();

            if (!string.IsNullOrEmpty(so))
            {
                SendGetScheduleRequestAsync(so);
            }
        }

        private async void cbxDevice_SelectedValueChanged(object sender, EventArgs e)
        {
            if (cbxDevice.SelectedIndex > 0)
            {
                string deviceID = cbxDevice.SelectedValue.ToString();
                string IpAddress = await dbHelper.GetIPAddressByDeviceIDAsync(int.Parse(deviceID));
                if (!string.IsNullOrEmpty(deviceID) || !string.IsNullOrEmpty(IpAddress))
                {
                    SendGetOperatorRequestAsync(IpAddress);
                }
            }
        }

        private async void SendGetOperatorRequestAsync(string IpAddress)
        {
            var request = JsonConvert.SerializeObject(new {app = Global.App, action = "getOperatorDistribution", IpAddress });
            await _webSocketClient.SendAsync(request);
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

        private async void SendGetScheduleRequestAsync(string so)
        {
            var soInfo = new { app = Global.App, action = "getSchedule", so };
            string jsonRequest = JsonConvert.SerializeObject(soInfo);
            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void ucDistribution_Load(object sender, EventArgs e)
        {
                loadDeviceDistribution();
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
            List<DistributionData> results = new List<DistributionData>();
            partSizeOrderIDs.Clear();
            //userID = Global.CurrentUser.
            deviceID = int.Parse(cbxDevice.SelectedValue.ToString());
            operatorID = int.Parse(lbl_operatorID.Text);
            int inventory = int.TryParse(txtInventory.Text, out int result) ? result : 0;
            cuttingDieQty = int.Parse(numCuttingDieQty.Text);
            piecesPerPair = int.Parse(numPiecesPerPair.Text);
            materialLayer = int.Parse(numMaterialLayer.Text);
            totalPiecesPerPair = int.Parse(numericTotalPeicesPerPair.Text);
            int productId = dbHelper.getProductIdByArt(lblArt.Text.Split(':')[1]);
            bool isLeather = false;
            int partID;

            // 1 part has many sizes - care part
            if (rdRawMaterial.Checked)
            {
                partID = (int)cbxPart.EditValue;
                // get partoderID
                foreach (int i in sizeIDs)
                {
                    partSizeOrderIDs.Add(dbHelper.getPartSizeOrderId(partID, i, orderID));
                }
            }
            // 1-3 size has many parts - care size
            else
            {
                isLeather = true;
                foreach (int i in sizeIDs)
                {
                    foreach (int partId in partIDs)
                    {
                        partSizeOrderIDs.Add(dbHelper.getPartSizeOrderId(partId, i, orderID));
                    }
                }
            }

            // save on DB
            foreach (int i in partSizeOrderIDs) {
                var distributionData = new DistributionData
                {
                    DeviceID = deviceID,
                    OperatorID = operatorID,
                    UserID = userID,
                    ProductID = productId,
                    PartSizeOrderID = i,
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
            }
            return results;
        }

        //    return distributionData;
        //}
        //private DistributionData GetDistributionDataFromControls()
        //{
        //    string user = Global.CurrentUser.EmployeeName;
        //    string machineName = cbxDevice.SelectedItem.ToString();
        //    string ipAddress = dbHelper.GetIpAddress(machineName);

            //    var distributionData = new DistributionData
            //    {
            //        MasterWorkOrder = lblMasterWorkOrder.Text.Replace("Master Work Order: ", ""),
            //        SO = lblSO.Text.Replace("SO: ", ""),
            //        Model = lblModel.Text.Replace("Model: ", ""),
            //        ART = lblArt.Text.Replace("ART: ", ""),
            //        SizeData = new List<SizeData>(),
            //        MaterialData = new List<MaterialData>(),
            //        User = user,
            //        IpAddress = ipAddress,
            //    };

            //    return distributionData;
            //}


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
            { btnSend, "Send" }
        };

            foreach (var control in controls)
            {
                control.Key.Text = LocalizationManager.GetString(control.Value) + (control.Key is Label ? ":" : "");
            }

            cbxDevice.Text = LocalizationManager.GetString("SelectDevice");
            cbxPart.Properties.NullText = LocalizationManager.GetString("SelectPart");
            rdLeather.Text = LocalizationManager.GetString("Leather");
            rdRawMaterial.Text = LocalizationManager.GetString("RawMaterial");
        }

        private async void SendDistributionDataToServer()
        {
            try
            {
                List<DistributionData> distributionData = getDistributionDataFromControls();
                var request = new
                {
                    app =  Global.App,
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
                        MessageBox.Show("Distribution Data saved successfully!\nThank you!", "Saved Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to save data. Message: " + response.Message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("No response received from the server.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while sending data: " + ex.Message);
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
            SendDistributionDataToServer();
        }

        private void rdLeather_CheckedChanged(object sender, EventArgs e)
        {
            cbxPart.Properties.NullText = "Please select Part...";
            cbxPart.EditValue = null;
            partIDs.Clear();
            setControlVisibility(false, numCuttingDieQty, numMaterialLayer, numPiecesPerPair, lblCuttingDie, lblMaterialLayer, lblPeicesPerPair);
            setControlVisibility(true, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
            if (rdLeather.Checked)
            {
                dataGrid_overviewDistribution.DataSource = null;
                if (cbxPart.Properties.DataSource != null)
                {
                    cbxPart.Properties.DataSource = materialDataList.Where(m => m.Unit == "FT2").ToList();
                // Allow multi-selection for cbxPart
                cbxPart.Properties.View.OptionsSelection.MultiSelect = true;
                cbxPart.Properties.View.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;
                }
            }
        }

        private void rdRawMaterial_CheckedChanged(object sender, EventArgs e)
        {
            cbxPart.Properties.NullText = "Please select Part...";
            cbxPart.EditValue = null;
            partIDs.Clear();
            setControlVisibility(false, numericTotalPeicesPerPair, lblTotalPeicesPerPair);
            setControlVisibility(true, numCuttingDieQty, numMaterialLayer, numPiecesPerPair, lblCuttingDie, lblMaterialLayer, lblPeicesPerPair);
            if (rdRawMaterial.Checked)
            {
                dataGrid_overviewDistribution.DataSource = null;
                lblPartValue.ResetText();
                if (cbxPart.Properties.DataSource != null)
                {
                    cbxPart.Properties.DataSource = materialDataList.Where(m => m.Unit == "YD").ToList();
                    // Allow single selection for cbxPart
                    cbxPart.Properties.View.OptionsSelection.MultiSelect = false;
                }
            }
        }
        private List<SizeData> ConvertDataTableToList(DataTable originalTable)
        {
            List<SizeData> sizeDataList = new List<SizeData>();

            // Iterate through each row in the DataTable
            foreach (DataRow row in originalTable.Rows)
            {
                // Create a new SizeData object for each row
                SizeData sizeData = new SizeData
                {
                    SizeID = Convert.ToInt32(row["SizeID"]),
                    Size = row["Size"].ToString(),
                    SizeQty = Convert.ToInt32(row["SizeQty"]),
                    UnitUsage = Convert.ToSingle(row["UnitUsage"]),
                    TotalUsage = Convert.ToSingle(row["TotalUsage"]),
                    SelectSize = Convert.ToBoolean(row["SelectSize"])
                };

                // Add the SizeData object to the list
                sizeDataList.Add(sizeData);
            }

            return sizeDataList;
        }
        private void dgvSize_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ensure the event fires only for the checkbox column
            if (dgvSize.Columns[e.ColumnIndex].Name == "SelectSize" && e.RowIndex >= 0)
            {
                DataGridViewCheckBoxCell checkBoxCell = (DataGridViewCheckBoxCell)dgvSize.Rows[e.RowIndex].Cells["SelectSize"];
                bool isChecked = (bool)(checkBoxCell.Value ?? false); // Handle null values

                // Get SizeID from the selected row
                int sizeId = Convert.ToInt32(dgvSize.Rows[e.RowIndex].Cells["SizeID"].Value);
                    
                if (!isChecked) // If user is checking the box
                {
                    // if Lether only 3 sizes
                    if (rdLeather.Checked)
                    {
                        if (sizeIDs.Count >= 3)
                        {
                            MessageBox.Show("You can select a maximum of 3 sizes.", "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    // if raw only 6 sizes
                    if (rdRawMaterial.Checked)
                    {
                        if (sizeIDs.Count >= 6)
                        {
                            MessageBox.Show("You can select a maximum of 3 sizes.", "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    sizeIDs.Add(sizeId);
                }
                else // If user is unchecking the box
                {
                    sizeIDs.Remove(sizeId);
                }

                // Toggle the checkbox manually since CellContentClick happens before the value is updated
                checkBoxCell.Value = !isChecked;

                // Refresh DataGridView to apply the change
                dgvSize.Refresh();

                UpdateOverviewDistributionGrid();
            }
        }


        private void setControlVisibility(bool isVisible, params Control[] controls)
        {
            foreach (var control in controls)
            {
                control.Visible = isVisible;
            }
        }
        private void UpdateOverviewDistributionGrid()
        {
            DataTable overviewTable = new DataTable();
            overviewTable.Columns.Add("PartName", typeof(string));
            overviewTable.Columns.Add("Size", typeof(string));

            Dictionary<string, HashSet<string>> partSizeMap = new Dictionary<string, HashSet<string>>();

            if (rdRawMaterial.Checked && cbxPart.EditValue != null)
            {
                partIDs.Add((int)cbxPart.EditValue);
            }

            // Loop over the selected parts and sizes
            foreach (int partId in partIDs)
            {
                var part = materialDataList.FirstOrDefault(m => m.PartID == partId);
                if (part == null)
                    continue;

                if (!partSizeMap.ContainsKey(part.PartName))
                {
                    partSizeMap[part.PartName] = new HashSet<string>(); // Store unique sizes
                }

                foreach (int sizeId in sizeIDs)
                {
                    string sizeName = GetSizeName(sizeId);
                    partSizeMap[part.PartName].Add(sizeName);
                }
            }

            // Add grouped data to DataTable
            foreach (var entry in partSizeMap)
            {
                string partName = entry.Key;
                string sizeList = string.Join(", ", entry.Value); // Merge sizes as a comma-separated string

                overviewTable.Rows.Add(partName, sizeList);
            }

            // Bind the table to your overview grid.
            dataGrid_overviewDistribution.DataSource = overviewTable;
            updateUIDataGridOverView();

        }

        private void updateUIDataGridOverView() {
            dataGrid_overviewDistribution.Dock = DockStyle.Fill;
            dataGrid_overviewDistribution.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_overviewDistribution.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGrid_overviewDistribution.AllowUserToResizeRows = false;
            dataGrid_overviewDistribution.AllowUserToResizeColumns = false;

            // Set alternating row colors for better readability
            dataGrid_overviewDistribution.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;

            // Center align content in cells
            dataGrid_overviewDistribution.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGrid_overviewDistribution.DefaultCellStyle.Font = new Font("Segoe UI", 10);

        
            // Allow selection of entire rows
            dataGrid_overviewDistribution.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid_overviewDistribution.MultiSelect = false;
            dataGrid_overviewDistribution.ReadOnly = true;  // Prevent editing if needed

        }

        private string GetSizeName(int sizeId)
        {
            var sizeData = sizeDataList.FirstOrDefault(s => s.SizeID == sizeId);
            return sizeData != null ? sizeData.Size : string.Empty;
        }


    }
}