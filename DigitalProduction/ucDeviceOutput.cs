using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucDeviceOutput : UserControl
    {
        private WebSocketClient _webSocketClient;
        private List<DeviceOutput> _deviceOutputList = new List<DeviceOutput>();
        private BindingSource _bindingSource = new BindingSource();
        private Timer _timer;

        public ucDeviceOutput()
        {
            InitializeComponent();

            // Initialize DataGridView
            dataGrid_DeviceOutput.DataSource = _bindingSource;
            dataGrid_DeviceOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_DeviceOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Attach the CellFormatting event
            dataGrid_DeviceOutput.CellFormatting += dataGrid_DeviceOutput_CellFormatting;

            // Handle resize event
            this.Resize += new EventHandler(UcDeviceOutput_Resize);
            // Initialize Timer
            _timer = new Timer { Interval = 2000 }; // 2 seconds
            _timer.Tick += Timer_Tick; // Attach event handler
        }
        private async void Timer_Tick(object sender, EventArgs e)
        {
            await GetDataAndLoadToGridAsync(); // Refresh data on timer tick
        }

        private void UcDeviceOutput_Resize(object sender, EventArgs e)
        {
            if (dataGrid_DeviceOutput.Columns.Count > 0)
            {
                foreach (DataGridViewColumn column in dataGrid_DeviceOutput.Columns)
                {
                    column.Width = dataGrid_DeviceOutput.Width / dataGrid_DeviceOutput.Columns.Count;
                }
            }
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseRealTime += WebSocket_OnMessage;
            if (this.Visible) // Only start the timer if the control is visible
            {
                _timer.Start();
            }
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getActualData" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendRealTimeAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<WebSocketResponse>(jsonData);

                if (response?.RealTime != null && response.Status == "success")
                {
                    // Ensure thread-safe UI updates
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => UpdateUI(response.RealTime)));
                    }
                    else
                    {
                        UpdateUI(response.RealTime);
                    }
                }
                else
                {
                    MessageBox.Show("Error: No Data Found", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateUI(RealTimeData realTimeData)
        {
            // Store previous values
            Dictionary<int, Dictionary<int, object>> previousValues = new Dictionary<int, Dictionary<int, object>>();

            for (int i = 0; i < dataGrid_DeviceOutput.Rows.Count; i++)
            {
                previousValues[i] = new Dictionary<int, object>();
                for (int j = 0; j < dataGrid_DeviceOutput.Columns.Count; j++)
                {
                    previousValues[i][j] = dataGrid_DeviceOutput.Rows[i].Cells[j].Value;
                }
            }

            // Update Labels
            lblOrder.Text = $"Order: {realTimeData.OrderID}";
            lblMasterWorkOrder.Text = $"MasterWorkOrder: {realTimeData.MasterWorkOrder}";
            lblSO.Text = $"SO: {realTimeData.SO}";
            lblModel.Text = $"Model: {realTimeData.Model}";

            // Group Data by IP Address
            var groupedData = realTimeData.OutputData
                .GroupBy(d => d.IpAddress)
                .Select(g => new
                {
                    IpAddress = g.Key,
                    OutputData = g.ToList()
                })
                .ToList();

            // Flatten the grouped data for display
            _deviceOutputList.Clear();
            foreach (var group in groupedData)
            {
                _deviceOutputList.Add(new DeviceOutput
                {
                    IpAddress = group.IpAddress, // Store IP in row
                    IsGroupHeader = true         // Mark as header row
                });

                _deviceOutputList.AddRange(group.OutputData);
            }

            // Update DataGridView
            _bindingSource.DataSource = _deviceOutputList;
            _bindingSource.ResetBindings(false);

            // 🔥 Animate only changed cells
            AnimateTextUpdate(previousValues);
        }



        private void dataGrid_DeviceOutput_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = dataGrid_DeviceOutput.Rows[e.RowIndex].DataBoundItem as DeviceOutput;

            if (row != null)
            {
                if (row.IsGroupHeader)
                {
                    e.CellStyle.BackColor = Color.LightGray;  // Highlight header row
                    e.CellStyle.Font = new Font(dataGrid_DeviceOutput.Font, FontStyle.Bold);
                }
                else
                {
                    // Hide the IpAddress column if it's a non-header row
                    if (dataGrid_DeviceOutput.Columns[e.ColumnIndex].Name == "IpAddress")
                    {
                        e.Value = string.Empty; // Set the cell value to an empty string
                        e.FormattingApplied = true;
                    }
                }
            }
        }
        private async void AnimateTextUpdate(Dictionary<int, Dictionary<int, object>> previousValues)
        {
            for (int i = 0; i < dataGrid_DeviceOutput.Rows.Count; i++)
            {
                for (int j = 0; j < dataGrid_DeviceOutput.Columns.Count; j++)
                {
                    var cell = dataGrid_DeviceOutput.Rows[i].Cells[j];
                    object newValue = cell.Value;

                    if (previousValues.ContainsKey(i) && previousValues[i].ContainsKey(j))
                    {
                        object oldValue = previousValues[i][j];

                        if (oldValue == null || newValue == null || !oldValue.Equals(newValue))
                        {
                            // 🔥 Text has changed, apply animation
                            cell.Style.ForeColor = Color.Red;
                        }
                    }
                }
            }

            await Task.Delay(500); // Wait 0.5s

            for (int i = 0; i < dataGrid_DeviceOutput.Rows.Count; i++)
            {
                for (int j = 0; j < dataGrid_DeviceOutput.Columns.Count; j++)
                {
                    dataGrid_DeviceOutput.Rows[i].Cells[j].Style.ForeColor = Color.Black; // Reset color
                }
            }
        }




        public class RealTimeData
        {
            public int OrderID { get; set; }
            public string MasterWorkOrder { get; set; }
            public string SO { get; set; }
            public string Model { get; set; }
            public List<DeviceOutput> OutputData { get; set; }
        }

        public class WebSocketResponse
        {
            public string Action { get; set; }
            public string Status { get; set; }
            public RealTimeData RealTime { get; set; }
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!this.Visible)
            {
                _timer.Stop(); // Stop the timer when this control is no longer visible
            }
        }
        private void ucDeviceOutput_Leave(object sender, EventArgs e)
        {
            _timer.Stop();
        }

    }
}