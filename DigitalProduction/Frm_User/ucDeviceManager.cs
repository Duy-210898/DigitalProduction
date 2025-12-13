using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
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
        private SimpleButton button;
        private ucRegisterDevice frmRegister;
        private Panel paginationPanel;
        private Label lblPageInfo;

        // DevExpress Controls
        private GridControl gridControlDevices;
        private GridView gridViewDevices;

        private bool isEditing = false;
        private int editingRowHandle = GridControl.InvalidRowHandle;

        private readonly Dictionary<int, object> originalEditValues = new Dictionary<int, object>();


        // Repository Items for in-place editing
        private RepositoryItemButtonEdit repositoryItemButtonEditAction;
        private RepositoryItemButtonEdit repositoryItemButtonEditCancel;
        private RepositoryItemGridLookUpEdit repositoryItemGridLookUpEditDepartment;
        private RepositoryItemGridLookUpEdit repositoryItemGridLookUpEditPlant;
        private RepositoryItemCheckEdit repositoryItemCheckEditIsActive;
        private RepositoryItemCheckEdit repositoryItemCheckEditConnectionStatus;


        public ucDeviceManager()
        {
            InitializeComponent();
            deviceDataTable = InitializeDeviceDataTable();
            InitializeGridControl();
            CreateButtonContainer();

            // show add new device
            frmRegister = new ucRegisterDevice();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            // frmRegister.DeviceCreated += RegisterForm_DeviceCreated; // Keep if needed
        }

        private void InitializeGridControl()
        {
            // 1. Create the GridControl and GridView
            gridControlDevices = new GridControl
            {
                Dock = DockStyle.Fill,
                DataSource = deviceDataTable
            };

            gridViewDevices = new GridView(gridControlDevices)
            {
                OptionsView = {
                    ShowGroupPanel = false,
                    ColumnAutoWidth = true,
                    RowAutoHeight = true,
                    EnableAppearanceEvenRow = true,
                    EnableAppearanceOddRow = true
                },
                OptionsBehavior = {
                    Editable = true,
                    ReadOnly = false
                }
            };

            gridControlDevices.MainView = gridViewDevices;
            this.Controls.Add(gridControlDevices);

            // 2. Configure Columns
            ConfigureGridColumns();

            // 3. Subscribe to event handlers
            gridViewDevices.RowCellClick += GridViewDevices_RowCellClick;
            gridViewDevices.CustomRowCellEdit += GridViewDevices_CustomRowCellEdit;
            gridViewDevices.CellValueChanged += GridViewDevices_CellValueChanged;
            gridControlDevices.LookAndFeel.UseDefaultLookAndFeel = true;
        }

        private void ConfigureGridColumns()
        {
            gridViewDevices.Columns.Clear();

            // Add all necessary columns from the DataTable
            foreach (DataColumn dataColumn in deviceDataTable.Columns)
            {
                var gridColumn = gridViewDevices.Columns.AddField(dataColumn.ColumnName);
                gridColumn.FieldName = dataColumn.ColumnName;
                gridColumn.Visible = true;
                gridColumn.OptionsColumn.AllowEdit = false; // Default to read-only
                gridColumn.OptionsColumn.ReadOnly = true;

                // Handle boolean columns with CheckEdit for better display
                if (dataColumn.DataType == typeof(bool))
                {
                    if (dataColumn.ColumnName == "IsActive")
                    {
                        repositoryItemCheckEditIsActive = new RepositoryItemCheckEdit();
                        gridControlDevices.RepositoryItems.Add(repositoryItemCheckEditIsActive);
                        gridColumn.ColumnEdit = repositoryItemCheckEditIsActive;
                    }
                    else if (dataColumn.ColumnName == "ConnectionStatus")
                    {
                        repositoryItemCheckEditConnectionStatus = new RepositoryItemCheckEdit { ReadOnly = true };
                        gridControlDevices.RepositoryItems.Add(repositoryItemCheckEditConnectionStatus);
                        gridColumn.ColumnEdit = repositoryItemCheckEditConnectionStatus;
                        gridColumn.OptionsColumn.AllowEdit = false;
                    }
                }
            }
            // Tạo và cấu hình RepositoryItemButtonEdit
            repositoryItemButtonEditAction = new RepositoryItemButtonEdit();
            repositoryItemButtonEditAction.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            var button = repositoryItemButtonEditAction.Buttons[0];
            button.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            button.Caption = Lang.Edit;
            button.ImageOptions.Image = null;

            // Gán sự kiện click
            repositoryItemButtonEditAction.ButtonClick += RepositoryItemButtonEditAction_ButtonClick;

            // Thêm vào repository
            gridControlDevices.RepositoryItems.Add(repositoryItemButtonEditAction);

            // Create Cancel button column (will be added to the grid later)
            repositoryItemButtonEditCancel = new RepositoryItemButtonEdit();
            repositoryItemButtonEditCancel.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            repositoryItemButtonEditCancel.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            repositoryItemButtonEditCancel.Buttons[0].Caption = Lang.Cancel;
            repositoryItemButtonEditCancel.ButtonClick += RepositoryItemButtonEditCancel_ButtonClick;
            gridControlDevices.RepositoryItems.Add(repositoryItemButtonEditCancel);


            // Add Action Column
            GridColumn actionColumn = new GridColumn
            {
                FieldName = "Action",
                Caption = LocalizationManager.GetString("Action"),
                VisibleIndex = gridViewDevices.Columns.Count,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                OptionsColumn = { AllowEdit = true, ReadOnly = false },
                ColumnEdit = repositoryItemButtonEditAction
            };
            gridViewDevices.Columns.Add(actionColumn);
            actionColumn.Width = 100;

            // Initialize LookUpEdit Repositories (will be populated in HandleEditAction)
            repositoryItemGridLookUpEditDepartment = new RepositoryItemGridLookUpEdit();
            repositoryItemGridLookUpEditPlant = new RepositoryItemGridLookUpEdit();

            // Set up all necessary Repository Items for editing Department/Plant in the grid
            ConfigureLookupEditRepositories(repositoryItemGridLookUpEditDepartment, "DepartmentCombo");
            ConfigureLookupEditRepositories(repositoryItemGridLookUpEditPlant, "PlantCombo");
        }

        private void ConfigureLookupEditRepositories(RepositoryItemGridLookUpEdit ri, string name)
        {
            ri.Name = name;
            ri.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            ri.ValueMember = name.Contains("Department") ? "DepartmentID" : "PlantID";
            ri.DisplayMember = name.Contains("Department") ? "DepartmentName" : "PlantName";
            ri.ShowFooter = false;
            ri.View.OptionsView.ShowAutoFilterRow = true;
            ri.View.OptionsView.ShowGroupPanel = false;
            ri.View.OptionsView.ShowIndicator = false;
            ri.PopupFormWidth = 200;
            gridControlDevices.RepositoryItems.Add(ri);
        }

        private void ApplyLocalization()
        {
            // Apply localization and styling for GridView columns
            if (gridViewDevices.Columns["Address"] != null)
                gridViewDevices.Columns["Address"].Caption = LocalizationManager.GetString("Address");
            if (gridViewDevices.Columns["Machine Name"] != null)
                gridViewDevices.Columns["Machine Name"].Caption = LocalizationManager.GetString("MachineName");
            if (gridViewDevices.Columns["Plant Name"] != null)
                gridViewDevices.Columns["Plant Name"].Caption = LocalizationManager.GetString("PlantName");
            if (gridViewDevices.Columns["Department Name"] != null)
                gridViewDevices.Columns["Department Name"].Caption = LocalizationManager.GetString("DepartmentName");
            if (gridViewDevices.Columns["ConnectionStatus"] != null)
                gridViewDevices.Columns["ConnectionStatus"].Caption = LocalizationManager.GetString("ConnectionStatus");
            if (gridViewDevices.Columns["IsActive"] != null)
                gridViewDevices.Columns["IsActive"].Caption = LocalizationManager.GetString("IsActive");
            if (gridViewDevices.Columns["DeviceID"] != null)
                gridViewDevices.Columns["DeviceID"].Visible = false;

            // Set initial action text
            repositoryItemButtonEditAction.Buttons[0].Caption = Lang.Edit;

            // Apply custom header style (DevExpress way)
            gridViewDevices.Appearance.HeaderPanel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            gridViewDevices.Appearance.HeaderPanel.ForeColor = Color.Black;

            gridViewDevices.BestFitColumns();
        }

        private void GridViewDevices_CustomRowCellEdit(object sender, CustomRowCellEditEventArgs e)
        {
            GridView view = sender as GridView;

            // Use Repository Items for in-place editing when the row is being edited
            if (e.RowHandle == editingRowHandle)
            {
                if (e.Column.FieldName == "Action")
                {
                    e.RepositoryItem = repositoryItemButtonEditAction;
                }
                // When editing, Department Name and Plant Name columns get the LookUpEdit control
                else if (e.Column.FieldName == "Department Name")
                {
                    e.RepositoryItem = repositoryItemGridLookUpEditDepartment;
                }
                else if (e.Column.FieldName == "Plant Name")
                {
                    e.RepositoryItem = repositoryItemGridLookUpEditPlant;
                }
                else if (e.Column.FieldName == "IsActive")
                {
                    e.RepositoryItem = repositoryItemCheckEditIsActive;
                }
            }
            // Logic for Cancel button (using an Unbound column named "Cancel")
            if (e.Column.FieldName == "Cancel")
            {
                if (e.RowHandle == editingRowHandle)
                {
                    e.RepositoryItem = repositoryItemButtonEditCancel;
                }
                else
                {
                    // Use a blank repository item to hide the button
                    e.RepositoryItem = null;
                }
            }

        }

        private void GridViewDevices_RowCellClick(object sender, RowCellClickEventArgs e)
        {
            // DevExpress grid uses CustomRowCellEdit and button events instead of CellClick for actions.
            // This event is generally not needed for the core logic implemented below.
        }

        private void RepositoryItemButtonEditAction_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            GridView view = gridViewDevices;
            int rowHandle = view.FocusedRowHandle;

            if (rowHandle == GridControl.InvalidRowHandle) return;

            // Check the current caption of the button that was clicked
            string actionCaption = repositoryItemButtonEditAction.Buttons[0].Caption;

            if (actionCaption == Lang.Edit && !isEditing)
            {
                // Ensure only one row is edited at a time
                if (editingRowHandle != GridControl.InvalidRowHandle)
                {
                    SetRowEditMode(editingRowHandle, false);
                }

                editingRowHandle = rowHandle;
                isEditing = true;
                HandleEditAction(rowHandle);
                view.RefreshData();
            }
            else if (actionCaption == Lang.Update && rowHandle == editingRowHandle)
            {
                HandleUpdateAction(rowHandle);
                view.RefreshData();
            }
        }

        private void RepositoryItemButtonEditCancel_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            GridView view = gridViewDevices;
            int rowHandle = view.FocusedRowHandle;

            if (rowHandle == GridControl.InvalidRowHandle || rowHandle != editingRowHandle) return;

            HandleCancelAction(rowHandle);
            view.RefreshData();
        }

        private void GridViewDevices_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            // If a value changes while editing, update the button text to Update
            if (e.RowHandle == editingRowHandle && repositoryItemButtonEditAction.Buttons[0].Caption == Lang.Edit)
            {
                repositoryItemButtonEditAction.Buttons[0].Caption = Lang.Update;
                gridViewDevices.InvalidateRowCell(editingRowHandle, gridViewDevices.Columns["Action"]);
            }
        }


        private void HandleEditAction(int rowHandle)
        {
            try
            {
                var departments = GetDepartments();
                var plants = GetPlants();

                if (departments == null || plants == null)
                    return;

                DataRow row = gridViewDevices.GetDataRow(rowHandle);
                int deviceId = Convert.ToInt32(row["DeviceID"]);

                // 1. Store original values in the class-level dictionary (FIX APPLIED HERE)
                if (!originalEditValues.ContainsKey(deviceId))
                {
                    originalEditValues.Add(deviceId, new
                    {
                        DepartmentName = row["Department Name"],
                        PlantName = row["Plant Name"]
                    });
                }

                // 2. Configure and populate LookUpEdit repositories
                repositoryItemGridLookUpEditDepartment.DataSource = departments;
                repositoryItemGridLookUpEditPlant.DataSource = plants;

                // Get the current Name values
                string deptName = row["Department Name"]?.ToString();
                string plantName = row["Plant Name"]?.ToString();

                // Find the corresponding ID for the current row
                int deptID = departments.FirstOrDefault(d => string.Equals(d.DepartmentName, deptName, StringComparison.OrdinalIgnoreCase))?.DepartmentID ?? (departments.Any() ? departments.First().DepartmentID : 0);
                int plantID = plants.FirstOrDefault(p => string.Equals(p.PlantName, plantName, StringComparison.OrdinalIgnoreCase))?.PlantID ?? (plants.Any() ? plants.First().PlantID : 0);

                // Temporarily replace the display text (Name) in the DataRow with the Value (ID) for the LookUpEdit to work correctly
                row["Department Name"] = deptID;
                row["Plant Name"] = plantID;

                // 3. Add Cancel button column if not present
                if (gridViewDevices.Columns["Cancel"] == null)
                {
                    GridColumn cancelColumn = new GridColumn
                    {
                        FieldName = "Cancel",
                        Caption = Lang.Cancel,
                        VisibleIndex = gridViewDevices.Columns.Count,
                        UnboundType = DevExpress.Data.UnboundColumnType.Object,
                        OptionsColumn = { AllowEdit = true, ReadOnly = false },
                        ColumnEdit = repositoryItemButtonEditCancel
                    };
                    gridViewDevices.Columns.Add(cancelColumn);
                    cancelColumn.Width = 100;
                }

                // 4. Set row to editable and change button text
                SetRowEditMode(rowHandle, true);
                repositoryItemButtonEditAction.Buttons[0].Caption = Lang.Update;

                // Refresh specific cells to apply custom editors/button text
                gridViewDevices.RefreshRowCell(rowHandle, gridViewDevices.Columns["Action"]);
                gridViewDevices.RefreshRowCell(rowHandle, gridViewDevices.Columns["Cancel"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during edit: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetRowEditMode(rowHandle, false);
            }
        }

        private void HandleUpdateAction(int rowHandle)
        {
            gridViewDevices.CloseEditor(); // Commit any pending changes from the editor
            DataRow row = gridViewDevices.GetDataRow(rowHandle);

            if (row == null) return;

            try
            {
                int deviceId = Convert.ToInt32(row["DeviceID"]);
                // Retrieve the updated IDs from the DataRow (which were set by the LookUpEdit controls)
                int plantId = Convert.ToInt32(row["Plant Name"]);
                int departmentId = Convert.ToInt32(row["Department Name"]);
                string address = row["Address"]?.ToString();
                string machineName = row["Machine Name"]?.ToString();
                bool isActive = Convert.ToBoolean(row["IsActive"]);
                bool connectionStatus = Convert.ToBoolean(row["ConnectionStatus"]);

                bool status = DbHelper.updateDevice(deviceId, departmentId, plantId, address, machineName, isActive, connectionStatus);
                string message = status ? "Updated device at: " + address : "Cannot update device at: " + address;

                if (status)
                {
                    // Update the row's display values from ID back to Name for permanent display mode
                    string departmentName = Extentions.getNameFromDataTable(DbHelper.getDepartments(), departmentId, "departmentID", "departmentName");
                    string plantName = Extentions.getNameFromDataTable(DbHelper.getPlants(), plantId, "plantID", "plantName");

                    row["Department Name"] = departmentName;
                    row["Plant Name"] = plantName;

                    // Update the underlying DataTable to refresh the GridView display
                    deviceDataTable.AcceptChanges();

                    // Clear the original values from the dictionary upon successful update
                    if (originalEditValues.ContainsKey(deviceId))
                    {
                        originalEditValues.Remove(deviceId);
                    }
                }

                ShowMessage.ShowInfo(message, status ? "Success" : "Fail");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during update: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Exit edit mode and clean up
                SetRowEditMode(rowHandle, false);
                isEditing = false;
                editingRowHandle = GridControl.InvalidRowHandle;

                repositoryItemButtonEditAction.Buttons[0].Caption = Lang.Edit;

                // Remove Cancel button column if it exists
                if (gridViewDevices.Columns["Cancel"] != null)
                {
                    gridViewDevices.Columns.Remove(gridViewDevices.Columns["Cancel"]);
                }

                gridViewDevices.RefreshData();
            }
        }

        private void HandleCancelAction(int rowHandle)
        {
            DataRow row = gridViewDevices.GetDataRow(rowHandle);
            if (row == null) return;

            int deviceId = Convert.ToInt32(row["DeviceID"]);

            // FIX APPLIED HERE: Retrieve original values from the dictionary
            if (originalEditValues.TryGetValue(deviceId, out object originalValueObj))
            {
                var originalValues = (dynamic)originalValueObj;

                // Revert changes back to the DataRow
                row["Department Name"] = originalValues.DepartmentName;
                row["Plant Name"] = originalValues.PlantName;

                // Clean up the dictionary
                originalEditValues.Remove(deviceId);
            }
            // else: If for some reason the original value wasn't stored, the user sees the partial edit, but the row exits edit mode.


            // Exit edit mode and clean up
            SetRowEditMode(rowHandle, false);
            isEditing = false;
            editingRowHandle = GridControl.InvalidRowHandle;

            repositoryItemButtonEditAction.Buttons[0].Caption = Lang.Edit;

            // Remove Cancel button column if it exists
            if (gridViewDevices.Columns["Cancel"] != null)
            {
                gridViewDevices.Columns.Remove(gridViewDevices.Columns["Cancel"]);
            }

            gridViewDevices.RefreshData();
        }

        private void SetRowEditMode(int rowHandle, bool isEditable)
        {
            // The CustomRowCellEdit event handles assigning the correct editor (LookUpEdit/TextEdit/CheckEdit)

            // Set AllowEdit/ReadOnly state for columns that should be editable
            foreach (GridColumn column in gridViewDevices.Columns)
            {
                bool isDataColumn = deviceDataTable.Columns.Contains(column.FieldName);

                if (isDataColumn && column.FieldName != "DeviceID" && column.FieldName != "ConnectionStatus")
                {
                    // Editable columns: Address, Machine Name, Plant Name, Department Name, IsActive
                    column.OptionsColumn.AllowEdit = isEditable;
                    column.OptionsColumn.ReadOnly = !isEditable;
                }
                else if (column.FieldName == "Action" || column.FieldName == "Cancel")
                {
                    // Action/Cancel button columns are always available for edit/click
                    column.OptionsColumn.AllowEdit = true;
                    column.OptionsColumn.ReadOnly = false;
                }
                else
                {
                    // Read-only columns: DeviceID, ConnectionStatus
                    column.OptionsColumn.AllowEdit = false;
                    column.OptionsColumn.ReadOnly = true;
                }
            }

            // Apply different back color for the row being edited
            gridViewDevices.OptionsView.EnableAppearanceOddRow = !isEditable;
            gridViewDevices.OptionsView.EnableAppearanceEvenRow = !isEditable;

            if (isEditable)
            {
                // Set appearance for the row being edited
                gridViewDevices.Appearance.FocusedRow.BackColor = Color.LightYellow;
                gridViewDevices.Appearance.FocusedRow.Options.UseBackColor = true;
            }
            else
            {
                // Revert to default
                gridViewDevices.Appearance.FocusedRow.BackColor = Color.Empty;
                gridViewDevices.Appearance.FocusedRow.Options.UseBackColor = false;
            }

            // Force refresh of row appearance
            gridViewDevices.LayoutChanged();
        }

        // Helper methods (kept mostly as-is, just adjusting data access)
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


        // ... (Remaining methods like SetWebSocketClient, GetDataAndLoadToGridAsync, WebSocket_OnMessage, SafeInvoke, ShowMessageBox, PopulateDeviceDataTable, InitializeDeviceDataTable, CreateButtonContainer, Button_Click, showRegisterDevice, RegisterControl_ExitClicked, RegisterForm_DeviceCreated, CreatelabelTotalControls, SyncButton_Click are kept similar)

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

                if (action != "getDevices") return;

                if (action?.Equals("getDevices") == true)
                {
                    var response = ResponseMessage<List<Device>>.FromJson(jsonData);

                    if (response?.Devices != null)
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
                            Thread.Sleep(10); // Simulate load time
                        }

                        SafeInvoke(() =>
                        {
                            PopulateDeviceDataTable(response.Devices);
                            CreatelabelTotalControls(response.Devices);
                            ApplyLocalization();
                        });
                    }
                    else
                    {
                        SafeInvoke(() =>
                            ShowMessageBox("No Data Found", "Info", MessageBoxIcon.Information));
                    }
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                SafeInvoke(() =>
                    ShowMessageBox($"JSON Deserialization Error: {jsonEx.Message}", "Error", MessageBoxIcon.Error));
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                    ShowMessageBox($"An error occurred: {ex.Message}", "Error", MessageBoxIcon.Error));
            }
            finally
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.CloseForm(false);
                }
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
            gridControlDevices.RefreshDataSource();
        }

        public DataTable InitializeDeviceDataTable()
        {
            var deviceDataTable = new DataTable("Devices");
            deviceDataTable.Columns.Add("DeviceID", typeof(int));
            deviceDataTable.Columns.Add("Address", typeof(string));
            deviceDataTable.Columns.Add("Machine Name", typeof(string));
            deviceDataTable.Columns.Add("Plant Name", typeof(string));
            deviceDataTable.Columns.Add("Department Name", typeof(string));
            deviceDataTable.Columns.Add("IsActive", typeof(bool));
            deviceDataTable.Columns.Add("ConnectionStatus", typeof(bool));

            foreach (DataColumn column in deviceDataTable.Columns)
            {
                column.ReadOnly = false;
            }

            return deviceDataTable;
        }

        private void CreateButtonContainer()
        {
            groupPanelButtonContainer = new Panel { Dock = DockStyle.Top, Height = 70 };
            Controls.Add(groupPanelButtonContainer);

            button = new SimpleButton { Text = LocalizationManager.GetString("AddNewDevice"), Size = new Size(150, 40), Location = new Point(10, 5) };
            groupPanelButtonContainer.Controls.Add(button);
            button.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterDevice();
        }

        private void showRegisterDevice()
        {
            gridControlDevices.Visible = false;
            frmRegister.Location = gridControlDevices.Location;
            frmRegister.Size = gridControlDevices.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
            _webSocketClient = WebSocketClient.Instance;
            frmRegister.SetWebSocketClient(_webSocketClient);
        }

        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            gridControlDevices.Visible = true;
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
            gridViewDevices.ClearSelection();
            if (gridViewDevices.RowCount > 0)
                gridViewDevices.SelectRow(0);
        }

        private void CreatelabelTotalControls(List<Device> devices)
        {
            if (paginationPanel != null)
            {
                this.Controls.Remove(paginationPanel);
                paginationPanel.Dispose();
            }

            paginationPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10),
                BackColor = Color.AliceBlue
            };

            lblPageInfo = new Label
            {
                Text = $"{LocalizationManager.GetString("TotalRecords")} {devices.Count}",
                AutoSize = true,
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(20, 10)
            };
            paginationPanel.Controls.Add(lblPageInfo);

            this.Controls.Add(paginationPanel);

            // Make sure grid stays above the bottom panel
            paginationPanel.SendToBack();

            // Re-order control Z-index to ensure grid is visible
            gridControlDevices.BringToFront();


            // Move Sync button to top panel
            SimpleButton syncButton = new SimpleButton()
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(150, 40),
                Location = new Point(180, 5),
                // Assuming Properties.Resources.sync_icon exists in your project
                // ImageOptions = { Image = Properties.Resources.sync_icon } 
            };
            syncButton.Click += SyncButton_Click;

            groupPanelButtonContainer.Controls.Add(syncButton);
        }

        private async void SyncButton_Click(object sender, EventArgs e)
        {
            if (!isEditing)
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