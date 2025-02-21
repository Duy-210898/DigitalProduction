using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucUserManagement : DevExpress.XtraEditors.XtraUserControl
    {
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterUser frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;
        public ucUserManagement()
        {
            InitializeComponent();
            LoadTextLable();
            gridView_UserManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
           // gridView_OperatorManagement.ShowFindPanel();
            gridView_UserManagement.OptionsFind.ShowFindButton = false;
            gridView_UserManagement.RowHeight = 50;
            CreateButtonContainer();

            // show add new user
            frmRegister = new ucRegisterUser();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            frmRegister.UserCreated += RegisterForm_UserCreated;
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
                Height = 50, // Adjust height
                Padding = new Padding(10)
            };

            // Create Label for page info (Total Records)
            lblPageInfo = new LabelControl()
            {
                Text = $"Total Records: {employees.Count}",
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

        // Event to handle refresh button click
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            _ = GetDataAndLoadToGridAsync();
        }


        private void MainForm_Click(object sender, EventArgs e)
        {
            if (!frmRegister.Bounds.Contains(PointToClient(MousePosition)))
            {
                frmRegister.Visible = false;
            }
        }
        // Event handler to refresh the grid when a user is created
        private void RegisterForm_UserCreated(object sender, Employee newUser)
        {
            employees.Insert(0, newUser); // GridView will automatically refresh
            gridView_UserManagement.FocusedRowHandle = 0;
        }
        private void showRegisterUser()
        {
            // Hide the GridView
            gridControl_UserManagement.Visible = false;

            // Show the user control
            frmRegister.Location = gridControl_UserManagement.Location;
            frmRegister.Size = gridControl_UserManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }
        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the GridView
            gridControl_UserManagement.Visible = true;

            // Hide the user control
            frmRegister.Visible = false;
        }
        private void registerButton_Click(object sender, EventArgs e)
        {
            // Logic for user registration goes here...

            // Show popup
            MessageBox.Show("User registered successfully!", "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            var request = new { app = Global.App, action = "getUsers" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                // Assuming the response
                ResponseMessage<List<Employee>> response = ResponseMessage<List<Employee>>.FromJson(jsonData);

                // Check if there are devices in the response
                if (response?.Users != null && response.Users.Count > 0)
                {
                    // Clear current employees list
                    employees.Clear();

                    // Retrieve departments from the database as a DataTable
                    DataTable departmentTable = DbHelper.getDepartments();
                    DataTable positionTable = DbHelper.getPositions();

                    foreach (var employee in response.Users)
                    {
                        // Find the department name by DepartmentID
                        var departmentRow = departmentTable.AsEnumerable()
                                                           .FirstOrDefault(row => row.Field<int>("DepartmentID") == employee.DepartmentID);
                        var positionRow = positionTable.AsEnumerable()
                                                           .FirstOrDefault(row => row.Field<int>("PositionID") == employee.PositionID);

                        // If the department is found, assign the DepartmentName to the employee
                        if (departmentRow != null)
                        {
                            employee.DepartmentName = departmentRow.Field<string>("DepartmentName");
                        }
                        else
                        {
                            employee.DepartmentName = "Unknown";
                        }

                        if (positionRow != null)
                        {
                            employee.PositionName = positionRow.Field<string>("PositionName");
                        }
                        else
                        {
                            employee.PositionName = "Unknown";
                        }
                        // Add the employee to the list
                        employees.Add(employee);
                    }
                    CreatelabelTotalControls();

                    // Refresh the grid control with the new employee data
                    gridView_UserManagement.SortInfo.Clear();
                    gridControl_UserManagement.DataSource = employees.OrderBy(e => e.EmployeeName.Split(' ').Last()).ToList();

                    // Configure grid columns and apply the edit mode
                    ConfigureGridView();
                    gridView_UserManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                    Extentions.showEditModeCellGridView(gridControl_UserManagement, gridView_UserManagement, "ucManagement");

                    // Apply localization for the grid
                    ApplyLocalization();
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

        private void ConfigureGridView()
        {
            gridView_UserManagement.BestFitColumns();
            // Hide sensitive data like Password and deparmentID
            gridView_UserManagement.Columns["Password"].Visible = false;
            gridView_UserManagement.Columns["DepartmentID"].Visible = false;
            gridView_UserManagement.Columns["OperatorID"].Visible = false;
            gridView_UserManagement.Columns["OperatorName"].Visible = false;
            gridView_UserManagement.Columns["PositionID"].Visible = false;
            gridView_UserManagement.Columns["CreatedAt"].Visible = false;
            gridView_UserManagement.Columns["UpdatedAt"].Visible = false;

            // Format the DateTime columns
       /*     gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";*/

            // Apply sorting by Username in ascending order when the grid loads
            gridView_UserManagement.SortInfo.Clear();
            gridView_UserManagement.SortInfo.Add(new GridColumnSortInfo(gridView_UserManagement.Columns["EmployeeName"], DevExpress.Data.ColumnSortOrder.Ascending));

            // Set custom column captions
            gridView_UserManagement.Columns["EmployeeName"].Caption = "Full Name";
            gridView_UserManagement.Columns["Username"].Caption = "Username";
            gridView_UserManagement.Columns["DepartmentID"].Caption = "Department Name";
            gridView_UserManagement.Columns["PositionID"].Caption = "Position Name";

            // Optionally, format the IsActive column to display checkboxes
            gridView_UserManagement.Columns["IsActive"].ColumnEdit = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            // Apply custom styles to column headers
            gridView_UserManagement.Appearance.HeaderPanel.BorderColor = Color.LightSteelBlue;
            gridView_UserManagement.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView_UserManagement.Appearance.HeaderPanel.Font = new Font("Arial", 10, FontStyle.Bold);
        }

        private void LoadTextLable()
        {
            gridView_UserManagement.GroupPanelText = LocalizationManager.GetString("ListOfUser");
            gridView_UserManagement.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");
        }

        private void CreateButtonContainer()
        {
            // Create a PanelControl to hold the button
            groupPanelButtonContainer = new PanelControl()
            {
                Dock = DockStyle.Top,
                Height = 50 // Adjust the height to suit your layout
            };

            // Add the PanelControl to the form
            Controls.Add(groupPanelButtonContainer);

            // Create the button
            button = new SimpleButton()
            {
                Text = "Create user",
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
            showRegisterUser();
        }
        private void ApplyLocalization()
        {
            // grid view
            gridControl_UserManagement.BeginUpdate();

            if (!gridView_UserManagement.Columns.Count.Equals(0))
            {
                gridView_UserManagement.Columns["Username"].Caption = LocalizationManager.GetString("Username");
                gridView_UserManagement.Columns["EmployeeID"].Caption = LocalizationManager.GetString("EmployeeID");
                gridView_UserManagement.Columns["EmployeeName"].Caption = LocalizationManager.GetString("EmployeeName");
                gridView_UserManagement.Columns["IsActive"].Caption = LocalizationManager.GetString("IsActive");
                gridView_UserManagement.Columns["DepartmentName"].Caption = LocalizationManager.GetString("Department");
                gridView_UserManagement.Columns["PositionName"].Caption = LocalizationManager.GetString("Position");
                gridView_UserManagement.Columns["Action"].Caption = LocalizationManager.GetString("Action");

                //gridControl_UserManagement.DataSource = null;
                //gridControl_UserManagement.DataSource = employees;
                gridView_UserManagement.SortInfo.Clear();
            }
            gridControl_UserManagement.EndUpdate();
        }
    }
}