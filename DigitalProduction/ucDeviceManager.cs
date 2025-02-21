using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucDeviceManager : XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        private List<Device> currentDeviceStatuses = new List<Device>();
        private readonly DataTable deviceDataTable;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterDevice frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;

        public ucDeviceManager()
        {
            InitializeComponent();
            deviceDataTable = new DataTable();
           // gridView_Device.ShowFindPanel();
            gridView_Device.OptionsFind.ShowFindButton = false;
            gridControl_Devices.DataSource = InitializeDeviceDataTable();
            gridView_Device.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            gridView_Device.RowHeight = 50;
            gridView_Device.BestFitColumns();
            CreateButtonContainer();

            // show add new device
            frmRegister = new ucRegisterDevice();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            frmRegister.DeviceCreated += RegisterForm_DeviceCreated;
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
            var request = new {app = Global.App, action = "getDevices" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                var item = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                string action = item.ContainsKey("action") ? item["action"].ToString() : null;

                if (action.Equals("getDevices"))
                {
                    // Assuming the response
                    ResponseMessage<List<Device>> response = ResponseMessage<List<Device>>.FromJson(jsonData);

                    // Check if there are devices in the response
                    if (response?.Devices != null)
                    {
                        gridView_Device.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                        Extentions.showEditModeCellGridView(gridControl_Devices, gridView_Device, "ucDevice");
                        PopulateDeviceDataTable(response.Devices);
                        CreatelabelTotalControls(response.Devices);
                        ApplyLocalization();
                    }
                    else
                    {
                        ShowMessage.ShowInfo("No Data Found");
                    }
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
                deviceDataTable.Rows.Add(
                    device.IpAddress,
                    device.MachineName,
                    device.PlantName,
                    device.DepartmentName,
                    device.IsActive,
                    device.ConnectionStatus
                );
            }

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
            deviceDataTable.Columns.Add("Department Name", typeof(string));
            deviceDataTable.Columns.Add("IsActive", typeof(bool));
            deviceDataTable.Columns.Add("ConnectionStatus", typeof(bool));
            return deviceDataTable;
        }


        private void ApplyLocalization()
        {
            gridView_Device.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");
            gridView_Device.GroupPanelText = LocalizationManager.GetString("ListOfDevice");

            // grid view
            gridView_Device.Columns["Address"].Caption = LocalizationManager.GetString("Address");
            gridView_Device.Columns["Machine Name"].Caption = LocalizationManager.GetString("MachineName");
            gridView_Device.Columns["Plant Name"].Caption = LocalizationManager.GetString("PlantName");
            gridView_Device.Columns["Department Name"].Caption = LocalizationManager.GetString("DepartmentName");
            gridView_Device.Columns["ConnectionStatus"].Caption = LocalizationManager.GetString("Status");
            gridView_Device.Columns["Action"].Caption = LocalizationManager.GetString("Action");
        }
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            _ = GetDataAndLoadToGridAsync();
        }

        private void CreateButtonContainer()
        {
            // Create a PanelControl to hold the button
            groupPanelButtonContainer = new PanelControl()
            {
                Dock = DockStyle.Top,
                Height = 50
            };

            // Add the PanelControl to the form
            Controls.Add(groupPanelButtonContainer);

            // Create the button
            button = new SimpleButton()
            {
                Text = "Add new device",
                Size = new System.Drawing.Size(100, 40)
            };

            // Add the button to the PanelControl
            groupPanelButtonContainer.Controls.Add(button);

            // Handle the button click event
            button.Click += Button_Click;

            // Position the button inside the PanelControl (optional)
            button.Location = new System.Drawing.Point(10, 5); // Adjust location as needed
        }
        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterDevice();
        }
        private void showRegisterDevice()
        {
            // Hide the GridView
            gridControl_Devices.Visible = false;

            // Show the user control
            frmRegister.Location = gridControl_Devices.Location;
            frmRegister.Size = gridControl_Devices.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
            _webSocketClient = WebSocketClient.Instance;
            frmRegister.SetWebSocketClient(_webSocketClient);
        }
        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the GridView
            gridControl_Devices.Visible = true;

            // Hide the user control
            frmRegister.Visible = false;
        }
        private void RegisterForm_DeviceCreated(object sender, Device newDevice)
        {
            // GridView will automatically refresh
            deviceDataTable.Rows.Add(
                   newDevice.IpAddress,
                   newDevice.MachineName,
                   newDevice.PlantName,
                   newDevice.DepartmentName,
                   newDevice.IsActive
               );
            gridView_Device.FocusedRowHandle = 0;
        }
        private void CreatelabelTotalControls(List<Device> devices)
        {
            // Remove any existing panel to prevent duplication
            if (paginationPanel != null)
            {
                this.Controls.Remove(paginationPanel);
                paginationPanel.Dispose();
            }

            // Create a new PanelControl for pagination at the bottom
            paginationPanel = new PanelControl()
            {
                Dock = DockStyle.Bottom,
                Height = 50, // Adjust height
                Padding = new Padding(10)
            };

            // Create Label for page info (Total Records)
            lblPageInfo = new LabelControl()
            {
                Text = $"Total Records: {devices.Count}",
                Size = new Size(200, 30),
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(20, 10)
            };
            //lblPageInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            // Create a "Refresh" Button
            SimpleButton refreshButton = new SimpleButton()
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(250, 10) // Adjust positioning
            };
            // Style the Refresh Button
            refreshButton.Appearance.BackColor = Color.LightBlue; // Change background color
            refreshButton.Appearance.Font = new Font("Arial", 9f, FontStyle.Bold);
            refreshButton.Appearance.Options.UseBackColor = true;

            // Add click event to refresh the data
            refreshButton.Click += async (sender, e) =>
            {
                // Disable the button while loading
                refreshButton.Enabled = false;
                refreshButton.Text = "Loading..."; // Provide user feedback

                await GetDataAndLoadToGridAsync(); // Load the data

                // Re-enable the button after data is loaded
                refreshButton.Enabled = true;
                refreshButton.Text = "Refresh"; // Reset button text
            };

            // Add components to the panel
            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);

            // Add the panel to the form (make sure it's added correctly)
            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0); // Ensures it appears at the bottom
        }
    }
}