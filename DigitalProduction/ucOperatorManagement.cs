using System.Collections.Generic;
using System;
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
    public partial class ucOperatorManagement : UserControl
    {
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterOperator frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;
        public ucOperatorManagement()
        {
            InitializeComponent();
            //SettingGridControls();
            LoadTextLable();
            gridView_OperatorManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            gridView_OperatorManagement.ShowFindPanel();
            gridView_OperatorManagement.OptionsFind.ShowFindButton = false;
            gridView_OperatorManagement.RowHeight = 50;
            CreateButtonContainer();
            // show add new user
            frmRegister = new ucRegisterOperator();
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

            // Create a "Refresh" Button
            SimpleButton refreshButton = new SimpleButton()
            {
                Text = "Refresh",
                Size = new Size(100, 30), // Adjusted size for consistency
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
            employees.Insert(0, newUser);
            gridView_OperatorManagement.FocusedRowHandle = 0;
        }
        private void showRegisterUser()
        {
            // Hide the GridView
            gridControl_OperatorManagement.Visible = false;

            // Show the user control
            frmRegister.Location = gridControl_OperatorManagement.Location;
            frmRegister.Size = gridControl_OperatorManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }
        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the GridView
            gridControl_OperatorManagement.Visible = true;

            // Hide the user control
            frmRegister.Visible = false;
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
            if (Global.CurrentUser == null) { return; }
            int departmentID = Global.CurrentUser.DepartmentID;
            var request = new { app = Global.App, action = "getOperators", departmentID};
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
                    UpdateEmployees(response.Users);
                }
                else
                {
                    ShowMessage.ShowInfo("No Data Found");
                }

                Invoke(new Action(() =>
                {
                    gridControl_OperatorManagement.DataSource = null;
                    gridControl_OperatorManagement.DataSource = employees;
                    gridControl_OperatorManagement.RefreshDataSource();

                    ConfigureGridView(); // Reconfigure columns

                    if (gridView_OperatorManagement.RowCount > 0) { 
                        gridView_OperatorManagement.FocusedRowHandle = 0;
                    }
                    gridView_OperatorManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                    Extentions.showEditModeCellGridView(gridControl_OperatorManagement, gridView_OperatorManagement, "ucOperator");
                    ApplyLocalization();
                }));
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


        private void UpdateEmployees(List<Employee> newEmployees)
        {
            foreach (var employee in newEmployees)
            {
                // Assume there is a method to set the Department and Position name based on IDs
                SetEmployeeDepartmentAndPosition(employee);
                employees.Add(employee);
            }
            CreatelabelTotalControls();
        }

        private void SetEmployeeDepartmentAndPosition(Employee employee)
        {
            // Logic to set DepartmentName and PositionName from their respective Datatables
            DataTable departmentTable = DbHelper.getDepartments();
            DataTable positionTable = DbHelper.getPositions();

            // Find and assign department and position
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

        private void ConfigureGridView()
        {
            gridView_OperatorManagement.BestFitColumns();
            gridView_OperatorManagement.Columns["Password"].Visible = false;
            gridView_OperatorManagement.Columns["DepartmentID"].Visible = false;
            gridView_OperatorManagement.Columns["Username"].Visible = false;
            gridView_OperatorManagement.Columns["EmployeeName"].Visible = false;
            gridView_OperatorManagement.Columns["PositionID"].Visible = false;
            gridView_OperatorManagement.Columns["CreatedAt"].Visible = false;
            gridView_OperatorManagement.Columns["UpdatedAt"].Visible = false;
            // Format the DateTime columns
            /*     gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                 gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";
                 gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                 gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";*/

            // Apply sorting by Username in ascending order when the grid loads
            gridView_OperatorManagement.SortInfo.Clear();
            gridView_OperatorManagement.SortInfo.Add(new GridColumnSortInfo(gridView_OperatorManagement.Columns["OperatorName"], DevExpress.Data.ColumnSortOrder.Ascending));

            // Set custom column captions
            gridView_OperatorManagement.Columns["OperatorName"].Caption = "Full Name";
            gridView_OperatorManagement.Columns["DepartmentID"].Caption = "Department Name";
            gridView_OperatorManagement.Columns["PositionID"].Caption = "Position Name";

            // Optionally, format the IsActive column to display checkboxes
            gridView_OperatorManagement.Columns["IsActive"].ColumnEdit = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            // Apply custom styles to column headers
            gridView_OperatorManagement.Appearance.HeaderPanel.BorderColor = Color.LightSteelBlue;
            gridView_OperatorManagement.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView_OperatorManagement.Appearance.HeaderPanel.Font = new Font("Arial", 10, FontStyle.Bold);
        }

        private void LoadTextLable()
        {
            gridView_OperatorManagement.GroupPanelText = LocalizationManager.GetString("ListOfUser");
            gridView_OperatorManagement.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");
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
            gridControl_OperatorManagement.BeginUpdate();

            if (!gridView_OperatorManagement.Columns.Count.Equals(0))
            {
                gridView_OperatorManagement.Columns["EmployeeID"].Caption = LocalizationManager.GetString("EmployeeID");
                gridView_OperatorManagement.Columns["OperatorName"].Caption = LocalizationManager.GetString("EmployeeName");
                gridView_OperatorManagement.Columns["IsActive"].Caption = LocalizationManager.GetString("IsActive");
                gridView_OperatorManagement.Columns["DepartmentName"].Caption = LocalizationManager.GetString("Department");
                gridView_OperatorManagement.Columns["PositionName"].Caption = LocalizationManager.GetString("Position");
                gridView_OperatorManagement.Columns["Action"].Caption = LocalizationManager.GetString("Action");
                gridView_OperatorManagement.SortInfo.Clear();
            }
            gridView_OperatorManagement.EndUpdate();
        }
    }
}
