using DevExpress.XtraEditors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalProduction
{
    public partial class ucDistribution : DevExpress.XtraEditors.XtraUserControl
    {
        private readonly WebSocketClient _webSocketClient;
        private readonly DataTable scheduleDataTable;

        //public void SetWebSocketClient(WebSocketClient webSocketClient)
        //{
        //    if (webSocketClient == null)
        //    {
        //        throw new ArgumentNullException(nameof(webSocketClient), "WebSocketClient cannot be null.");
        //    }

        //    _webSocketClient = webSocketClient;
        //    _webSocketClient.OnMessage += WebSocket_OnMessage;
        //}
        //public ucDistribution()
        //{
        //    InitializeComponent();
        //    if (_webSocketClient != null)
        //    {
        //        _webSocketClient.OnMessage += WebSocket_OnMessage;
        //    }
        //}
        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }
        private void OnLanguageChanged()
        {

        }

        private void ShowMessage(string title, string message, MessageBoxIcon icon)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }
        private void WebSocket_OnMessage(string data)
        {
            Console.WriteLine($"Received message from server: {data}");

            try
            {
                var response = JsonConvert.DeserializeObject<Response>(data);
                if (response != null && response.Action == "delete_order")
                {
                    NotificationManager.ShowNotification("Xác nhận xóa đơn", response.Message);

                    DialogResult dialogResult = MessageBox.Show(response.Message, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (dialogResult == DialogResult.Yes)
                    {
                        SendDeleteOrderConfirmation(response, "Yes");
                    }
                    else
                    {
                        SendDeleteOrderConfirmation(response, "No");
                    }
                }
                else
                {
                    var scheduleResponse = JsonConvert.DeserializeObject<ScheduleResponse>(data);
                    if (scheduleResponse != null)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (scheduleResponse.Action == "getSchedule")
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
                            else if (scheduleResponse.Action == "getUniquePages")
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
                        });
                    }
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                Console.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }
        private void HandleReceivedUniquePages(List<int> uniquePages)
        {
            if (uniquePages == null || !uniquePages.Any())
            {
                ShowErrorNotification("No pages data received.");
                return;
            }

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

        private void ShowErrorNotification(string message)
        {
            ShowMessage("Error", message, MessageBoxIcon.Error);
        }

        private void HandleReceivedSchedule(List<ProductionSchedule> schedules)
        {
            // Kiểm tra xem dữ liệu có tồn tại không
            if (schedules == null || !schedules.Any())
            {
                ShowErrorNotification("No schedule data received.");
                return;
            }

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

            // Kiểm tra giá trị Factory và hiển thị tên tương ứng
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
            dgvMaterial.AllowUserToAddRows = false;
            dgvSize.AllowUserToAddRows = false;
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


        private async void SendDeleteOrderConfirmation(Response response, string userChoice)
        {
            // Xây dựng đối tượng thông điệp với định dạng yêu cầu
            var message = new
            {
                action = "confirm_delete_order",
                orderId = response.Message,
                userChoice = userChoice
            };

            // Chuyển đối tượng thành chuỗi JSON
            string jsonMessage = JsonConvert.SerializeObject(message);

            // Gửi thông điệp JSON qua WebSocket đến server
            await _webSocketClient.SendAsync(jsonMessage);
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
    }
    public class Response
    {
        public string Action { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
    }
    public class ScheduleResponse
    {
        public string Action { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public List<int> Pages { get; set; }
        public List<ProductionSchedule> Schedule { get; set; }
    }
    public class ProductionSchedule
    {
        public string MasterWorkOrder { get; set; }
        public string PartId { get; set; }
        public string PartName { get; set; }
        public string MaterialsId { get; set; }
        public string MaterialsName { get; set; }
        public int SizeQty { get; set; }
        public string Factory { get; set; }
        public string ART { get; set; }
        public string Model { get; set; }
        public string PO { get; set; }
        public string SO { get; set; }
        public string Size { get; set; }
        public string LastNo { get; set; }
        public float UnitUsage { get; set; }
        public string ProductionProcess { get; set; }
    }
}
