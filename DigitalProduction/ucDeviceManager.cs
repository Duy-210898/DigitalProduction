using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucDeviceManager : UserControl
    {
        private WebSocketClient _webSocketClient;
        private List<Device> currentDeviceStatuses = new List<Device>();
        private readonly DataTable deviceDataTable;
        private Panel groupPanelButtonContainer;
        private Button button;
        private ucRegisterDevice frmRegister;
        private Panel paginationPanel;
        private Label lblPageInfo;
        private DataGridView dgvDevices;
        private ComboBox departmentComboBox;

        public ucDeviceManager()
        {
            InitializeComponent();
            deviceDataTable = new DataTable();
            InitializeDataGridView();
            dgvDevices.DataSource = InitializeDeviceDataTable();
            CreateButtonContainer();

            // show add new device
            frmRegister = new ucRegisterDevice();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            frmRegister.DeviceCreated += RegisterForm_DeviceCreated;
        }

        private void InitializeDataGridView()
        {
            dgvDevices = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 50 }
            };

            this.Controls.Add(dgvDevices);
        }
        private void ApplyLocalization()
        {
            dgvDevices.Columns["Address"].HeaderText = LocalizationManager.GetString("Address");
            dgvDevices.Columns["Machine Name"].HeaderText = LocalizationManager.GetString("MachineName");
            dgvDevices.Columns["Plant Name"].HeaderText = LocalizationManager.GetString("PlantName");
            dgvDevices.Columns["Department Name"].HeaderText = LocalizationManager.GetString("DepartmentName");
            dgvDevices.Columns["ConnectionStatus"].HeaderText = LocalizationManager.GetString("Status");

            // ✅ Add Action Column if it does not exist
            if (!dgvDevices.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = "Action",
                    UseColumnTextForButtonValue = false  // ✅ Set to false to allow dynamic text change
                };
                dgvDevices.Columns.Add(actionColumn);
            }

            // ✅ Set every row to be read-only at the start
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                row.Cells["Address"].ReadOnly = true;
                row.Cells["Machine Name"].ReadOnly = true;
                row.Cells["Plant Name"].ReadOnly = true;
                row.Cells["Department Name"].ReadOnly = true;
                row.Cells["IsActive"].ReadOnly = true;
                row.Cells["ConnectionStatus"].ReadOnly = true;

                row.Cells["Action"].Value = "Edit";
            }

            dgvDevices.CellClick += DgvDevices_CellClick; // Attach event
        }

        private void DgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvDevices.Rows[e.RowIndex];

                // Check if the clicked cell is in the "Action" column
                if (dgvDevices.Columns[e.ColumnIndex].Name == "Action")
                {
                    if (row.Cells["Action"].Value.ToString() == "Edit")
                    {
                        // Enable Editing
                        row.Cells["Address"].ReadOnly = false;
                        row.Cells["Machine Name"].ReadOnly = false;
                        row.Cells["Plant Name"].ReadOnly = false;
                        row.Cells["Department Name"].ReadOnly = false;
                        row.Cells["IsActive"].ReadOnly = false;

                        // Change Background Color to Indicate Editable Mode
                        row.Cells["Address"].Style.BackColor = Color.LightYellow;
                        row.Cells["Machine Name"].Style.BackColor = Color.LightYellow;
                        row.Cells["Plant Name"].Style.BackColor = Color.LightYellow;
                        row.Cells["Department Name"].Style.BackColor = Color.LightYellow;
                        row.Cells["IsActive"].Style.BackColor = Color.LightYellow;

                        row.Cells["Action"].Value = "Update";  // Change button text to "Update"

                        // Display ComboBox for "Department Name"
                        departmentComboBox = new ComboBox
                        {
                            DataSource = new List<string> { "HR", "Engineering", "Sales", "Marketing" },  // Example department names
                            Location = row.Cells["Department Name"].ContentBounds.Location,
                            Size = row.Cells["Department Name"].ContentBounds.Size,
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        departmentComboBox.SelectedItem = row.Cells["Department Name"].Value.ToString();
                        departmentComboBox.Leave += (s, ev) => UpdateDepartmentName(row, departmentComboBox);

                        dgvDevices.Controls.Add(departmentComboBox);
                    }
                    else if (row.Cells["Action"].Value.ToString() == "Update")
                    {
                        // Get Updated Data from Row
                        string address = row.Cells["Address"].Value.ToString();
                        string machineName = row.Cells["Machine Name"].Value.ToString();
                        string plantName = row.Cells["Plant Name"].Value.ToString();
                        string departmentName = row.Cells["Department Name"].Value.ToString();
                        bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);
                        bool connectionStatus = Convert.ToBoolean(row.Cells["ConnectionStatus"].Value);

                        // Update Device
                        UpdateDevice(address, machineName, plantName, departmentName, isActive, connectionStatus);

                        // Make ReadOnly Again
                        row.Cells["Machine Name"].ReadOnly = true;
                        row.Cells["Plant Name"].ReadOnly = true;
                        row.Cells["Department Name"].ReadOnly = true;

                        // Reset Background Color
                        row.Cells["Machine Name"].Style.BackColor = Color.White;
                        row.Cells["Plant Name"].Style.BackColor = Color.White;
                        row.Cells["Department Name"].Style.BackColor = Color.White;

                        row.Cells["Action"].Value = "Edit";  // Change button text back to "Edit"
                    }
                }
            }
        }

        private void UpdateDepartmentName(DataGridViewRow row, ComboBox comboBox)
        {
            // Update the department name in the DataGridView cell
            row.Cells["Department Name"].Value = comboBox.SelectedItem.ToString();

            // Remove the ComboBox from the DataGridView controls
            dgvDevices.Controls.Remove(comboBox);
        }




        private void UpdateDevice(string address, string machineName, string plantName, string departmentName, bool isActive, bool connectionStatus)
        {
            // Example: Update data in DataTable
            foreach (DataRow row in deviceDataTable.Rows)
            {
                if (row["Address"].ToString() == address)
                {
                    row["Machine Name"] = machineName;
                    row["Plant Name"] = plantName;
                    row["Department Name"] = departmentName;
                    row["IsActive"] = isActive;
                    row["ConnectionStatus"] = connectionStatus;
                    break;
                }
            }

            dgvDevices.Refresh(); // Refresh UI after updating
        }



        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getDevices" };
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
                    ResponseMessage<List<Device>> response = ResponseMessage<List<Device>>.FromJson(jsonData);
                    if (response?.Devices != null)
                    {
                        PopulateDeviceDataTable(response.Devices);
                        CreatelabelTotalControls(response.Devices);
                        ApplyLocalization();
                    }
                    else
                    {
                        MessageBox.Show("No Data Found", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                MessageBox.Show($"JSON Deserialization Error: {jsonEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateDeviceDataTable(List<Device> devices)
        {
            deviceDataTable.Rows.Clear();
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
            dgvDevices.Refresh();
        }

        public DataTable InitializeDeviceDataTable()
        {
            deviceDataTable.Columns.Add("Address", typeof(string));
            deviceDataTable.Columns.Add("Machine Name", typeof(string));
            deviceDataTable.Columns.Add("Plant Name", typeof(string));
            deviceDataTable.Columns.Add("Department Name", typeof(string));
            deviceDataTable.Columns.Add("IsActive", typeof(bool));
            deviceDataTable.Columns.Add("ConnectionStatus", typeof(bool));

            // ✅ Prevent binding issues by setting ReadOnly = false
            foreach (DataColumn column in deviceDataTable.Columns)
            {
                column.ReadOnly = false;
            }

            return deviceDataTable;
        }


        private void RefreshButton_Click(object sender, EventArgs e)
        {
            _ = GetDataAndLoadToGridAsync();
        }

        private void CreateButtonContainer()
        {
            groupPanelButtonContainer = new Panel { Dock = DockStyle.Top, Height = 50 };
            Controls.Add(groupPanelButtonContainer);

            button = new Button { Text = "Add new device", Size = new Size(100, 40), Location = new Point(10, 5) };
            groupPanelButtonContainer.Controls.Add(button);
            button.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterDevice();
        }

        private void showRegisterDevice()
        {
            dgvDevices.Visible = false;
            frmRegister.Location = dgvDevices.Location;
            frmRegister.Size = dgvDevices.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
            _webSocketClient = WebSocketClient.Instance;
            frmRegister.SetWebSocketClient(_webSocketClient);
        }

        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            dgvDevices.Visible = true;
            frmRegister.Visible = false;
        }

        private void RegisterForm_DeviceCreated(object sender, Device newDevice)
        {
            deviceDataTable.Rows.Add(
                newDevice.IpAddress,
                newDevice.MachineName,
                newDevice.PlantName,
                newDevice.DepartmentName,
                newDevice.IsActive
            );
            dgvDevices.ClearSelection();
            if (dgvDevices.Rows.Count > 0)
                dgvDevices.Rows[0].Selected = true;
        }

        private void CreatelabelTotalControls(List<Device> devices)
        {
            if (paginationPanel != null)
            {
                this.Controls.Remove(paginationPanel);
                paginationPanel.Dispose();
            }

            paginationPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
            lblPageInfo = new Label
            {
                Text = $"Total Records: {devices.Count}",
                Size = new Size(200, 30),
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(20, 10)
            };

            Button refreshButton = new Button { Text = "Refresh", Size = new Size(80, 30), Location = new Point(250, 10) };
            refreshButton.Click += async (sender, e) => await GetDataAndLoadToGridAsync();

            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);
            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0);
        }
    }
}
