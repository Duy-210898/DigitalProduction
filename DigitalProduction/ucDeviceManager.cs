using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Extensions;
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

        private bool isEditing = false;

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
           // frmRegister.DeviceCreated += RegisterForm_DeviceCreated;
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
            if (dgvDevices.Columns.Contains("Address"))
                dgvDevices.Columns["Address"].HeaderText = LocalizationManager.GetString("Address");
            dgvDevices.Columns["Machine Name"].HeaderText = LocalizationManager.GetString("MachineName"); 
            dgvDevices.Columns["Plant Name"].HeaderText = LocalizationManager.GetString("PlantName");
            dgvDevices.Columns["Department Name"].HeaderText = LocalizationManager.GetString("DepartmentName");
            dgvDevices.Columns["ConnectionStatus"].HeaderText = LocalizationManager.GetString("Status");
            dgvDevices.Columns["DeviceID"].Visible = false;


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

            if (dgvDevices != null)
            {
                dgvDevices.CellClick += dgvDevices_CellClick; // Unsubscribe if exists
                dgvDevices.CellClick += dgvDevices_CellClick; // Subscribe
            }
        }


        private void dgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvDevices.Rows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= dgvDevices.Columns.Count)
                return;

            DataGridViewRow row = dgvDevices.Rows[e.RowIndex];

            if (dgvDevices.Columns[e.ColumnIndex].Name == "Action")
            {
                string action = row.Cells["Action"].Value.ToString();


                if (action == "Edit")
                {
                    // Reset "Action" column for all other rows to prevent multiple edits
                    foreach (DataGridViewRow r in dgvDevices.Rows)
                    {
                        if (r.Index != e.RowIndex)
                        {
                            r.Cells["Action"].Value = "Edit";
                            SetRowEditable(r, false);
                        }
                    }
                    isEditing = true;
                    HandleEditAction(row);
                }
                else if (action == "Update")
                {
                    HandleUpdateAction(row);
                }
            }
            else if (dgvDevices.Columns[e.ColumnIndex].Name == "CancelAction")
            {
                if (row.Cells["Action"].Value != null && row.Cells["Action"].Value.ToString() == "Update")
                {
                    isEditing = false;
                    HandleCancelAction(row);
                }
            }
        }

        private void HandleEditAction(DataGridViewRow row)
        {
            try
            {
                var departments = GetDepartments();
                var plants = GetPlants();

                if (departments == null || plants == null)
                    return;

                // Store the original values for cancellation
                row.Tag = new { DepartmentName = row.Cells["Department Name"].Value, PlantName = row.Cells["Plant Name"].Value };

                // Create and insert ComboBox columns
                CreateComboBoxColumn("DepartmentCombo", "Department Name", departments, "DepartmentName", "DepartmentID");
                CreateComboBoxColumn("PlantCombo", "Plant Name", plants, "PlantName", "PlantID");

                // Set initial values for ComboBox cells
                SetComboBoxInitialValues(row, departments, plants);

                // Hide original columns
                SetColumnVisibility(false, "Department Name", "Plant Name");

                // Change button text to "Update"
                row.Cells["Action"].Value = "Update";

                // Add Cancel button
                AddCancelButton(row);
                SetRowEditable(row, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleUpdateAction(DataGridViewRow row)
        {
            int deviceId = Convert.ToInt32(row.Cells["DeviceID"].Value);
            int plantId = Convert.ToInt32(row.Cells["PlantCombo"].Value);
            int departmentId = Convert.ToInt32(row.Cells["DepartmentCombo"].Value);
            string address = row.Cells["Address"].Value?.ToString();
            string machineName = row.Cells["Machine Name"].Value?.ToString();
            bool isActive = Convert.ToBoolean(row.Cells["IsActive"].Value);
            bool connectionStatus = Convert.ToBoolean(row.Cells["ConnectionStatus"].Value);

            bool status = DbHelper.updateDevice(deviceId, departmentId, plantId, address, machineName, isActive, connectionStatus);
            string message = status ? "Updated device at: " + address : "Cannot update device at: " + address;
            if (status) {
                RemoveColumnIfExists("DepartmentCombo");
                RemoveColumnIfExists("PlantCombo");
                SetColumnVisibility(true, "Department Name", "Plant Name");
                string departmentName = Extentions.getNameFromDataTable(DbHelper.getDepartments(), departmentId, "departmentID", "departmentName");
                string plantName = Extentions.getNameFromDataTable(DbHelper.getPlants(), plantId, "plantID", "plantName");
                UpdateDepartmentAndPlantName(row.Index, "Department", departmentName);
                UpdateDepartmentAndPlantName(row.Index, "Plant", plantName);
            }
            ShowMessage.ShowInfo(message, status ? "Success" : "Fail");
            SetRowEditable(row, false);
            row.Cells["Action"].Value = "Edit";

            // Clear Cancel button text
            RemoveColumnIfExists("CancelAction");
            isEditing = false;
        }

        private void HandleCancelAction(DataGridViewRow row)
        {
            var originalValues = (dynamic)row.Tag;
            if (originalValues != null)
            {
                // Revert changes
                row.Cells["Department Name"].Value = originalValues.DepartmentName;
                row.Cells["Plant Name"].Value = originalValues.PlantName;

                // Show original columns again
                SetColumnVisibility(true, "Department Name", "Plant Name");

                SetRowEditable(row, false);
                row.Cells["Action"].Value = "Edit";

                // Remove Cancel button and ComboBox columns
                RemoveColumnIfExists("CancelAction");
                RemoveColumnIfExists("DepartmentCombo");
                RemoveColumnIfExists("PlantCombo");
            }
        }

        // Helper methods
        private List<Department> GetDepartments()
        {
            DataTable dt = DbHelper.getDepartments();
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Error: No departments found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return dt.AsEnumerable().Select(s => new Department
            {
                DepartmentID = s.Field<int>("DepartmentID"),
                DepartmentName = s.Field<string>("DepartmentName")
            }).ToList();
        }

        private List<Plant> GetPlants()
        {
            DataTable dt = DbHelper.getPlants();
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Error: No plants found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return dt.AsEnumerable().Select(s => new Plant
            {
                PlantID = s.Field<int>("PlantID"),
                PlantName = s.Field<string>("PlantName")
            }).ToList();
        }

        private void CreateComboBoxColumn<T>(string columnName, string headerText, List<T> dataSource, string displayMember, string valueMember)
        {
            var comboBoxColumn = new DataGridViewComboBoxColumn
            {
                Name = columnName,
                HeaderText = headerText,
                DataSource = dataSource,
                DisplayMember = displayMember,
                ValueMember = valueMember,
                FlatStyle = FlatStyle.Flat,
                DropDownWidth = 160,
                Width = 130
            };

            // Insert the new ComboBox column only if it doesn't already exist
            if (!dgvDevices.Columns.Contains(columnName))
            {
                int insertIndex = columnName == "DepartmentCombo" ? dgvDevices.Columns["Department Name"].Index : dgvDevices.Columns["Plant Name"].Index;
                dgvDevices.Columns.Insert(insertIndex, comboBoxColumn);
            }
        }

        private void SetComboBoxInitialValues(DataGridViewRow row, List<Department> departments, List<Plant> plants)
        {
            foreach (DataGridViewRow dgvRow in dgvDevices.Rows)
            {
                string deptName = dgvRow.Cells["Department Name"].Value?.ToString();
                string plantName = dgvRow.Cells["Plant Name"].Value?.ToString();

                int deptID = departments.FirstOrDefault(d => string.Equals(d.DepartmentName, deptName, StringComparison.OrdinalIgnoreCase))?.DepartmentID ?? departments.First().DepartmentID;
                dgvRow.Cells["DepartmentCombo"].Value = deptID;

                int plantID = plants.FirstOrDefault(p => string.Equals(p.PlantName, plantName, StringComparison.OrdinalIgnoreCase))?.PlantID ?? plants.First().PlantID;
                dgvRow.Cells["PlantCombo"].Value = plantID;
            }
        }

        private void SetColumnVisibility(bool isVisible, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (dgvDevices.Columns.Contains(name))
                {
                    dgvDevices.Columns[name].Visible = isVisible;
                }
            }
        }

        private void AddCancelButton(DataGridViewRow row)
        {
            if (!dgvDevices.Columns.Contains("CancelAction"))
            {
                DataGridViewButtonColumn cancelColumn = new DataGridViewButtonColumn
                {
                    Name = "CancelAction",
                    HeaderText = "Cancel",
                    Text = "Cancel",
                    UseColumnTextForButtonValue = true
                };
                dgvDevices.Columns.Add(cancelColumn);
            }
            row.Cells["CancelAction"].Value = "Cancel";
        }
        private void UpdateDepartmentAndPlantName(int rowIndex, string key, string newName)
        {
            if (rowIndex >= 0 && rowIndex < dgvDevices.Rows.Count)
            {
                // Update the hidden column
                dgvDevices.Rows[rowIndex].Cells[$"{key} Name"].Value = newName;

                // Refresh to reflect changes
                dgvDevices.Refresh();
            }
        }
        private void RemoveColumnIfExists(string columnName)
        {
            if (dgvDevices.Columns.Contains(columnName))
            {
                dgvDevices.Columns.Remove(columnName);
            }
        }

        private void SetRowEditable(DataGridViewRow row, bool isEditable)
        {
            Color backColor = isEditable ? Color.LightYellow : Color.White;
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.OwningColumn.Name != "Action")
                {
                    cell.ReadOnly = !isEditable;
                    cell.Style.BackColor = backColor;
                }
            }
            row.Cells["ConnectionStatus"].ReadOnly = true;
            row.Cells["ConnectionStatus"].Style.BackColor = Color.White;
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
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage; // Unsubscribe previous instance
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            _ = GetDataAndLoadToGridAsync();
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getDevices" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            if (IsDisposed || !IsHandleCreated) return; // Ensure control is still valid

            try
            {
                var item = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                string action = item.ContainsKey("action") ? item["action"].ToString() : null;

                if (action?.Equals("getDevices") == true)
                {
                    ResponseMessage<List<Device>> response = ResponseMessage<List<Device>>.FromJson(jsonData);

                    if (response?.Devices != null)
                    {
                        // Ensure UI updates happen on the main thread
                        SafeInvoke(() =>
                        {
                            PopulateDeviceDataTable(response.Devices);
                            CreatelabelTotalControls(response.Devices);
                            ApplyLocalization();
                        });
                    }
                    else
                    {
                        SafeInvoke(() => ShowMessageBox("No Data Found", "Info", MessageBoxIcon.Information));
                    }
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                SafeInvoke(() => ShowMessageBox($"JSON Deserialization Error: {jsonEx.Message}", "Error", MessageBoxIcon.Error));
            }
            catch (Exception ex)
            {
                SafeInvoke(() => ShowMessageBox($"An error occurred: {ex.Message}", "Error", MessageBoxIcon.Error));
            }
        }

        // Helper method to safely invoke UI updates
        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    Invoke(action);
                }
                catch (ObjectDisposedException) { } // Handle case where form is already disposed
            }
            else
            {
                action();
            }
        }

        // Helper method to show message boxes safely
        private void ShowMessageBox(string message, string title, MessageBoxIcon icon)
        {
            SafeInvoke(() => MessageBox.Show(message, title, MessageBoxButtons.OK, icon));
        }
      

        private void PopulateDeviceDataTable(List<Device> devices)
        {
            deviceDataTable.Rows.Clear();
            foreach (var device in devices)
            {
                deviceDataTable.Rows.Add(
                    device.DeviceID,
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
            deviceDataTable.Columns.Add("DeviceID", typeof(int));
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


            SimpleButton syncButton = new SimpleButton()
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(100, 40)
            };
            syncButton.Click += SyncButton_Click;
            syncButton.ImageOptions.Image = Properties.Resources.sync_icon;

            paginationPanel.Controls.Add(lblPageInfo);
            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0);
            groupPanelButtonContainer.Controls.Add(syncButton);
            syncButton.Location = new Point(120, 5); // Adjust the location accordingly
        }
        private async void SyncButton_Click(object sender, EventArgs e)
        {
            if (!isEditing) // Prevent refresh while editing
            {
                await GetDataAndLoadToGridAsync();
            }
            else
            {
                MessageBox.Show("Finish editing before refreshing.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public class Department
        {
            public int DepartmentID { get; set; }
            public string DepartmentName { get; set; }
        }
        public class Plant
        {
            public int PlantID { get; set; }
            public string PlantName { get; set; }
        }
    }
}
