using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;
using DigitalProduction.Validation;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucRegisterDevice : UserControl
    {
        public event EventHandler ExitClicked;
       // public event EventHandler<Device> DeviceCreated;
        private WebSocketClient _webSocketClient;

        public ucRegisterDevice()
        {
            InitializeComponent();
            loadDepartments();
            loadPlants();
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (webSocketClient == null)
            {
                ShowMessage.ShowError("WebSocketClient instance is null.");
                return;
            }

            // Assign the passed WebSocketClient instead of creating a new instance
            _webSocketClient = webSocketClient;

            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        private bool statusConnect = false;
        // Connect to WebSocket and check HMI device
        public async Task getConnectToHMIAdress(string ipAddress)
        {
            if (_webSocketClient == null)
            {
                ShowMessage.ShowError("WebSocket client is not initialized.");
            }

            var request = new { app = Global.App, action = "connectDevice", ipAddress };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void WebSocket_OnMessage(string jsonData)
        {
            statusConnect = false;
            try
            {
                var response = ResponseMessage<List<Device>>.FromJson(jsonData);
                if (response.Status == "connected")
                {
                    statusConnect = true;
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

        private void loadDepartments()
        {
            DataTable dt = DbHelper.getDepartments();
            cb_department.Properties.DataSource = dt;
            cb_department.Properties.DisplayMember = "DepartmentName";
            cb_department.Properties.ValueMember = "DepartmentID";
            cb_department.Properties.NullText = "Select a Department";
        }

        private void loadPlants()
        {
            DataTable dt = DbHelper.getPlants();
            cb_plant.Properties.DataSource = dt;
            cb_plant.Properties.DisplayMember = "PlantName";
            cb_plant.Properties.ValueMember = "PlantID";
            cb_plant.Properties.NullText = "Select a Plant";
        }

        private void btn_close_Click(object sender, System.EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private async void btn_submit_Click(object sender, EventArgs e)
        {
            if (!txt_addressIP.ValidateInput(ValidationType.NotEmptyString) ||
                !txt_deviceName.ValidateInput(ValidationType.NotEmptyString) ||
                 !cb_department.ValidateInput(ValidationType.NotNull) ||
                  !cb_plant.ValidateInput(ValidationType.NotNull))
            {
                MessageBox.Show("Invalid input! Please correct the highlighted fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int departmentID = cb_department.EditValue != null ? Convert.ToInt32(cb_department.EditValue) : 0;
            int plantId = cb_plant.EditValue != null ? Convert.ToInt32(cb_plant.EditValue) : 0;
            string ipAddress = txt_addressIP.Text.Trim();
            string machineName = txt_machineName.Text.Trim();

            await getConnectToHMIAdress(ipAddress);

            if (!statusConnect)
            {
                ShowMessage.ShowError($"Device at {ipAddress} is unreachable. Please check the network.");
                return;
            }

            bool statusAddDevice = DbHelper.dddNewDevice(departmentID, plantId, ipAddress, machineName);
            Console.WriteLine(statusAddDevice ? "Device added successfully!" : "Failed to add device (Duplicate IP or error).");

            if (statusAddDevice)
            {
                ShowMessage.ShowInfo($"Added new device: {ipAddress}!", "Success");
           //     DeviceCreated?.Invoke(this, new Device(ipAddress, machineName, departmentID, plantId));
            }
            else
            {
                ShowMessage.ShowError($"Failed to add new device: {ipAddress}!");
            }
        }
    }
}
