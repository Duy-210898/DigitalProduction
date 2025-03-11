using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucSchedule : UserControl
    {
        private BindingList<ProductionSchedule> productionSchedules = new BindingList<ProductionSchedule>();
        private WebSocketClient _webSocketClient;
        private DateTime? selectedMonth; // Store selected filter month
        private Label lblTotalRecords; // Label to display total records
        private DataGridView dataGridView_productionSchedule; // DataGridView for display

        public ucSchedule()
        {
            InitializeComponent();
            SetupDataGridView();
            InitializeTotalLabel();
            InitializeMonthFilter();
        }

        private void SetupDataGridView()
        {
            dataGridView_productionSchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };
            var headerStyle = new DataGridViewCellStyle
            {
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = System.Drawing.Color.AntiqueWhite,
                ForeColor = System.Drawing.Color.Black
            };
            dataGridView_productionSchedule.ColumnHeadersHeight = 35;
            dataGridView_productionSchedule.EnableHeadersVisualStyles = false; // Important: Disable default styles
            dataGridView_productionSchedule.ColumnHeadersDefaultCellStyle = headerStyle;

            // Optionally configure DataGridView styles and properties here

            // Add DataGridView to the user control
            Controls.Add(dataGridView_productionSchedule);
        }

        private void InitializeTotalLabel()
        {
            // Create and configure the total label
            lblTotalRecords = new Label
            {
                Dock = DockStyle.Bottom,
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                Text = "Total Records: 0",
                BackColor = System.Drawing.Color.Transparent
            };

            // Add label to the control
            Controls.Add(lblTotalRecords);
        }

        private void InitializeMonthFilter()
        {
            // Create and configure the DateTimePicker control
            DateTimePicker dateTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/yyyy",
                Dock = DockStyle.Top
            };

            // Handle value change
            dateTimePicker.ValueChanged += (sender, e) =>
            {
                selectedMonth = new DateTime(dateTimePicker.Value.Year, dateTimePicker.Value.Month, 1);
                ApplyMonthFilter();
            };

            // Add the DateTimePicker control to the form
            Controls.Add(dateTimePicker);
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                // Unsubscribe previous event handler if already set
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage;
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            if (productionSchedules.Count == 0) // Only fetch data if not already populated
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
        }

        private void ApplyMonthFilter()
        {
            if (selectedMonth == null)
            {
                dataGridView_productionSchedule.DataSource = productionSchedules.ToList();
            }
            else
            {
                var filteredData = productionSchedules
                    .Where(schedule => schedule.CreatedAt.Year == selectedMonth.Value.Year && schedule.CreatedAt.Month == selectedMonth.Value.Month)
                    .ToList();

                dataGridView_productionSchedule.DataSource = filteredData;
            }

            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            // Ensure DataSource is not null and count the number of items based on its type
            int totalCount = 0;

            if (dataGridView_productionSchedule.DataSource is BindingList<ProductionSchedule> bindingList)
            {
                totalCount = bindingList.Count;
            }
            else if (dataGridView_productionSchedule.DataSource is List<ProductionSchedule> list)
            {
                totalCount = list.Count;
            }

            lblTotalRecords.Text = $"Total Records: {totalCount}";
            lblTotalRecords.BackColor = System.Drawing.Color.AntiqueWhite;
            lblTotalRecords.ForeColor = System.Drawing.Color.Green;
        }
    }
}