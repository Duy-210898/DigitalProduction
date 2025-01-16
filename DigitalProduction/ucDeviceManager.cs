using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class ucDeviceManager : XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        private List<Device> currentDeviceStatuses = new List<Device>();
        private readonly DataTable deviceDataTable;


        public ucDeviceManager()
        {
            InitializeComponent();
            LanguageSettings.LanguageChanged += OnLanguageChanged;
            deviceDataTable = new DataTable();
            gridView_Device.ShowFindPanel();
            gridView_Device.OptionsFind.ShowFindButton = false;
            gridControl_Devices.DataSource = InitializeDeviceDataTable();
            gridView_Device.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            gridView_Device.BestFitColumns();
        }
        private void gridView_CustomDrawGroupPanel(object sender, CustomDrawEventArgs e)
        {
            // Set the alignment of the GroupPanelText
            e.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            e.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            // Optional: You can also change the font and color if necessary
            e.Appearance.Font = new Font("Tahoma", 13, FontStyle.Bold);
            e.Appearance.ForeColor = Color.Blue;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }
        // Send request to WebSocket or API and load data
        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { action = "getDevices" };  
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                // Assuming the response
                ResponseMessage<List<Device>> response = ResponseMessage<List<Device>>.FromJson(jsonData);

                // Check if there are devices in the response
                if (response?.Devices != null)
                {
                    PopulateDeviceDataTable(response.Devices);
                }
                else
                {
                    ShowMessage.ShowInfo("No Data Found");
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

        // Populate the DataTable with device data
        private void PopulateDeviceDataTable(List<Device> devices)
        {
            // Clear existing data
            deviceDataTable.Rows.Clear();

            // Add rows to the DataTable
            foreach (var device in devices)
            {
                string de = device.Plant;
                deviceDataTable.Rows.Add(
                    device.IpAddress,
                    device.MachineName,
                    device.Plant,
                    device.ConnectionStatus
                );
            }

            gridView_Device.EditFormPrepared += Extentions.GridView_EditFormPrepared;
            Extentions.showEditModeCellGridView(gridControl_Devices, gridView_Device, "Machine Name");

            // Ensure localization is applied after data is refreshed
            ApplyLocalization();

            // Apply custom styles to column headers
            gridView_Device.Appearance.HeaderPanel.BackColor = Color.LightSteelBlue;
            gridView_Device.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView_Device.Appearance.HeaderPanel.Font = new Font("Arial", 10, FontStyle.Bold);

            // Refresh the GridControl to show the updated data
            gridControl_Devices.RefreshDataSource();
            gridControl_Devices.Refresh();
        }
        public DataTable InitializeDeviceDataTable()
        {
            deviceDataTable.Columns.Add("Address", typeof(string));
            deviceDataTable.Columns.Add("Machine Name", typeof(string));
            deviceDataTable.Columns.Add("Plant Name", typeof(string));
            deviceDataTable.Columns.Add("ConnectionStatus", typeof(bool));
            return deviceDataTable;
        }

        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }

        private void OnLanguageChanged()
        {
            ApplyLocalization();
        }
        private void ApplyLocalization() {
            gridView_Device.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");
            gridView_Device.GroupPanelText = LocalizationManager.GetString("ListOfDevice");

            // grid view
            gridView_Device.Columns["Address"].Caption = LocalizationManager.GetString("Address");
            gridView_Device.Columns["Machine Name"].Caption = LocalizationManager.GetString("MachineName");
            gridView_Device.Columns["Plant Name"].Caption = LocalizationManager.GetString("PlantName");
            gridView_Device.Columns["ConnectionStatus"].Caption = LocalizationManager.GetString("ConnectionStatus");
            gridView_Device.Columns["Action"].Caption = LocalizationManager.GetString("Action");
        }
    }
}