using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucUserManagement : XtraUserControl
    {
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterUser frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;
        private bool isEditing = false;

        private DataGridView dataGridView_UserManagement;

        public ucUserManagement()
        {
            InitializeComponent();
            LoadTextLable();
            InitializeDataGridView();
            CreateButtonContainer();

            // show add new user
            frmRegister = new ucRegisterUser();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            frmRegister.UserCreated += RegisterForm_UserCreated;
        }
        private void InitializeDataGridView()
        {
            dataGridView_UserManagement = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 50 }
            };

            this.Controls.Add(dataGridView_UserManagement);
        }
        private void ApplyLocalization()
        {
            // Hide sensitive data like Password and deparmentID
            dataGridView_UserManagement.Columns["Password"].Visible = false;
            dataGridView_UserManagement.Columns["DepartmentID"].Visible = false;
            dataGridView_UserManagement.Columns["OperatorID"].Visible = false;
            dataGridView_UserManagement.Columns["OperatorName"].Visible = false;
            dataGridView_UserManagement.Columns["PositionID"].Visible = false;
            dataGridView_UserManagement.Columns["CreatedAt"].Visible = false;
            dataGridView_UserManagement.Columns["UpdatedAt"].Visible = false;

            if (dataGridView_UserManagement.Columns.Contains("Username"))
                dataGridView_UserManagement.Columns["Username"].HeaderText = LocalizationManager.GetString("Username");
            dataGridView_UserManagement.Columns["EmployeeID"].HeaderText = LocalizationManager.GetString("EmployeeID");
            dataGridView_UserManagement.Columns["EmployeeName"].HeaderText = LocalizationManager.GetString("EmployeeName");
            dataGridView_UserManagement.Columns["IsActive"].HeaderText = LocalizationManager.GetString("IsActive");
            dataGridView_UserManagement.Columns["DepartmentName"].HeaderText = LocalizationManager.GetString("DepartmentName");
            dataGridView_UserManagement.Columns["PositionName"].HeaderText = LocalizationManager.GetString("PositionName");

            // ✅ Add Action Column if it does not exist
            if (!dataGridView_UserManagement.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = "Action",
                    UseColumnTextForButtonValue = false  // ✅ Set to false to allow dynamic text change
                };
                dataGridView_UserManagement.Columns.Add(actionColumn);
            }

            // ✅ Set every row to be read-only at the start
            foreach (DataGridViewRow row in dataGridView_UserManagement.Rows)
            {
                row.Cells["Username"].ReadOnly = true;
                row.Cells["EmployeeID"].ReadOnly = true;
                row.Cells["EmployeeName"].ReadOnly = true;
                row.Cells["IsActive"].ReadOnly = true;
                row.Cells["DepartmentName"].ReadOnly = true;
                row.Cells["PositionName"].ReadOnly = true;

                row.Cells["Action"].Value = "Edit";
            }
            dataGridView_UserManagement.Columns["Action"].HeaderText = LocalizationManager.GetString("Action");

            if (dataGridView_UserManagement != null)
            {
                dataGridView_UserManagement.CellClick -= dgvUserManagment_CellClick; // Unsubscribe if exists
                dataGridView_UserManagement.CellClick += dgvUserManagment_CellClick; // Subscribe
            }
        }
        private void dgvUserManagment_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || e.RowIndex >= dataGridView_UserManagement.Rows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= dataGridView_UserManagement.Columns.Count)
                return;

            DataGridViewRow row = dataGridView_UserManagement.Rows[e.RowIndex];

            if (dataGridView_UserManagement.Columns[e.ColumnIndex].Name == "Action")
            {
                string action = row.Cells["Action"].Value.ToString();

                if (action == "Edit")
                {
                    // Reset "Action" column for all other rows to prevent multiple edits
                    foreach (DataGridViewRow r in dataGridView_UserManagement.Rows)
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
            else if (dataGridView_UserManagement.Columns[e.ColumnIndex].Name == "CancelAction")
            {
                if (row.Cells["Action"].Value != null && row.Cells["Action"].Value.ToString() == "Update")
                {
                    isEditing = false;
                    HandleCancelAction(row);
                }
            }
        }
        private void HandleUpdateAction(DataGridViewRow row)
        {
            string username = row.Cells["Username"].Value?.ToString();
            int newEmployeeID = Convert.ToInt32(row.Cells["EmployeeID"].Value);
            int newDepartmentID = Convert.ToInt32(row.Cells["DepartmentCombo"].Value);
            int newPositionID = Convert.ToInt32(row.Cells["PositionCombo"].Value);
            string newEmployeeName = row.Cells["EmployeeName"].Value.ToString();
            bool newIsActive = Convert.ToBoolean(row.Cells["IsActive"].Value);

            // Call your update function
            bool status = DbHelper.updateUser(username, newEmployeeName, newEmployeeID, newDepartmentID, newPositionID, newIsActive);
            string message = status ? "Updated user : " + username : "Cannot update user : " + username;
            if (status)
            {
                RemoveColumnIfExists("DepartmentCombo");
                RemoveColumnIfExists("PositionCombo");
                SetColumnVisibility(true, "DepartmentName", "PositionName");
                string departmentName = Extentions.getNameFromDataTable(DbHelper.getDepartments(), newDepartmentID, "departmentID", "departmentName");
                string positionName = Extentions.getNameFromDataTable(DbHelper.getPositions(), newPositionID, "PositionID", "PositionName");
                UpdateDepartmentAndPositionName(row.Index, "Department", departmentName);
                UpdateDepartmentAndPositionName(row.Index, "Position", positionName);
            }
            ShowMessage.ShowInfo(message, status ? "Success" : "Fail");
            SetRowEditable(row, false);
            row.Cells["Action"].Value = "Edit";

            // Clear Cancel button text
            RemoveColumnIfExists("CancelAction");
            isEditing = false;
        }

        private void HandleEditAction(DataGridViewRow row)
        {
            try
            {
                var departments = GetDepartments();
                var positions = GetPositions();

                if (departments == null || positions == null)
                    return;

                // Store the original values for cancellation
                row.Tag = new { DepartmentName = row.Cells["DepartmentName"].Value, PositionName = row.Cells["PositionName"].Value };

                // Create and insert ComboBox columns
                CreateComboBoxColumn("DepartmentCombo", "Department Name", departments, "DepartmentName", "DepartmentID");
                CreateComboBoxColumn("PositionCombo", "Position Name", positions, "PositionName", "PositionID");

                // Set initial values for ComboBox cells
                SetComboBoxInitialValues(row, departments, positions);

                // Hide original columns
                SetColumnVisibility(false, "DepartmentName", "PositionName");

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
        private void HandleCancelAction(DataGridViewRow row)
        {
            var originalValues = (dynamic)row.Tag;
            if (originalValues != null)
            {
                // Revert changes
                row.Cells["DepartmentName"].Value = originalValues.DepartmentName;
                row.Cells["PositionName"].Value = originalValues.PositionName;

                // Show original columns again
                SetColumnVisibility(true, "DepartmentName", "PositionName");

                SetRowEditable(row, false);
                row.Cells["Action"].Value = "Edit";

                // Remove Cancel button and ComboBox columns
                RemoveColumnIfExists("CancelAction");
                RemoveColumnIfExists("DepartmentCombo");
                RemoveColumnIfExists("PositionCombo");
            }
        }
        private void CreatelabelTotalControls()
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
                Height = 50,
                Padding = new Padding(10)
            };

            lblPageInfo = new LabelControl()
            {
                Text = $"Total Records: {employees.Count}",
                Size = new Size(200, 30),
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(20, 10)
            };

            paginationPanel.Controls.Add(lblPageInfo);
            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0);
        }

        private void RegisterForm_UserCreated(object sender, Employee newUser)
        {
            employees.Insert(0, newUser); // Refresh DataGridView
            LoadDataGridView();
        }

        private void LoadDataGridView()
        {
            // Store the state of existing columns
            var columnState = dataGridView_UserManagement.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => new
                {
                    c.Name,
                    c.Visible,
                    c.DisplayIndex
                })
                .ToList();

            // Reset the data source
            dataGridView_UserManagement.DataSource = null;
            dataGridView_UserManagement.DataSource = employees.ToList(); // Bind to updated list

            // Ensure Action column exists and is set correctly
            if (!dataGridView_UserManagement.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = LocalizationManager.GetString("Action"),
                    UseColumnTextForButtonValue = false // Allows for dynamic text
                };
                dataGridView_UserManagement.Columns.Add(actionColumn);
            }

            // Restore column state after resetting the data source
            foreach (var column in columnState)
            {
                if (dataGridView_UserManagement.Columns.Contains(column.Name))
                {
                    dataGridView_UserManagement.Columns[column.Name].Visible = column.Visible;
                    dataGridView_UserManagement.Columns[column.Name].DisplayIndex = column.DisplayIndex;
                }
            }
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getUsers" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                ResponseMessage<List<Employee>> response = ResponseMessage<List<Employee>>.FromJson(jsonData);

                if (response?.Users != null && response.Users.Count > 0)
                {
                    employees.Clear();

                    foreach (var employee in response.Users)
                    {
                        employees.Add(employee);
                    }

                    CreatelabelTotalControls(); // Create/update pagination info
                    LoadDataGridView(); // Load data into the DataGridView
                    ApplyLocalization();
                }
                else
                {
                    MessageBox.Show("No Data Found");
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                MessageBox.Show($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Create button container and other existing methods remain unchanged...

        private void CreateButtonContainer()
        {
            // Create a PanelControl to hold the button
            groupPanelButtonContainer = new PanelControl()
            {
                Dock = DockStyle.Top,
                Height = 50 // Adjust the height to suit your layout
            };

            Controls.Add(groupPanelButtonContainer);
            button = new SimpleButton()
            {
                Text = "Create user",
                Size = new Size(100, 40)
            };

            groupPanelButtonContainer.Controls.Add(button);
            button.Click += Button_Click;
            button.Location = new Point(10, 5);
            // Sync Data button
            SimpleButton syncButton = new SimpleButton()
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(100, 40)
            };
            syncButton.ImageOptions.Image = Properties.Resources.sync_icon;
            syncButton.Click += SyncButton_Click; // Event for syncing data
            groupPanelButtonContainer.Controls.Add(syncButton);
            syncButton.Location = new Point(120, 5); // Adjust the location accordingly
        }
        private async void SyncButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isEditing) // Prevent refresh while editing
                {
                    // Disable the button to prevent multiple clicks during sync
                    ((SimpleButton)sender).Enabled = false;
                    await GetDataAndLoadToGridAsync(); // Fetch and load data from the server
                }
                else
                {
                    MessageBox.Show("Finish editing before refreshing.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during data sync: {ex.Message}", "Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ((SimpleButton)sender).Enabled = true; // Re-enable the button after the operation
            }
        }
        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterUser();
        }

        private void showRegisterUser()
        {
            // Hide the DataGridView
            dataGridView_UserManagement.Visible = false;
            frmRegister.Location = dataGridView_UserManagement.Location;
            frmRegister.Size = dataGridView_UserManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }

        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the DataGridView
            dataGridView_UserManagement.Visible = true;
            frmRegister.Visible = false;
        }

        private void LoadTextLable()
        {
            // Assuming this is where you load the grid's header labels, adjust accordingly 
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
            row.Cells["UserName"].ReadOnly = true;
            row.Cells["UserName"].Style.BackColor = Color.White;
        }
        private void AddCancelButton(DataGridViewRow row)
        {
            if (!dataGridView_UserManagement.Columns.Contains("CancelAction"))
            {
                DataGridViewButtonColumn cancelColumn = new DataGridViewButtonColumn
                {
                    Name = "CancelAction",
                    HeaderText = "Cancel",
                    Text = "Cancel",
                    UseColumnTextForButtonValue = true
                };
                dataGridView_UserManagement.Columns.Add(cancelColumn);
            }
            row.Cells["CancelAction"].Value = "Cancel";
        }
        private void SetColumnVisibility(bool isVisible, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (dataGridView_UserManagement.Columns.Contains(name))
                {
                    dataGridView_UserManagement.Columns[name].Visible = isVisible;
                }
            }
        }
        private void SetComboBoxInitialValues(DataGridViewRow row, List<Department> departments, List<Position> positions)
        {
            foreach (DataGridViewRow dgvRow in dataGridView_UserManagement.Rows)
            {
                string deptName = dgvRow.Cells["DepartmentName"].Value?.ToString();
                string plantName = dgvRow.Cells["PositionName"].Value?.ToString();

                int deptID = departments.FirstOrDefault(d => string.Equals(d.DepartmentName, deptName, StringComparison.OrdinalIgnoreCase))?.DepartmentID ?? departments.First().DepartmentID;
                dgvRow.Cells["DepartmentCombo"].Value = deptID;

                int positionID = positions.FirstOrDefault(p => string.Equals(p.PositionName, plantName, StringComparison.OrdinalIgnoreCase))?.PositionID ?? positions.First().PositionID;
                dgvRow.Cells["PositionCombo"].Value = positionID;
            }
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
            if (!dataGridView_UserManagement.Columns.Contains(columnName))
            {
                int insertIndex = columnName == "DepartmentCombo" ? dataGridView_UserManagement.Columns["DepartmentName"].Index : dataGridView_UserManagement.Columns["PositionName"].Index;
                dataGridView_UserManagement.Columns.Insert(insertIndex, comboBoxColumn);
            }
        }
        private void RemoveColumnIfExists(string columnName)
        {
            if (dataGridView_UserManagement.Columns.Contains(columnName))
            {
                dataGridView_UserManagement.Columns.Remove(columnName);
            }
        }
        private void UpdateDepartmentAndPositionName(int rowIndex, string key, string newName)
        {
            if (rowIndex >= 0 && rowIndex < dataGridView_UserManagement.Rows.Count)
            {
                // Update the hidden column
                dataGridView_UserManagement.Rows[rowIndex].Cells[$"{key}Name"].Value = newName;

                // Refresh to reflect changes
                dataGridView_UserManagement.Refresh();
            }
        }
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

        private List<Position> GetPositions()
        {
            DataTable dt = DbHelper.getPositions();
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Error: No plants found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return dt.AsEnumerable().Select(s => new Position
            {
                PositionID = s.Field<int>("PositionID"),
                PositionName = s.Field<string>("PositionName")
            }).ToList();
        }
        public class Department
        {
            public int DepartmentID { get; set; }
            public string DepartmentName { get; set; }
        }
        public class Position
        {
            public int PositionID { get; set; }
            public string PositionName { get; set; }
        }
    }
}