using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using Newtonsoft.Json;

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
            ApplyLocalization();

            txtSO.Focus();
        }
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }
        private DataTable TransformSizeData(DataTable originalTable)
        {
            DataTable transformedTable = new DataTable();

            transformedTable.Columns.Add("Field");

            foreach (DataRow row in originalTable.Rows)
            {
                transformedTable.Columns.Add(row["Size"].ToString());
            }

            // Lấy danh sách các tiêu đề cột (bỏ qua cột "Size")
            var columnNames = originalTable.Columns.Cast<DataColumn>()
                               .Where(c => c.ColumnName != "Size")
                               .Select(c => c.ColumnName)
                               .ToList();

            // Thêm dữ liệu vào bảng mới
            foreach (var columnName in columnNames)
            {
                DataRow newRow = transformedTable.NewRow();
                newRow["Field"] = columnName;

                foreach (DataRow row in originalTable.Rows)
                {
                    newRow[row["Size"].ToString()] = row[columnName];
                }

                transformedTable.Rows.Add(newRow);
            }

            return transformedTable;
        }
        private DataTable ConvertSizeDataToDataTable(List<SizeData> sizeDataList)
        {
            DataTable table = new DataTable();

            table.Columns.Add("Size", typeof(string));
            table.Columns.Add("SizeQty", typeof(int));
            table.Columns.Add("UnitUsage", typeof(decimal));
            table.Columns.Add("TotalUsage", typeof(decimal));
            table.Columns.Add("SelectSize", typeof(bool));

            foreach (var data in sizeDataList)
            {
                table.Rows.Add(data.Size, data.SizeQty, data.UnitUsage, data.TotalUsage, false);
            }

            return table;
        }
        private void ConfigureGridViewSize()
        {
            gridView_Size.OptionsView.ShowGroupPanel = false;
            gridView_Size.OptionsView.EnableAppearanceEvenRow = true;

            gridView_Size.OptionsSelection.MultiSelect = true;
            gridView_Size.OptionsCustomization.AllowSort = false;

            gridView_Size.OptionsCustomization.AllowRowSizing = false;

            gridView_Size.OptionsBehavior.Editable = true;

            gridView_Size.Appearance.HeaderPanel.BackColor = Color.LightSteelBlue;
            gridView_Size.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView_Size.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridView_Size.Appearance.HeaderPanel.Font = new Font("Arial", 10, FontStyle.Bold);

            gridView_Size.Columns["Field"].Caption = "Size";

            RepositoryItemCheckEdit checkEdit = new RepositoryItemCheckEdit();
            gridControl_Size.RepositoryItems.Add(checkEdit);

            foreach (DevExpress.XtraGrid.Columns.GridColumn column in gridView_Size.Columns)
            {
                column.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            }

            int gridHeight = gridControl_Size.Height;
            int rowHeight = gridHeight / 6;
            gridView_Size.RowHeight = rowHeight;

            gridView_Size.BestFitColumns();
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
                .OrderBy(part => part.PartCode)
                .ToList();
            // Chuyển đổi dữ liệu sang DataTable
            DataTable originalTable = ConvertSizeDataToDataTable(sizeDataList);
            DataTable transformedTable = TransformSizeData(originalTable);
            // Gán dữ liệu vào gridControl_Size
            gridControl_Size.DataSource = transformedTable;

            // Cấu hình hiển thị của gridView_Size
            ConfigureGridViewSize();
            // Duyệt qua các phần vật liệu và thêm vào materialDataList
            foreach (var schedule in uniqueParts)
            {
                materialDataList.Add(new MaterialData
                {
                    PartCode = schedule.PartCode,
                    PartName = schedule.PartName,
                    MaterialCode = schedule.MaterialCode,
                    MaterialName = schedule.MaterialName,
                    Unit = schedule.Unit
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

            // Bind the material data list to the gridControl_Material
            //gridControl_Material.DataSource = materialDataList;
            cbxPart.Properties.DataSource = materialDataList;

            FormatGridLookUpEdit();
        }

        private void FormatGridLookUpEdit()
        {
            cbxPart.Properties.DisplayMember = "PartName";
            cbxPart.Properties.ValueMember = "PartId";

            GridView view = cbxPart.Properties.View;
            view.Columns.AddVisible("PartCode", "Part Code");
            view.Columns.AddVisible("PartName", "Part Name");
            view.Columns.AddVisible("MaterialCode", "Material Code");
            view.Columns.AddVisible("MaterialName", "Material Name");
            cbxPart.Properties.PopupView = view;
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

        private void ShowErrorNotification(string message)
        {
            ShowMessage("Error", message, MessageBoxIcon.Error);
        }

        private void txtSO_Leave(object sender, EventArgs e)
        {
            string so = txtSO.Text.Trim();

            if (!string.IsNullOrEmpty(so))
            {
                SendGetScheduleRequestAsync(so);
            }
        }

        private async Task SendGetDevicesRequestAsync()
        {
            var request = JsonConvert.SerializeObject(new { action = "getDevices" });
            await _webSocketClient.SendAsync(request);
        }

        private async void SendGetScheduleRequestAsync(string so)
        {
            var soInfo = new { app = Global.App, action = "getSchedule", so };
            string jsonRequest = JsonConvert.SerializeObject(soInfo);
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
            lblFactory.Text = LocalizationManager.GetString("Factory") + ": " + lblFactory.Text.Split(':').Last().Trim();
            lblLastNo.Text = LocalizationManager.GetString("LastNo") + ": " + lblLastNo.Text.Split(':').Last().Trim();
            lblMasterWorkOrder.Text = LocalizationManager.GetString("MasterWorkOrder") + ": " + lblMasterWorkOrder.Text.Split(':').Last().Trim();
            lblSO.Text = LocalizationManager.GetString("SO") + ": " + lblSO.Text.Split(':').Last().Trim();
            lblPO.Text = LocalizationManager.GetString("PO") + ": " + lblPO.Text.Split(':').Last().Trim();
            lblModel.Text = LocalizationManager.GetString("Model") + ": " + lblModel.Text.Split(':').Last().Trim();
            lblArt.Text = LocalizationManager.GetString("ART") + ": " + lblArt.Text.Split(':').Last().Trim();

            // Cập nhật nút bấm và combobox
            btnSend.Text = LocalizationManager.GetString("Send");
            cbxDevice.Text = LocalizationManager.GetString("SelectDevice");

            // Nếu có GroupControl hoặc PanelControl thì cập nhật tiêu đề
            if (pnlMaterial != null) pnlMaterial.Text = LocalizationManager.GetString("Material");
            if (pnlSize != null) pnlSize.Text = LocalizationManager.GetString("SizeDistribution");

            // Nếu có GridLookUpEdit thì cập nhật tiêu đề popup
            cbxPart.Properties.NullText = LocalizationManager.GetString("SelectPart");
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