using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;
using static DevExpress.Utils.Frames.FrameHelper;

namespace DigitalProduction
{
    public partial class ucDistribution : XtraUserControl
    {
        private DbHelper dbHelper;
        private ResourceManager resourceManager;
        private readonly DataTable scheduleDataTable;
        private WebSocketClient _webSocketClient;
        public ucDistribution()
        {
            dbHelper = new DbHelper();
            resourceManager = new ResourceManager("DigitalProduction.en", typeof(frmMain).Assembly);
            InitializeComponent();
            scheduleDataTable = new DataTable();
            UpdateFormTexts();
            InitializeScheduleDataTable();
            InitializeDataGridViewColumns();
            txtSO.Focus();
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
            UpdateFormTexts();
        }
        private void UpdateFormTexts()
        {
            labelControl1.Text = resourceManager.GetString("InputSO");
            labelControl2.Text = resourceManager.GetString("Page");
        }

        private void WebSocket_OnResponseReceived(string data)
        {
            Console.WriteLine("Response from server: " + data);
        }

        private void WebSocket_OnMessage(string jsonData)
        { 
            try
            {
                ResponseMessage<List<object>> response = ResponseMessage<List<object>>.FromJson(jsonData);

                if (response.Pages != null)
                {
                    HandleGetUniquePagesResponse(response);
                }
                else if (response.Schedules != null)
                {
                    HandleGetScheduleResponse(response);
                }
                else if (response.Action == "saveDistributionData")
                {
                    HandleSendSchedules(response);
                }
                else
                {
                    ShowMessage.ShowInfo("No Data Found");
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                ShowMessage.ShowError($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                ShowMessage.ShowError($"An error occurred: {ex.Message}");
            }

        }

        private void HandleGetScheduleResponse(ResponseMessage<List<object>> response)
        {
            if (response.Status == "success")
            {
                SaveScheduleDataToTable(response.Schedules);
                HandleReceivedSchedule(response.Schedules);
            }
            else
            {
                ShowErrorNotification(response.Message);
            }
        }
        private void HandleSendSchedules(ResponseMessage<List<object>> response) 
        {
            if (response.Status == "success")
            {
                ShowMessage.ShowSuccessfully("Saved distribution data successfully");
            }
            else
            {
                ShowErrorNotification(response.Message);
            }
        }

        private void HandleGetUniquePagesResponse(ResponseMessage<List<object>> response)
        {
            if (response.Status == "success")
            {
                HandleReceivedUniquePages(response.Pages);
            }
            else
            {
                ShowErrorNotification(response.Message);
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
                    cbxPage.Properties.Items.Clear();
                    foreach (var page in uniquePages)
                    {
                        cbxPage.Properties.Items.Add(page.ToString());
                    }

                    if (cbxPage.Properties.Items.Count > 0)
                    {
                        cbxPage.SelectedIndex = 0;
                    }       
                }));
            }
            else
            {
                cbxPage.Properties.Items.Clear();
                foreach (var page in uniquePages)
                {
                    cbxPage.Properties.Items.Add(page.ToString());
                }

                if (cbxPage.Properties.Items.Count > 0)
                {
                    cbxPage.SelectedIndex = 0;
                }
            }
        }

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
        ShowMessage.ShowError(message);
        }

        private async void txtMasterWorkOrder_Leave(object sender, EventArgs e)
        {
            string masterWorkOrder = txtSO.Text.Trim();

            if (string.IsNullOrEmpty(masterWorkOrder))
            {
                ShowMessage.ShowWarning("Input Error", "Please enter the Master Work Order.");
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
            string masterWorkOrder = txtSO.Text.Trim();
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

                string jsonResponse = await _webSocketClient.SendAsync(jsonRequestWrapper);
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