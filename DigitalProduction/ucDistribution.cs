using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Models;
using Newtonsoft.Json;
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class ucDistribution : XtraUserControl
    {
        private DbHelper dbHelper;
        private WebSocketClient _webSocketClient;

        public ucDistribution()
        {
            dbHelper = new DbHelper();
            InitializeComponent();
            txtMasterWorkOrder.Focus();
        }
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
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

            // Hiển thị các bảng điều khiển
            pnlMaterial.Visible = true;
            pnlSize.Visible = true;

            var sizeDataList = new List<SizeData>();
            var materialDataList = new List<MaterialData>();

            // Lọc và sắp xếp các kích thước duy nhất
            var uniqueSortedSizes = schedules
                .Where(s => !string.IsNullOrEmpty(s.Size))
                .Select(s => s.Size)
                .Distinct()
                .OrderBy(size => size)
                .ToList();

            // Thêm dữ liệu mới vào sizeDataList
            foreach (var size in uniqueSortedSizes)
            {
                var sizeSchedule = schedules.FirstOrDefault(s => s.Size == size);
                sizeDataList.Add(new SizeData
                {
                    Size = size,
                    SizeQty = sizeSchedule?.SizeQty ?? 0,
                    UnitUsage = sizeSchedule?.UnitUsage ?? 0
                });
            }

            // Lọc và sắp xếp các phần duy nhất từ schedule
            var uniqueParts = schedules
                .GroupBy(s => s.PartName)
                .Select(g => g.First())
                .OrderBy(part => part.PartId)
                .ToList();

            // Duyệt qua các phần vật liệu và thêm vào materialDataList
            foreach (var schedule in uniqueParts)
            {
                materialDataList.Add(new MaterialData
                {
                    PartId = schedule.PartId,
                    PartName = schedule.PartName,
                    MaterialsId = schedule.MaterialsId,
                    MaterialsName = schedule.MaterialsName
                });
            }

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

            // Bind the size data list to the gridControl_Size
            gridControl_Size.DataSource = sizeDataList;

            // Bind the material data list to the gridControl_Material
            gridControl_Material.DataSource = materialDataList;
        }

        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }

        private void OnLanguageChanged()
        {
            ApplyLocalization();
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
            //foreach (DataGridViewRow row in gridView_Size.Rows)
            //{
            //    if (row.IsNewRow) continue;
            //    try
            //    {
            //        distributionData.SizeData.Add(new SizeData
            //        {
            //            Size = row.Cells["Size"].Value?.ToString(),
            //            SizeQty = Convert.ToInt32(row.Cells["SizeQty"].Value),
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("Error processing size data: " + ex.Message);
            //    }
            //}

            //// Get MaterialData from dgvMaterial
            //foreach (DataGridViewRow row in dgvMaterial.Rows)
            //{
            //    if (row.IsNewRow) continue;
            //    try
            //    {
            //        distributionData.MaterialData.Add(new MaterialData
            //        {
            //            PartName = row.Cells["PartName"].Value?.ToString(),
            //            MaterialsName = row.Cells["MaterialsName"].Value?.ToString()
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("Error processing material data: " + ex.Message);
            //    }
            //}

            return distributionData;
        }
        private void ApplyLocalization()
        {
            gridView_Size.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");

            gridView_Size.Columns["Size"].Caption = LocalizationManager.GetString("Size");
            gridView_Size.Columns["SizeQty"].Caption = LocalizationManager.GetString("SizeQty");
            gridView_Size.Columns["UnitUsage"].Caption = LocalizationManager.GetString("UnitUsage");
            gridView_Size.Columns["TotalUsage"].Caption = LocalizationManager.GetString("TotalUsage");
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