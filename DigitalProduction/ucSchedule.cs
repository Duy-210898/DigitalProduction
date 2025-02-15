using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucSchedule : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<ProductionSchedule> productionSchedules = new BindingList<ProductionSchedule>();
        private WebSocketClient _webSocketClient;
        private DateTime? selectedMonth; // Store selected filter month
        private LabelControl lblTotalRecords; // Label to display total records

        public ucSchedule()
        {
            InitializeComponent();
            SetupGridControl();
            InitializeTotalLabel();
            InitializeMonthFilter();
        }

        private void SetupGridControl()
        {
            gridControl_productionSchedule.DataSource = productionSchedules;
            gridview_productionSchedule.OptionsView.ShowGroupPanel = false;
            gridview_productionSchedule.BestFitColumns();
        }

        private void InitializeTotalLabel()
        {
            // Create and configure the total label
            lblTotalRecords = new LabelControl
            {
                Dock = DockStyle.Bottom,
                Appearance = { Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold) },
                Text = "Total Records: 0",
                BackColor = System.Drawing.Color.Transparent,
                ForeColor = System.Drawing.Color.Transparent,
            };

            // Add label to the control
            Controls.Add(lblTotalRecords);
        }

        private void InitializeMonthFilter()
        {
            // Create and configure the DateEdit control
            DateEdit dateEdit = new DateEdit
            {
                Dock = DockStyle.Top
            };

            // Set calendar view and display format
            dateEdit.Properties.CalendarView = DevExpress.XtraEditors.Repository.CalendarView.Vista;
            dateEdit.Properties.DisplayFormat.FormatString = "MM/yyyy";
            dateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;

            // Handle value change
            dateEdit.EditValueChanged += (sender, e) =>
            {
                selectedMonth = dateEdit.DateTime;
                ApplyMonthFilter();
            };

            // Add the DateEdit control to the form
            Controls.Add(dateEdit);
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
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
        }

        private void ApplyMonthFilter()
        {
            if (selectedMonth == null)
            {
                gridControl_productionSchedule.DataSource = productionSchedules;
            }
            else
            {
                var filteredData = productionSchedules
                    .Where(schedule => schedule.CreatedAt.Year == selectedMonth.Value.Year && schedule.CreatedAt.Month == selectedMonth.Value.Month)
                    .ToList();

                gridControl_productionSchedule.DataSource = new BindingList<ProductionSchedule>(filteredData);
            }

            gridControl_productionSchedule.RefreshDataSource();
            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            int totalCount = ((BindingList<ProductionSchedule>)gridControl_productionSchedule.DataSource)?.Count ?? 0;
            lblTotalRecords.Text = $"Total Records: {totalCount}";
        }
    }
}
