using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucOperatorManagement : UserControl
    {
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private ucRegisterOperator frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;
        private DataGridView dataGridView_OperatorManagement;
        private bool isEditing = false;
        private TextBox txtOperatorId;
        private Label lblResult;

        public ucOperatorManagement()
        {
            InitializeComponent();
            LoadTextLable();
            InitializeDataGridView(); // Initialize the DataGridView
            CreatelabelTotalControls();
            CreateButtonContainer();
            frmRegister = new ucRegisterOperator();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
           // frmRegister.UserCreated += RegisterForm_UserCreated;
        }

        private void InitializeDataGridView()
        {
            dataGridView_OperatorManagement = new DataGridView
            {
                Dock = DockStyle.Fill, // Fill the remaining space in the parent control
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 50 }
            };

            this.Controls.Add(dataGridView_OperatorManagement); // Add DataGridView first
        }
        private void MainForm_Resize(object sender, EventArgs e)
        {
            dataGridView_OperatorManagement.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - paginationPanel.Height);
        }
        private void ApplyLocalization()
        {
            // Hide sensitive data like Password and deparmentID
            dataGridView_OperatorManagement.Columns["UserName"].Visible = false;
            dataGridView_OperatorManagement.Columns["Password"].Visible = false;
            dataGridView_OperatorManagement.Columns["DepartmentID"].Visible = false;
            dataGridView_OperatorManagement.Columns["OperatorID"].Visible = false;
            dataGridView_OperatorManagement.Columns["Employeename"].Visible = false;
            dataGridView_OperatorManagement.Columns["PositionID"].Visible = false;
            dataGridView_OperatorManagement.Columns["CreatedAt"].Visible = false;
            dataGridView_OperatorManagement.Columns["OperatorNameUnaccented"].Visible = false;
            dataGridView_OperatorManagement.Columns["UpdatedAt"].Visible = false;

            if (dataGridView_OperatorManagement.Columns.Contains("Username"))
                dataGridView_OperatorManagement.Columns["Username"].HeaderText = LocalizationManager.GetString("Username");
            dataGridView_OperatorManagement.Columns["EmployeeID"].HeaderText = LocalizationManager.GetString("EmployeeID");
            dataGridView_OperatorManagement.Columns["OperatorName"].HeaderText = LocalizationManager.GetString("EmployeeName");
            dataGridView_OperatorManagement.Columns["IsActive"].HeaderText = LocalizationManager.GetString("IsActive");
            dataGridView_OperatorManagement.Columns["DepartmentName"].HeaderText = LocalizationManager.GetString("DepartmentName");
            dataGridView_OperatorManagement.Columns["PositionName"].HeaderText = LocalizationManager.GetString("PositionName");

            // custom header
            dataGridView_OperatorManagement.EnableHeadersVisualStyles = false;
            dataGridView_OperatorManagement.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridView_OperatorManagement.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dataGridView_OperatorManagement.ColumnHeadersHeight = 30;

            // ✅ Add Action Column if it does not exist
            if (!dataGridView_OperatorManagement.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = "Action",
                    UseColumnTextForButtonValue = false  // ✅ Set to false to allow dynamic text change
                };
                dataGridView_OperatorManagement.Columns.Add(actionColumn);
            }

            // ✅ Set every row to be read-only at the start
            foreach (DataGridViewRow row in dataGridView_OperatorManagement.Rows)
            {
                row.Cells["Username"].ReadOnly = true;
                row.Cells["EmployeeID"].ReadOnly = true;
                row.Cells["EmployeeName"].ReadOnly = true;
                row.Cells["IsActive"].ReadOnly = true;
                row.Cells["DepartmentName"].ReadOnly = true;
                row.Cells["PositionName"].ReadOnly = true;

                row.Cells["Action"].Value = "Edit";
            }
            dataGridView_OperatorManagement.Columns["Action"].HeaderText = LocalizationManager.GetString("Action");

            if (dataGridView_OperatorManagement != null)
            {
                dataGridView_OperatorManagement.CellClick -= DataGridView_OperatorManagement_CellClick; // Unsubscribe if exists
                dataGridView_OperatorManagement.CellClick += DataGridView_OperatorManagement_CellClick; // Subscribe
            }
        }

        private void DataGridView_OperatorManagement_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView_OperatorManagement.Rows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= dataGridView_OperatorManagement.Columns.Count)
                return;

            DataGridViewRow row = dataGridView_OperatorManagement.Rows[e.RowIndex];

            if (dataGridView_OperatorManagement.Columns[e.ColumnIndex].Name == "Action")
            {
                string action = row.Cells["Action"].Value.ToString();

                if (action == "Edit")
                {
                    // Reset "Action" column for all other rows to prevent multiple edits
                    foreach (DataGridViewRow r in dataGridView_OperatorManagement.Rows)
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
            else if (dataGridView_OperatorManagement.Columns[e.ColumnIndex].Name == "CancelAction")
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
        private void HandleUpdateAction(DataGridViewRow row)
        {
            int newEmployeeID = Convert.ToInt32(row.Cells["EmployeeID"].Value);
            int newDepartmentID = Convert.ToInt32(row.Cells["DepartmentCombo"].Value);
            int newPositionID = Convert.ToInt32(row.Cells["PositionCombo"].Value);
            int newOperatorID = Convert.ToInt32(row.Cells["OperatorID"].Value);
            string newOperatorName = row.Cells["OperatorName"].Value.ToString();
            bool newIsActive = Convert.ToBoolean(row.Cells["IsActive"].Value);

            Employee employee = new Employee(newOperatorID, newOperatorName, newEmployeeID, newPositionID, newDepartmentID, newIsActive);
            // Call your update function
            bool status = DbHelper.updateOperator(employee);
            string message = status ? "Updated user : " + newOperatorName : "Cannot update user : " + newOperatorName;
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
        private void UpdateDepartmentAndPositionName(int rowIndex, string key, string newName)
        {
            if (rowIndex >= 0 && rowIndex < dataGridView_OperatorManagement.Rows.Count)
            {
                // Update the hidden column
                dataGridView_OperatorManagement.Rows[rowIndex].Cells[$"{key}Name"].Value = newName;

                // Refresh to reflect changes
                dataGridView_OperatorManagement.Refresh();
            }
        }

        private void RemoveColumnIfExists(string columnName)
        {
            if (dataGridView_OperatorManagement.Columns.Contains(columnName))
            {
                dataGridView_OperatorManagement.Columns.Remove(columnName);
            }
        }

        private void ShowRegisterUser()
        {
            // Hide the GridView
            dataGridView_OperatorManagement.Visible = false;
            
                        // Show the user control
            frmRegister.Location = dataGridView_OperatorManagement.Location;
            frmRegister.Size = dataGridView_OperatorManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }

        private void MainForm_Click(object sender, EventArgs e)
        {
            if (!frmRegister.Bounds.Contains(PointToClient(MousePosition)))
            {
                frmRegister.Visible = false;
            }
        }

        private void RegisterForm_UserCreated(object sender, Employee newUser)
        {
            employees.Insert(0, newUser);
            LoadDataGridView();
        }

        private void LoadDataGridView()
        {
            // Store the state of existing columns
            var columnState = dataGridView_OperatorManagement.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => new
                {
                    c.Name,
                    c.Visible,
                    c.DisplayIndex
                })
                .ToList();

            // Reset the data source
            dataGridView_OperatorManagement.DataSource = null;
            dataGridView_OperatorManagement.DataSource = employees.ToList(); // Bind to updated list

            // Ensure Action column exists and is set correctly
            if (!dataGridView_OperatorManagement.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = LocalizationManager.GetString("Action"),
                    UseColumnTextForButtonValue = false // Allows for dynamic text
                };
                // Add to the end of the columns to not interfere with existing order
                dataGridView_OperatorManagement.Columns.Add(actionColumn);
            }

            // Restore column state after resetting the data source
            foreach (var column in columnState)
            {
                if (dataGridView_OperatorManagement.Columns.Contains(column.Name))
                {
                    dataGridView_OperatorManagement.Columns[column.Name].Visible = column.Visible;
                    dataGridView_OperatorManagement.Columns[column.Name].DisplayIndex = column.DisplayIndex;
                }
            }
        }

        private void CreatelabelTotalControls()
        {
            // Creating pagination panel if it does not exist
            if (paginationPanel == null)
            {
                paginationPanel = new PanelControl()
                {
                    Dock = DockStyle.Bottom, // Dock to the bottom
                    Height = 50, // Set fixed height
                    Padding = new Padding(10)
                };

                lblPageInfo = new LabelControl()
                {
                    Size = new Size(200, 30),
                    ForeColor = Color.Green,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Location = new Point(20, 10)
                };

                paginationPanel.Controls.Add(lblPageInfo); // Add Label to Panel
                this.Controls.Add(paginationPanel); // Finally add Panel to control collection
            }

            lblPageInfo.Text = $"{LocalizationManager.GetString("TotalRecords")} {employees.Count}"; // Update the label text
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
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            if (Global.CurrentUser == null) { return; }
            //int departmentID = Global.CurrentUser.DepartmentID;
            var request = new { app = Global.App, action = "getOperators" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                var response = ResponseMessage<List<Employee>>.FromJson(jsonData);

                if (response?.Action != "getOperators") return;
                if (response?.Users != null && response.Users.Count > 0)
                {
                    // Show SplashScreen from parent Form
                    var parentForm = this.FindForm();
                    if (parentForm != null)
                    {
                        SplashScreenManager.ShowForm(parentForm, typeof(frmLoading), true, true, false);
                    }

                    // Simulate loading progress (optional)
                    for (int i = 1; i <= 100; i += 20)
                    {
                        if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                        {
                            SplashScreenManager.Default.SetWaitFormDescription($"Loading... {i}%");
                        }
                        Thread.Sleep(10); // Simulate brief loading
                    }

                    // Safely update UI
                    SafeInvoke(() =>
                    {
                        employees.Clear();
                        UpdateEmployees(response.Users);

                        CreatelabelTotalControls();
                        LoadDataGridView();
                        ApplyLocalization();

                        paginationPanel?.Invalidate();
                        paginationPanel?.Refresh();
                    });
                }
                else
                {
                    SafeInvoke(() =>
                        ShowMessage.ShowInfo("No Data Found"));
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                SafeInvoke(() =>
                    ShowMessage.ShowError($"JSON Deserialization Error: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                    ShowMessage.ShowError($"An error occurred: {ex.Message}"));
            }
            finally
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.CloseForm(false);
                }
            }
        }

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


        private void AddCancelButton(DataGridViewRow row)
        {
            if (!dataGridView_OperatorManagement.Columns.Contains("CancelAction"))
            {
                DataGridViewButtonColumn cancelColumn = new DataGridViewButtonColumn
                {
                    Name = "CancelAction",
                    HeaderText = "Cancel",
                    Text = "Cancel",
                    UseColumnTextForButtonValue = true
                };
                dataGridView_OperatorManagement.Columns.Add(cancelColumn);
            }
            row.Cells["CancelAction"].Value = "Cancel";
        }
        private void SetColumnVisibility(bool isVisible, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (dataGridView_OperatorManagement.Columns.Contains(name))
                {
                    dataGridView_OperatorManagement.Columns[name].Visible = isVisible;
                }
            }
        }
        private void SetComboBoxInitialValues(DataGridViewRow row, List<Department> departments, List<Position> positions)
        {
            foreach (DataGridViewRow dgvRow in dataGridView_OperatorManagement.Rows)
            {
                string deptName = dgvRow.Cells["DepartmentName"].Value?.ToString();
                string plantName = dgvRow.Cells["PositionName"].Value?.ToString();

                int deptID = departments.FirstOrDefault(d => string.Equals(d.DepartmentName, deptName, StringComparison.OrdinalIgnoreCase))?.DepartmentID ?? departments.First().DepartmentID;
                dgvRow.Cells["DepartmentCombo"].Value = deptID;

                int positionID = positions.FirstOrDefault(p => string.Equals(p.PositionName, plantName, StringComparison.OrdinalIgnoreCase))?.PositionID ?? positions.First().PositionID;
                dgvRow.Cells["PositionCombo"].Value = positionID;
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
        }
        private void UpdateEmployees(List<Employee> newEmployees)
        {
            foreach (var employee in newEmployees)
            {
                SetEmployeeDepartmentAndPosition(employee);
                employees.Add(employee);
            }
        }

        private void SetEmployeeDepartmentAndPosition(Employee employee)
        {
            DataTable departmentTable = DbHelper.getDepartments();
            DataTable positionTable = DbHelper.getPositions();
            employee.DepartmentName = FindDepartmentName(employee.DepartmentID, departmentTable);
            employee.PositionName = FindPositionName(employee.PositionID, positionTable);
        }

        private string FindDepartmentName(int departmentId, DataTable departmentTable)
        {
            return departmentTable.AsEnumerable()
                .Where(row => row.Field<int>("DepartmentID") == departmentId)
                .Select(row => row.Field<string>("DepartmentName"))
                .FirstOrDefault() ?? "Unknown";
        }

        private string FindPositionName(int positionId, DataTable positionTable)
        {
            return positionTable.AsEnumerable()
                .Where(row => row.Field<int>("PositionID") == positionId)
                .Select(row => row.Field<string>("PositionName"))
                .FirstOrDefault() ?? "Unknown";
        }

        private void LoadTextLable()
        {
            // Load the text for labels, prompts, etc.
        }

        private void CreateButtonContainer()
        {
            // Create container panel
            groupPanelButtonContainer = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 70
            };
            Controls.Add(groupPanelButtonContainer);

            int marginLeft = 10;
            int marginTop = 10;
            int spacing = 10;

            // 🔹 TextBox for OperatorID (with placeholder simulation)
            txtOperatorId = new TextBox
            {
                Location = new Point(marginLeft, marginTop),
                Size = new Size(100, 30),
                ForeColor = Color.Gray,
                Text = LocalizationManager.GetString("EmployeeID")
            };
            txtOperatorId.GotFocus += (s, e) =>
            {
                if (txtOperatorId.Text == LocalizationManager.GetString("EmployeeID"))
                {
                    txtOperatorId.Text = "";
                    txtOperatorId.ForeColor = Color.Black;
                }
            };
            txtOperatorId.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtOperatorId.Text))
                {
                    txtOperatorId.Text = LocalizationManager.GetString("EmployeeID");
                    txtOperatorId.ForeColor = Color.Gray;
                }
            };
            groupPanelButtonContainer.Controls.Add(txtOperatorId);

            // 🔹 Find EmployeeID button
            SimpleButton btnFind = new SimpleButton
            {
                Text = LocalizationManager.GetString("FindEmployeeCode"),
                Size = new Size(120, 30),
                Location = new Point(txtOperatorId.Right + spacing, marginTop)
            };
            btnFind.Click += BtnFind_Click;
            groupPanelButtonContainer.Controls.Add(btnFind);

            // 🔹 Register Operator button
            SimpleButton btnRegister = new SimpleButton
            {
                Text = LocalizationManager.GetString("RegisterOperator"),
                Size = new Size(135, 40),
                Location = new Point(btnFind.Right + spacing, marginTop)
            };
            btnRegister.Click += Button_Click;
            groupPanelButtonContainer.Controls.Add(btnRegister);

            // 🔹 Sync button
            SimpleButton syncButton = new SimpleButton
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(100, 40),
                Location = new Point(btnRegister.Right + spacing, marginTop),
                ImageOptions = { Image = Properties.Resources.sync_icon }
            };
            syncButton.Click += SyncButton_Click;
            groupPanelButtonContainer.Controls.Add(syncButton);

            // 🔹 Result Label
            lblResult = new Label
            {
                Location = new Point(marginLeft, txtOperatorId.Bottom + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Text = ""
            };
            groupPanelButtonContainer.Controls.Add(lblResult);
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            string filterText = txtOperatorId.Text.Trim();

            if (!string.IsNullOrWhiteSpace(filterText) && filterText != LocalizationManager.GetString("EmployeeID"))
            {
                var filteredList = employees
                    .Where(o => o.EmployeeID.ToString().Contains(filterText))
                    .ToList();

                dataGridView_OperatorManagement.DataSource = new BindingSource { DataSource = filteredList };
                lblResult.Text = $"🔍 Showing {filteredList.Count} result(s)";
            }
            else
            {
                // ✅ Show full list
                dataGridView_OperatorManagement.DataSource = new BindingSource { DataSource = employees };
                lblResult.Text = $"✅ Showing all {employees.Count} employees";
            }

            ApplyLocalization(); // <-- Reapply header labels and action column
        }

        private void Button_Click(object sender, EventArgs e)
        {
            ShowRegisterUser();
        }
        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the GridView
            dataGridView_OperatorManagement.Visible = true;

            // Hide the user control
            frmRegister.Visible = false;
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
            if (!dataGridView_OperatorManagement.Columns.Contains(columnName))
            {
                int insertIndex = columnName == "DepartmentCombo" ? dataGridView_OperatorManagement.Columns["DepartmentName"].Index : dataGridView_OperatorManagement.Columns["PositionName"].Index;
                dataGridView_OperatorManagement.Columns.Insert(insertIndex, comboBoxColumn);
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
