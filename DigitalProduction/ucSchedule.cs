using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
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

        public ucSchedule()
        {
            InitializeComponent();
            SetupGridControl();
            InitializeTotalLabel();
            InitializeMonthFilter();
        }

        private void SetupGridControl()
        {
            gridControl = new GridControl { Dock = DockStyle.Fill };
            gridView = new GridView(gridControl)
            {
                OptionsView = { ShowGroupPanel = false, ColumnAutoWidth = true }
            };
            gridControl.MainView = gridView;
            gridControl.DataSource = productionSchedules;
            gridView.OptionsBehavior.Editable = false;
            gridView.Appearance.HeaderPanel.Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Bold);
            gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.AntiqueWhite;
            gridView.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.Black;
            gridView.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            Controls.Add(gridControl);
        }

        private void InitializeTotalLabel()
        {
            lblTotalRecords = new Label
            {
                Dock = DockStyle.Bottom,
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                Text = "Total Records: 0",
                BackColor = System.Drawing.Color.Transparent
            };
            Controls.Add(lblTotalRecords);
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
                    Invoke(new Action(() => UpdateGrid(response.Schedule)));
                }
            }
            catch (Exception ex)
            {
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
            if (gridView.Columns.Count > 0)
            {
                gridView.Columns["OrderID"].Visible = false;
                gridView.Columns["LastNo"].Visible = false;
                gridView.Columns["PartSizeUnit"].Visible = false;
                gridView.Columns[18].Visible = false; //Part ID
                gridView.Columns["SizeID"].Visible = false;
                gridView.Columns["Process"].Visible = false;
            }
        }


        private void ApplyMonthFilter()
        {
            if (selectedMonth == null)
            {
                gridControl.DataSource = productionSchedules.ToList();
            }
            else
            {
                var filteredData = productionSchedules
                    .Where(schedule => schedule.CreatedAt.Year == selectedMonth.Value.Year && schedule.CreatedAt.Month == selectedMonth.Value.Month)
                    .ToList();
                gridControl.DataSource = filteredData;
            }
            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            int totalCount = (gridControl.DataSource as List<ProductionSchedule>)?.Count ?? 0;
            lblTotalRecords.Text = $"Total Records: {totalCount}";
            lblTotalRecords.BackColor = System.Drawing.Color.AntiqueWhite;
            lblTotalRecords.ForeColor = System.Drawing.Color.Green;
        }
    }
}
