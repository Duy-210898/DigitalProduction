using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraSplashScreen;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucOperatorManagement : DevExpress.XtraEditors.XtraUserControl
    {
        // --- Instance Fields ---
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private ucRegisterOperator frmRegister; // Renamed to ucRegisterOperator for consistency with original code
        private PanelControl paginationPanel;

        // --- Constructor ---
        public ucOperatorManagement()
        {
            InitializeComponent();
            LoadTextLable();
            CreateButtonContainer();

            // Group panel and appearance customization from the original ucUserManagement
            gridViewOperatorManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            gridViewOperatorManagement.OptionsFind.ShowFindButton = false;
            gridViewOperatorManagement.RowHeight = 50;

            // Show add new user
            frmRegister = new ucRegisterOperator();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            // frmRegister.UserCreated += RegisterForm_UserCreated;
        }

        // --- Data Loading and Binding ---
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        // Send request to WebSocket or API and load data
        public async Task GetDataAndLoadToGridAsync()
        {
            if (Global.CurrentUser == null) { return; } // Check for current user
            var request = new { app = Global.App, action = "getOperators" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void LoadDataGridView()
        {
            // Save column state before setting DataSource = null
            var columnState = gridViewOperatorManagement.Columns
                .Cast<GridColumn>()
                .Where(c => c.FieldName != "DepartmentID" && c.FieldName != "PositionID")
                .Select(c => new
                {
                    c.FieldName,
                    c.Visible,
                    c.VisibleIndex
                })
                .ToList();

            // Reset the data source
            // Refresh the grid control with the new employee data
            gridViewOperatorManagement.SortInfo.Clear();
            gridControlOperatorManagement.DataSource = employees.OrderBy(e => e.OperatorName.Split(' ').Last()).ToList();

            // Restore column state after resetting the data source
            foreach (var column in columnState)
            {
                var col = gridViewOperatorManagement.Columns[column.FieldName];
                if (col != null)
                {
                    col.Visible = column.Visible;
                    col.VisibleIndex = column.VisibleIndex;
                }
            }
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
                    var parentForm = this.FindForm();
                    if (parentForm != null)
                    {
                        SplashScreenManager.ShowForm(parentForm, typeof(frmLoading), true, true, false);
                    }

                    // Simulate loading progress
                    for (int i = 1; i <= 100; i += 20)
                    {
                        if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                        {
                            SplashScreenManager.Default.SetWaitFormDescription($"Loading... {i}%");
                        }
                        Thread.Sleep(10);
                    }

                    SafeInvoke(() =>
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
                        LoadDataGridView();
                        gridViewOperatorManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                        Extentions.showEditModeCellGridView(gridControlOperatorManagement, gridViewOperatorManagement, "ucOperatorManagement");
                        ApplyLocalization(); // Apply column configuration

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

        // --- UI Initialization and Events --

        private void CreateButtonContainer()
        {
         
            // TextBox for EmployeeID (Search Field)

            txtOperatorId.Text = LocalizationManager.GetString("EmployeeID");
            txtOperatorId.ForeColor = Color.Gray;
            txtOperatorId.GotFocus += (s, e) =>
            {
                if (txtOperatorId.Text == LocalizationManager.GetString("EmployeeID"))
                {
                    txtOperatorId.Text = "";
                }
            };
            txtOperatorId.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtOperatorId.Text))
                {
                    txtOperatorId.Text = LocalizationManager.GetString("EmployeeID");
                }
            };

            // Find EmployeeID button
            btnFind.Text = LocalizationManager.GetString("FindEmployeeCode");
            btnFind.Click += BtnFind_Click;

            // Register Operator button
            btnRegister.Text = LocalizationManager.GetString("RegisterOperator");
           
            btnRegister.Click += Button_Click; // Calls ShowRegisterUser

            // Sync button
            btnSync.Text = LocalizationManager.GetString("Sync");

            btnSync.Click += async (sender, e) =>
            {
                btnSync.Enabled = false;
                btnSync.Text = LocalizationManager.GetString("Loading") + "...";

                await GetDataAndLoadToGridAsync();

                btnSync.Enabled = true;
                btnSync.Text = LocalizationManager.GetString("Sync");
            };

            // Result Label
            lblResult.Text = String.Empty;
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            string filterText = txtOperatorId.Text.Trim();

            if (!string.IsNullOrWhiteSpace(filterText) && filterText != LocalizationManager.GetString("EmployeeID"))
            {
                var filteredList = employees
                    .Where(o => o.EmployeeID.ToString().Contains(filterText))
                    .ToList();

                gridControlOperatorManagement.DataSource = filteredList;
                lblResult.Text = $"🔍 Showing {filteredList.Count} result(s)";
            }
            else
            {
                gridControlOperatorManagement.DataSource = employees.ToList();
                lblResult.Text = $"✅ Showing all {employees.Count} employees";
            }

            ApplyLocalization(); // Re-apply configuration after data source change
        }

        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterUser();
        }


        private void showRegisterUser()
        {
            gridControlOperatorManagement.Visible = false;
            frmRegister.Location = gridControlOperatorManagement.Location;
            frmRegister.Size = gridControlOperatorManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }

        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            gridControlOperatorManagement.Visible = true;
            frmRegister.Visible = false;
            // Refresh data when exiting the registration screen
            _ = GetDataAndLoadToGridAsync();
        }

        private void gridView_CustomDrawGroupPanel(object sender, DevExpress.XtraGrid.Views.Base.CustomDrawEventArgs e)
        {
            // Set the alignment of the GroupPanelText
            e.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            e.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            // Optional: You can also change the font and color if necessary
            e.Appearance.Font = new Font("Tahoma", 13, FontStyle.Bold);

            e.Appearance.ForeColor = Color.Blue;
        }

        private void LoadTextLable()
        {
            // Implementation for loading text labels (localization)
        }

        // --- Column Configuration and Localization ---
        private void ApplyLocalization()
        {
            // Apply column configuration only if the grid has data and columns are created
            if (gridViewOperatorManagement.Columns.Count == 0) return;

            var cols = gridViewOperatorManagement.Columns;

            // Columns to hide everywhere (including Edit Form)
            if (cols["Username"] != null) cols["Username"].Visible = false;
            if (cols["Password"] != null) cols["Password"].Visible = false;
            if (cols["OperatorID"] != null) cols["OperatorID"].Visible = false;
            if (cols["EmployeeName"] != null) cols["EmployeeName"].Visible = false;
            if (cols["PositionID"] != null) cols["PositionID"].Visible = false;
            if (cols["DepartmentID"] != null) cols["DepartmentID"].Visible = false;
            if (cols["CreatedAt"] != null) cols["CreatedAt"].Visible = false;
            if (cols["OperatorNameUnaccented"] != null) cols["OperatorNameUnaccented"].Visible = false;
            if (cols["UpdatedAt"] != null) cols["UpdatedAt"].Visible = false;

            // Columns visible on the Grid, and automatically editable/visible in Edit Form
            if (cols["EmployeeID"] != null)
            {
                cols["EmployeeID"].Caption = LocalizationManager.GetString("EmployeeID");
                cols["EmployeeID"].OptionsColumn.AllowEdit = true;
            }
            if (cols["OperatorName"] != null)
            {
                cols["OperatorName"].Caption = LocalizationManager.GetString("OperatorName");
                cols["OperatorName"].OptionsColumn.AllowEdit = true;
            }
            if (cols["IsActive"] != null)
            {
                cols["IsActive"].Caption = LocalizationManager.GetString("IsActive");
                cols["IsActive"].OptionsColumn.AllowEdit = true;
            }

            // Columns visible on the Grid, read-only (display names)
            if (cols["DepartmentName"] != null)
            {
                cols["DepartmentName"].Caption = LocalizationManager.GetString("DepartmentName");
                cols["DepartmentName"].OptionsColumn.AllowEdit = false;
            }
            if (cols["PositionName"] != null)
            {
                cols["PositionName"].Caption = LocalizationManager.GetString("PositionName");
                cols["PositionName"].OptionsColumn.AllowEdit = false;
            }
            if (cols["Action"] != null)
            {
                cols["Action"].Caption = LocalizationManager.GetString("Action");
                cols["Action"].OptionsColumn.AllowEdit = true;
            }

            // DevExpress Header Customization
            gridViewOperatorManagement.Appearance.HeaderPanel.Options.UseFont = true;
            gridViewOperatorManagement.Appearance.HeaderPanel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            gridViewOperatorManagement.OptionsFind.ShowFindButton = false;
            gridViewOperatorManagement.OptionsView.ShowGroupPanel = false;
            gridViewOperatorManagement.BestFitColumns();
        }

        // --- Helper Methods (Re-included for completeness) ---

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try
                {
                    Invoke(action);
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                action();
            }
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