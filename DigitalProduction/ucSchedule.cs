using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucSchedule : UserControl
    {
        private BindingList<ProductionSchedule> productionSchedules = new BindingList<ProductionSchedule>();
        private WebSocketClient _webSocketClient;
        private DateTime? selectedMonth;
        private Label lblTotalRecords;
        private GridControl gridControl;
        private GridView gridView;
        private Button btnSendData;
        private readonly string[] columnsToHide = { "Factory", "OrderID", "LastNo", "PartSizeUnit", "SizeID", "MaterialUnit", "MaterialID", "Process", "PartId", "GroupSO" };

        public ucSchedule()
        {
            InitializeComponent();
            SetupGridControl();
            InitializeTotalLabel();
            InitializeMonthFilter();
        }

        public class ProductionScheduleComparer : IEqualityComparer<ProductionSchedule>
        {
            public bool Equals(ProductionSchedule x, ProductionSchedule y)
            {
                return x != null && y != null && x.PartId == y.PartId && x.SizeID == y.SizeID && x.OrderID == y.OrderID;
            }

            public int GetHashCode(ProductionSchedule obj)
            {
                return obj.GetHashCode();
            }
        }

        private async void BtnSendData_Click(object sender, EventArgs e)
        {

            // avoid dupliacte
            List<ProductionSchedule> filteredSchedules = GetFilteredData().Distinct(new ProductionScheduleComparer()).ToList();

            if (filteredSchedules.Count == 0)
            {
                MessageBox.Show("No data available to send.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool allSame = filteredSchedules
                .GroupBy(s => new {s.Model })
                .Count() == 1;

            if (!allSame) {
                MessageBox.Show("Please sure Model is the same", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            HashSet<int> sizeIDs = new HashSet<int>(filteredSchedules.Select(s => s.SizeID));
            if (sizeIDs.Count > 6) {
                MessageBox.Show("Only allow minimun or equal to 6 sizes", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //if (filteredSchedules.Count >= 20) {
            //    MessageBox.Show("Only allow 20 SO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            // Get reference to frmMain
            Form parentForm = this.FindForm();
            if (parentForm is frmMain mainForm)
            {
                // Switch to ucDistribution
                await mainForm.ShowUserControlAsync<ucDistribution>();

                // Send filtered data to ucDistribution
                if (mainForm._userControls.TryGetValue(typeof(ucDistribution), out UserControl userControl))
                {
                    if (userControl is ucDistribution distributionControl)
                    {
                        distributionControl.ReceiveFilteredData(filteredSchedules);
                    }
                }

                // 🔥 Highlight "Distribution" in Accordion Menu
                mainForm.HighlightSelectedItem(mainForm.btnDistribution);

                MessageBox.Show($"Sent {filteredSchedules.Count} records to Distribution!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private List<ProductionSchedule> GetFilteredData()
        {
            var filteredData = new List<ProductionSchedule>();

            if (gridView == null || gridView.DataSource == null)
                return filteredData;

            // Ensure the grid view is refreshed to reflect the latest filter changes
            gridView.RefreshData();

            // Iterate through filtered (visible) rows
            for (int i = 0; i < gridView.RowCount; i++)
            {
                int rowHandle = gridView.GetVisibleRowHandle(i);
                if (gridView.IsDataRow(rowHandle))
                {
                    var row = gridView.GetRow(rowHandle) as ProductionSchedule;
                    if (row != null)
                    {
                        filteredData.Add(row);
                    }
                }
            }

            return filteredData;
        }


        private void SetupGridControl()
        {
            gridControl = new GridControl { Dock = DockStyle.Fill };
            gridView = new GridView(gridControl)
            {
                OptionsView = { ShowGroupPanel = true, ColumnAutoWidth = true },
                OptionsBehavior = { AutoExpandAllGroups = true } // Automatically expands all groups
            };

            gridControl.MainView = gridView;
            gridControl.DataSource = productionSchedules;
            gridView.OptionsBehavior.Editable = true;

            gridView.Appearance.FilterPanel.Font = new Font("Segoe UI", 10F); // for filter panel
            gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10F, FontStyle.Bold); // optional: match header
           // gridView.Appearance.Row.Font = new Font("Segoe UI", 12F);         // optional: match row font
            gridView.RowHeight = 30; // increase height if needed

            // Customize headers
         //   gridView.Appearance.HeaderPanel.Font = new Font(gridView.Appearance.Row.Font, FontStyle.Bold);
            //gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.AntiqueWhite;
            gridView.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.Black;
            gridView.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Ensure groups are always expanded
            gridView.OptionsView.ShowGroupPanel = true;
            gridView.OptionsView.GroupDrawMode = DevExpress.XtraGrid.Views.Grid.GroupDrawMode.Office;
            gridView.OptionsView.ShowGroupedColumns = true;

            Controls.Add(gridControl);
            GroupGridViewColumns();
            gridView.ColumnFilterChanged += (sender, e) => UpdateTotalLabel();
        }

        private void GroupGridViewColumns()
        {
            gridView.ClearGrouping();

            GridColumn partNameColumn = gridView.Columns["PartName"];
            GridColumn sizeColumn = gridView.Columns["Size"];
            GridColumn soColumn = gridView.Columns["SO"];
            if (soColumn != null)
            {
                soColumn.GroupIndex = 0;
            }
            if (partNameColumn != null)
            {
                partNameColumn.GroupIndex = 1;
            }

            if (sizeColumn != null)
            {
                sizeColumn.GroupIndex = 2;
            }

            gridView.ExpandAllGroups(); // Expand all groups after setting
        }

        private void InitializeTotalLabel()
        {
            FlowLayoutPanel bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(5),
                BackColor = System.Drawing.Color.Transparent
            };

            lblTotalRecords = new Label
            {
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                Text = $" {LocalizationManager.GetString("TotalRecords")} 0",
                AutoSize = true,
                BackColor = System.Drawing.Color.AntiqueWhite,
                ForeColor = System.Drawing.Color.Green,
                Padding = new Padding(5)
            };

            btnSendData = new Button
            {
                Text = LocalizationManager.GetString("SelectData"),
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightBlue,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0) // Adds space between label and button
            };
            btnSendData.Click += BtnSendData_Click;

            // Add components to FlowLayoutPanel
            bottomPanel.Controls.Add(lblTotalRecords);
            bottomPanel.Controls.Add(btnSendData);

            // Add to UserControl
            Controls.Add(bottomPanel);
        }


        private void InitializeMonthFilter()
        {
            DateTimePicker dateTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/yyyy",
                Dock = DockStyle.Top
            };

            dateTimePicker.ValueChanged += (sender, e) =>
            {
                selectedMonth = new DateTime(dateTimePicker.Value.Year, dateTimePicker.Value.Month, 1);
                ApplyMonthFilter();
            };

            Controls.Add(dateTimePicker);
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage;
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            if (productionSchedules.Count == 0)
            {
                _ = GetDataAndLoadToGridAsync();
            }
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getSchedule" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ResponseMessage<List<ProductionSchedule>>>(jsonData);
                if (response != null && response.Status == "success" && response.Schedule != null)
                {
                    // Ensure UI update happens on the main thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => UpdateGrid(response.Schedule)));
                    }
                    else
                    {
                        UpdateGrid(response.Schedule);
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionManager.Instance.IsConnected = false;
                MessageBox.Show($"Error processing WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateGrid(List<ProductionSchedule> newSchedules)
        {
            productionSchedules.Clear();
            foreach (var schedule in newSchedules)
            {
                productionSchedules.Add(schedule);
            }

            ApplyMonthFilter();

            // Hide columns after data is bound
            HideGridColumns();
        }

        private void HideGridColumns()
        {
            foreach (var columnName in columnsToHide)
            {
                var column = gridView.Columns[columnName];
                if (column != null)
                {
                    column.Visible = false;
                }
            }

            TranslateHeaders();
        }


        private void TranslateHeaders()
        {
            if (gridControl.MainView is GridView gridView && gridView.Columns.Count > 0)
            {
                foreach (GridColumn col in gridView.Columns)
                {
                    string translatedText = LocalizationManager.GetString(col.FieldName);
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        col.Caption = translatedText;
                    }
                }
                gridView.LayoutChanged(); // Force update to reflect changes
            }
        }

        private void ApplyMonthFilter()
        {
            var filteredData = productionSchedules
                .Where(schedule => selectedMonth == null ||
                                  (schedule.CreatedAt.Year == selectedMonth.Value.Year &&
                                   schedule.CreatedAt.Month == selectedMonth.Value.Month))
                .ToList();

            // Update the grid control's data point
            gridControl.DataSource = filteredData;

            // Reset and apply grouping
            gridView.ClearGrouping();
            GroupGridViewColumns();
            gridView.ExpandAllGroups();
            gridView.RefreshData();

            if (!filteredData.Any())
            {
                MessageBox.Show("No records found for the selected month.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            if (gridView == null)
                return;

            int totalCount = gridView.DataRowCount; // Get only filtered rows
            lblTotalRecords.Text = $"{LocalizationManager.GetString("TotalRecords")} {totalCount}";
            lblTotalRecords.BackColor = System.Drawing.Color.AntiqueWhite;
            lblTotalRecords.ForeColor = System.Drawing.Color.Green;
        }
    }
}
