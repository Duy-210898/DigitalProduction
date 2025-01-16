using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucDistribution : XtraUserControl
    {
        private DbHelper dbHelper;
        private readonly DataTable scheduleDataTable;
        private WebSocketClient _webSocketClient;
        public ucDistribution()
        {
            dbHelper = new DbHelper();
            InitializeComponent();
            scheduleDataTable = new DataTable();
            InitializeScheduleDataTable();
            InitializeDataGridViewColumns();
            txtMasterWorkOrder.Focus();
        }
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        private void InitializeScheduleDataTable()
        {
            scheduleDataTable.Columns.Add("MasterWorkOrder");
            scheduleDataTable.Columns.Add("Factory");
            scheduleDataTable.Columns.Add("LastNo");
            scheduleDataTable.Columns.Add("SO");
            scheduleDataTable.Columns.Add("PO");
            scheduleDataTable.Columns.Add("Model");
            scheduleDataTable.Columns.Add("ART");
            scheduleDataTable.Columns.Add("Size");
            scheduleDataTable.Columns.Add("SizeQty", typeof(int));
            scheduleDataTable.Columns.Add("PartId");
            scheduleDataTable.Columns.Add("PartName");
            scheduleDataTable.Columns.Add("MaterialsId");
            scheduleDataTable.Columns.Add("MaterialsName");
        }

        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }

        private void OnLanguageChanged()
        {
            // Logic to refresh UI text for the current language
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

                            case "saveDistributionData":
                                Console.WriteLine("Suscess");
                                break;

                            case "getUniquePages":
                                HandleGetUniquePagesResponse(scheduleResponse);
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
                SaveScheduleDataToTable(scheduleResponse.Schedule);
                HandleReceivedSchedule(scheduleResponse.Schedule);
            }
            else
            {
                ShowErrorNotification(scheduleResponse.Message);
            }
        }

        private void HandleGetUniquePagesResponse(ScheduleResponse scheduleResponse)
        {
            if (scheduleResponse.Status == "success")
            {
                HandleReceivedUniquePages(scheduleResponse.Pages);
            }
            else
            {
                ShowErrorNotification(scheduleResponse.Message);
            }
        }
        private void HandleReceivedUniquePages(List<int> uniquePages)
        {
            if (uniquePages == null || !uniquePages.Any())
            {
                ShowErrorNotification("No pages data received.");
                return;
            }

            if (cbxPage.InvokeRequired)
            {
                cbxPage.Invoke(new Action(() =>
                {
                    cbxPage.Items.Clear();
                    foreach (var page in uniquePages)
                    {
                        cbxPage.Items.Add(page.ToString());
                    }

                    if (cbxPage.Items.Count > 0)
                    {
                        cbxPage.SelectedIndex = 0;
                    }
                }));
            }
            else
            {
                cbxPage.Items.Clear();
                foreach (var page in uniquePages)
                {
                    cbxPage.Items.Add(page.ToString());
                }

                if (cbxPage.Items.Count > 0)
                {
                    cbxPage.SelectedIndex = 0;
                }
            }
        }
        // Đảm bảo gọi `InvokeRequired` đúng cách khi thao tác với DataGridView hoặc các điều khiển UI từ luồng khác
        private void HandleReceivedSchedule(List<ProductionSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
            {
                ShowErrorNotification("No schedule data received.");
                return;
            }

            // Thực hiện trong luồng UI
            if (InvokeRequired)
            {
                Invoke(new Action(() => HandleReceivedSchedule(schedules)));
                return;
            }

            // Khởi tạo lại các cột cho DataGridView nếu chưa được khởi tạo
            InitializeDataGridViewColumns();

            // Hiển thị các bảng điều khiển
            pnlMaterial.Visible = true;
            pnlSize.Visible = true;
            dgvSize.Rows.Clear();
            dgvMaterial.Rows.Clear();
            scheduleDataTable.Clear();

            // Lọc và sắp xếp các kích thước duy nhất
            var uniqueSortedSizes = schedules
                .Where(s => !string.IsNullOrEmpty(s.Size))
                .Select(s => s.Size)
                .Distinct()
                .OrderBy(size => size)
                .ToList();

            // Duyệt qua các kích thước duy nhất và thêm vào DataGridView
            foreach (var size in uniqueSortedSizes)
            {
                int rowIndex = dgvSize.Rows.Add();
                DataGridViewRow row = dgvSize.Rows[rowIndex];
                row.Cells["Size"].Value = size;

                // Tìm kiếm thông tin SizeQty và UnitUsage của kích thước
                var sizeSchedule = schedules.FirstOrDefault(s => s.Size == size);
                var sizeQty = sizeSchedule?.SizeQty ?? 0;
                var unitUsage = sizeSchedule?.UnitUsage ?? 0;

                row.Cells["SizeQty"].Value = sizeQty;
                row.Cells["UnitUsage"].Value = unitUsage;

                // Tính TotalUsage
                row.Cells["TotalUsage"].Value = sizeQty * unitUsage;

                // Lưu dữ liệu vào DataTable
                var dataRow = scheduleDataTable.NewRow();
                dataRow["Size"] = size;
                dataRow["SizeQty"] = sizeQty;
                scheduleDataTable.Rows.Add(dataRow);
            }

            // Lọc và sắp xếp các phần duy nhất từ schedule
            var uniqueParts = schedules
                .GroupBy(s => s.PartName)
                .Select(g => g.First())
                .OrderBy(part => part.PartId)
                .ToList();

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

            // Duyệt qua các phần vật liệu
            foreach (var schedule in uniqueParts)
            {
                int rowIndex = dgvMaterial.Rows.Add();
                DataGridViewRow row = dgvMaterial.Rows[rowIndex];
                row.Cells["PartId"].Value = schedule.PartId;
                row.Cells["PartName"].Value = schedule.PartName;
                row.Cells["MaterialsID"].Value = schedule.MaterialsId;
                row.Cells["MaterialsName"].Value = schedule.MaterialsName;

                // Lưu dữ liệu vào DataTable
                var dataRow = scheduleDataTable.NewRow();
                dataRow["PartId"] = schedule.PartId;
                dataRow["PartName"] = schedule.PartName;
                dataRow["MaterialsId"] = schedule.MaterialsId;
                dataRow["MaterialsName"] = schedule.MaterialsName;
                scheduleDataTable.Rows.Add(dataRow);
            }

            // Định dạng lại DataGridView
            FormatDataGridView(dgvSize);
            FormatDataGridView(dgvMaterial);
        }
        private void FormatDataGridView(DataGridView dgv)
        {
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.LightSteelBlue;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            dgv.AllowUserToAddRows = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dgv.RowHeadersVisible = false;
            UpdateColumnWidthPercentage(dgv);
            dgv.CellFormatting += (sender, e) =>
            {
                if (e.RowIndex % 2 == 0)
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.AliceBlue;
                }
                else
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                }
            };
        }
        private void UpdateColumnWidthPercentage(DataGridView dgv)
        {
            if (dgv.Columns.Count == 0) return;  
            float totalWidth = dgv.Width;
            int columnCount = dgv.Columns.Count;
            float[] columnPercentages = { 0.1f, 0.15f, 0.15f, 0.6f };

            for (int i = 0; i < columnCount; i++)
            {
                dgv.Columns[i].Width = (i < columnPercentages.Length)
                    ? (int)(totalWidth * columnPercentages[i])
                    : 0;
            }
        }
        private void InitializeDataGridViewColumns()
        {
            if (dgvSize.Columns.Count == 0)
            {
                dgvSize.Columns.Add("Size", "Size");
                dgvSize.Columns.Add("SizeQty", "Size Quantity");
                dgvSize.Columns.Add("UnitUsage", "Unit Usage");
                dgvSize.Columns.Add("TotalUsage", "Total Usage");
            }

            if (dgvMaterial.Columns.Count == 0)
            {
                dgvMaterial.Columns.Add("PartId", "Part ID");
                dgvMaterial.Columns.Add("PartName", "Part Name");
                dgvMaterial.Columns.Add("MaterialsID", "Materials ID");
                dgvMaterial.Columns.Add("MaterialsName", "Materials Name");
            }
        }

        private void SaveScheduleDataToTable(List<ProductionSchedule> schedules)
        {
            scheduleDataTable.Clear();

            foreach (var schedule in schedules)
            {
                var row = scheduleDataTable.NewRow();
                row["MasterWorkOrder"] = schedule.MasterWorkOrder ?? string.Empty;
                row["Factory"] = schedule.Factory ?? string.Empty;
                row["LastNo"] = schedule.LastNo ?? string.Empty;
                row["SO"] = schedule.SO ?? string.Empty;
                row["PO"] = schedule.PO ?? string.Empty;
                row["Model"] = schedule.Model ?? string.Empty;
                row["ART"] = schedule.ART ?? string.Empty;
                row["Size"] = schedule.Size ?? string.Empty;
                row["SizeQty"] = schedule.SizeQty;
                row["PartId"] = schedule.PartId ?? string.Empty;
                row["PartName"] = schedule.PartName ?? string.Empty;
                row["MaterialsId"] = schedule.MaterialsId ?? string.Empty;
                row["MaterialsName"] = schedule.MaterialsName ?? string.Empty;

                scheduleDataTable.Rows.Add(row);
            }
        }

        private void ShowErrorNotification(string message)
        {
            ShowMessage("Error", message, MessageBoxIcon.Error);
        }

        private async void txtMasterWorkOrder_Leave(object sender, EventArgs e)
        {
            string masterWorkOrder = txtMasterWorkOrder.Text.Trim();

            if (string.IsNullOrEmpty(masterWorkOrder))
            {
                ShowMessage("Input Error", "Please enter the Master Work Order.", MessageBoxIcon.Warning);
                return;
            }

            await SendGetUniquePagesRequestAsync(masterWorkOrder);
        }

        private async Task SendGetDevicesRequestAsync()
        {
                var request = JsonConvert.SerializeObject(new { action = "getDevices" });
                await _webSocketClient.SendAsync(request);
        }

        private async Task SendGetUniquePagesRequestAsync(string masterWorkOrder)
        {
            var request = new { action = "getUniquePages", masterWorkOrder };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void cbxPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            string masterWorkOrder = txtMasterWorkOrder.Text.Trim();
            string page = cbxPage.SelectedItem?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(masterWorkOrder))
            {
                SendGetScheduleRequestAsync(masterWorkOrder, page);
            }
        }

        private async void SendGetScheduleRequestAsync(string masterWorkOrder, string page)
        {
            var workOrderInfo = new { action = "getSchedule", masterWorkOrder, page };
            string jsonRequest = JsonConvert.SerializeObject(workOrderInfo);
            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void ucDistribution_Load(object sender, EventArgs e)
        {
            {
                List<string> machineNames = dbHelper.GetMachineNames();

                cbxDevice.DataSource = machineNames;
                cbxDevice.SelectedIndex = -1;
            }
        }
        private DistributionData GetDistributionDataFromControls()
        {
            string user = Global.Username;
            string machineName = cbxDevice.SelectedItem.ToString();
            string ipAddress = dbHelper.GetIpAddress(machineName);

            var distributionData = new DistributionData
            {
                MasterWorkOrder = lblMasterWorkOrder.Text.Replace("Master Work Order: ", ""),
                SO = lblSO.Text.Replace("SO: ", ""),
                Model = lblModel.Text.Replace("Model: ", ""),
                ART = lblArt.Text.Replace("ART: ", ""),
                SizeData = new List<SizeData>(),
                MaterialData = new List<MaterialData>(),
                User = user,
                IpAddress = ipAddress,
            };

            // Get SizeData from dgvSize
            foreach (DataGridViewRow row in dgvSize.Rows)
            {
                if (row.IsNewRow) continue;
                try
                {
                    distributionData.SizeData.Add(new SizeData
                    {
                        Size = row.Cells["Size"].Value?.ToString(),
                        SizeQty = Convert.ToInt32(row.Cells["SizeQty"].Value),
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing size data: " + ex.Message);
                }
            }

            // Get MaterialData from dgvMaterial
            foreach (DataGridViewRow row in dgvMaterial.Rows)
            {
                if (row.IsNewRow) continue;
                try
                {
                    distributionData.MaterialData.Add(new MaterialData
                    {
                        PartName = row.Cells["PartName"].Value?.ToString(),
                        MaterialsName = row.Cells["MaterialsName"].Value?.ToString()
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing material data: " + ex.Message);
                }
            }

            return distributionData;
        }

        private async void SendDistributionDataToServer()
        {
            try
            {
                var distributionData = GetDistributionDataFromControls();

                var request = new
                {
                    action = "saveDistributionData",
                    data = distributionData
                };

                string jsonRequestWrapper = JsonConvert.SerializeObject(request);

                // Gửi yêu cầu và nhận phản hồi từ máy chủ
                string jsonResponse = await _webSocketClient.SendAsync(jsonRequestWrapper);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    // Phân tích phản hồi JSON từ máy chủ
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
    }
}