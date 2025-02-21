using System;
using System.Collections.Generic;
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
        private BindingSource _bindingSource = new BindingSource(); // Binding source for DataGridView

        public ucDeviceOutput()
        {
            InitializeComponent();

            // Initialize DataGridView
            dataGrid_DeviceOutput.DataSource = _bindingSource;
            dataGrid_DeviceOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_DeviceOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Handle resize event
            this.Resize += new EventHandler(UcDeviceOutput_Resize);
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
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getActualData"};
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<WebSocketResponse>(jsonData);

                if (response?.RealTime?.OutputData != null && response.RealTime.OutputData.Count > 0)
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
                    MessageBox.Show("No Data Found", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateUI(RealTimeData realTimeData)
        {
            // Update Labels
            lblOrder.Text = $"Order: {realTimeData.OrderID}";
            lblMasterWorkOrder.Text = $"MasterWorkOrder: {realTimeData.MasterWorkOrder}";
            lblSO.Text = $"SO: {realTimeData.SO}";
            lblModel.Text = $"Model: {realTimeData.Model}";

            // Update DataGridView
            _deviceOutputList.Clear();
            _deviceOutputList.AddRange(realTimeData.OutputData);
            _bindingSource.DataSource = _deviceOutputList;
            _bindingSource.ResetBindings(false);
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
    }
}
