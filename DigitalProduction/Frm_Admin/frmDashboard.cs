using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction.Frm_Admin
{
    public partial class frmDashboard : Form
    {
        public BindingList<Device> Devices { get; set; } = new BindingList<Device>();
        private WebSocketClient _webSocketClient;
        private SynchronizationContext _syncContext;
        // Add at the top of the class
        private RealTimeCutMonitor _cutMonitor;
        public frmDashboard()
        {
            InitializeComponent();
            _cutMonitor = new RealTimeCutMonitor();
            _cutMonitor.OnNewActiveDevices += HandleCuttingUpdate;
            _cutMonitor.Start();
            windowsUIButtonPanel1.AllowGlyphSkinning = true;
            windowsUIButtonPanel1.AppearanceButton.Pressed.BackColor = Color.DarkGray;
            windowsUIButtonPanel1.UseButtonBackgroundImages = false;
            windowsUIButtonPanel1.ButtonInterval = 3;
            dateFrom.EditValue = DateTime.Today;
            dateTo.EditValue = DateTime.Today;
            loadDeviceDistribution();

            // Create a container Panel for top controls
            Panel topPanel = new Panel();
            topPanel.Height = 40;
            topPanel.Dock = DockStyle.Top;
            topPanel.BackColor = Color.Transparent;
            this.Controls.Add(topPanel); // Add before adding grid

            // Move gridDistribution down
            gridDistribution.Dock = DockStyle.Fill;

            // Add the delete button to this topPanel
            btnDeleteSelected.Text = LocalizedWithIcon("Delete", "🗑");
            btnDeleteSelected.AutoSize = true;
            btnDeleteSelected.BackColor = Color.LightCoral;
            btnDeleteSelected.ForeColor = Color.Red;
            btnDeleteSelected.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDeleteSelected.Cursor = Cursors.Hand;
            btnDeleteSelected.Anchor = AnchorStyles.Right;
            btnDeleteSelected.Click += btnDeleteSelected_Click;

            // select default
            SelectButtonByTag("DistributionManagement");
            windowsUIButtonPanel1.AllowGlyphSkinning = true;

            // allow mutilple select
            var view = gridDistribution.MainView as GridView;
            if (view != null)
            {
                view.OptionsSelection.MultiSelect = true;
                view.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
            }
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            SetWebSocketClient(null);
            gridViewDeviceManagement.OptionsBehavior.Editable = false;
            gridViewDistribution.OptionsBehavior.Editable = false;

            // --------------- set up gridViewDistribution and gridViewDeviceManagement ---------------//
            // Always show filter panel
            gridViewDistribution.OptionsView.ShowAutoFilterRow = true;
            gridViewDeviceManagement.OptionsView.ShowAutoFilterRow = true;

            // Hide completely
            gridViewDistribution.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;
            gridViewDeviceManagement.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;

            // Hide the "Drag a column here to group" panel
            gridViewDistribution.OptionsView.ShowGroupPanel = false;
            gridViewDeviceManagement.OptionsView.ShowGroupPanel = false;

            // Show the Find (search) panel
            gridViewDistribution.OptionsFind.AlwaysVisible = true;
            gridViewDeviceManagement.OptionsFind.AlwaysVisible = true;

            lblDateFrom.Text = LocalizationManager.GetString("StartDate");
            lblDateTo.Text = LocalizationManager.GetString("EndDate");
            lblInputSO.Text = LocalizationManager.GetString("InputSO");
            lblStatus.Text = LocalizationManager.GetString("Status");
            lblSelectDevice.Text = LocalizationManager.GetString("SelectMachine");
            btnFilter.Text = LocalizationManager.GetString("Filter");
        }
        private static string LocalizedWithIcon(string key, string icon) 
                    => $"{icon} {LocalizationManager.GetString(key)}";
        private void HandleCuttingUpdate(List<CutActivityInfo> activities)
        {
            _syncContext.Post(_ =>
            {
                foreach (var activity in activities)
                {
                    var device = Devices.FirstOrDefault(d => d.DeviceID == activity.DeviceID);
                    if (device != null)
                    {
                        device.LastCutTime = activity.UpdatedAt;
                        device.LastSize = activity.Size;
                        device.LastCutQty = activity.ActualSizeQty;
                        device.LastSizeQty = activity.SizeQty;
                        //device.RefreshCuttingStatus();

                        // ✅ Count only if last cut within 60s
                        if (activity.UpdatedAt.HasValue)
                        {
                            TimeSpan diff = DateTime.Now - activity.UpdatedAt.Value;
                            if (diff.TotalSeconds <= 60)
                            {
                                device.ActiveHoursToday += activity.DurationMinutes / 60.0;

                                // Save to DB
                                DbHelper.UpdateActiveHoursToday(device.DeviceID, device.ActiveHoursToday);
                            }
                        }
                    }
                }

                gridViewDeviceManagement.RefreshData();

            }, null);
        }


        private void WindowsUIButtonPanel1_ButtonClick(object sender, ButtonEventArgs e)
        {
            string tag = ((WindowsUIButton)e.Button).Tag.ToString();
            SelectButtonByTag(tag);
        }
        private void SelectButtonByTag(string tag)
        {
            foreach (WindowsUIButton btn in windowsUIButtonPanel1.Buttons)
            {
                if (btn is WindowsUIButton button)
                {
                    // Use localization to set default caption
                    string localizedCaption = LocalizationManager.GetString(button.Tag?.ToString() ?? "");
                    if (button.Tag != null && button.Tag.ToString() == tag)
                    {
                        button.Appearance.ForeColor = Color.SteelBlue;
                        button.Appearance.Options.UseForeColor = true;
                        button.Appearance.BackColor = Color.SteelBlue;
                        button.Appearance.ForeColor = Color.Green;
                        button.Appearance.Font = new Font(button.Appearance.Font, FontStyle.Bold);
                    }
                    else
                    {
                        button.Appearance.ForeColor = Color.SteelBlue;
                        button.Appearance.Options.UseForeColor = true;
                        button.Appearance.BackColor = Color.Transparent;
                        button.Appearance.ForeColor = Color.Black;
                        button.Appearance.Font = new Font(button.Appearance.Font, FontStyle.Regular);
                    }

                    // Active caption (localized + marker)
                    button.Caption = $"{localizedCaption}";
                }
            }
            // Navigate to corresponding page
            switch (tag)
            {
                case "DistributionManagement":
                    navigationFrame1.SelectedPage = navigationPage1;
                    break;
                case "CuttingManagement":
                    navigationFrame1.SelectedPage = navigationPage2;
                    break;
                case "Ad3":
                    navigationFrame1.SelectedPage = navigationPage3;
                    break;
            }
        }
        private void loadDeviceDistribution()
        {
            List<Device> machines = DbHelper.getlistMachines();
            machines.Insert(0, new Device { DeviceID = 0, MachineName = "" });
            cbxDevice.DataSource = machines;
            cbxDevice.DisplayMember = "MachineName";
            cbxDevice.ValueMember = "DeviceID";
            cbxDevice.SelectedIndex = 0;
            cbxDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void LoadDistributionData()
        {
            // Parse filters
            DateTime fromDate = Convert.ToDateTime(dateFrom.EditValue);
            DateTime toDate = Convert.ToDateTime(dateTo.EditValue).AddDays(1);
            int selectedDeviceId = Convert.ToInt32(cbxDevice.SelectedValue);
            string so = txtSO.Text.Trim();
            string status = cbx_Status.SelectedItem == null ? "" : cbx_Status.SelectedItem.ToString();

            // Get data from database
            List<DistributionModelView> data = DbHelper.GetDistributionData(fromDate, toDate, selectedDeviceId, so, status);

            // Bind to grid or other control
            gridDistribution.DataSource = data;

            // Subscribe to the RowStyle event
            gridViewDistribution.RowCellStyle += GridViewProgressManagement_RowCellStyle;
            gridViewDistribution.Columns["MaterialType"].Caption = LocalizationManager.GetString("MaterialType");
            // hide column specific
            HideGridColumns(gridDistribution ,"DistributionID", "UserID", "IsDelete", "IsLeather", "OperatorID", "PartSizeOrderID", "DeviceID", "PartID", "ProductID", "ActualSizeQty", "Note", "InventoryQty");
            TranslateHeaders(gridDistribution);
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            LoadDistributionData();
        }
        private void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            GridView view = gridDistribution.MainView as GridView;
            if (view == null) return;

            var selectedRows = view.GetSelectedRows();
            if (selectedRows.Length == 0)
            {
                MessageBox.Show("Please select at least one row to delete.");
                return;
            }

            List<int> idsToDelete = new List<int>();
            List<string> nonPendingStatuses = new List<string>();

            foreach (var rowHandle in selectedRows)
            {
                var row = view.GetRow(rowHandle);
                if (row is DistributionModelView item)
                {
                    if (item.Status == "Pending" || item.Status == "Complete")
                    {
                        idsToDelete.Add(item.DistributionID);
                    }
                    else
                    {
                        nonPendingStatuses.Add($"{item.SO} - {item.Status}");
                    }
                }
            }

            if (nonPendingStatuses.Count > 0)
            {
                MessageBox.Show("Only rows with status 'Pending' can be deleted.\n" +
                                "Skipped:\n" + string.Join("\n", nonPendingStatuses),
                                "Deletion Restricted",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }

            if (idsToDelete.Count == 0)
            {
                MessageBox.Show("No valid 'Pending' records selected to delete.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Are you sure you want to delete {idsToDelete.Count} 'Pending' record(s)?",
                                                  "Confirm Delete",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                DbHelper.DeleteDistributionAndDeviceOutput(idsToDelete);
                LoadDistributionData();
            }
        }

        private void gridViewDítribution_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e)
        {

        }

        private void HideGridColumns(DevExpress.XtraGrid.GridControl grid, params string[] columnNames)
        {
            GridView view = grid.MainView as GridView;
            if (view == null) return;

            foreach (string name in columnNames)
            {
                var column = view.Columns[name];
                if (column != null)
                {
                    column.Visible = false;
                }
            }
        }
        private void GridViewProgressManagement_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (view != null)
            {
                // Get the current row data
                DistributionModelView rowData = view.GetRow(e.RowHandle) as DistributionModelView;

                if (rowData != null && e.Column.FieldName == "Status")
                {
                    if (rowData.Status == "Complete")
                    {
                        e.Appearance.BackColor = Color.LightGreen;
                    }
                    else if (rowData.Status == "Pending")
                    {
                        e.Appearance.BackColor = Color.LightYellow;
                    }
                    else
                    {
                        e.Appearance.BackColor = Color.LightCoral;
                    }
                }
            }
        }
        private void TranslateHeaders(GridControl gridControl)
        {
            var gridView = gridControl.MainView as GridView;
            if (gridView == null || gridView.Columns.Count == 0)
                return;

            // 1. Translate columns by FieldName (default translation)
            foreach (GridColumn col in gridView.Columns)
            {
                string translatedText = LocalizationManager.GetString(col.FieldName);
                if (!string.IsNullOrEmpty(translatedText))
                {
                    col.Caption = translatedText;
                }
            }

            // 2. Special column translations
            var specialColumns = new Dictionary<string, string>
            {
                { "NoteReason", "Reason" },
                { "LastCutTime", "LastCutTime" },
                { "LastSize", "LastSizeID" },
                { "LastCutQty", "LastCutQty" },
                { "LastSizeQty", "LastSizeQty" },
                { "IsCutting", "IsCutting" }
            };

            foreach (var kvp in specialColumns)
            {
                var column = gridView.Columns.ColumnByFieldName(kvp.Key);
                if (column != null)
                {
                    string localizedText = LocalizationManager.GetString(kvp.Value);
                    if (!string.IsNullOrEmpty(localizedText))
                    {
                        column.Caption = localizedText;
                    }
                }
            }

            gridView.LayoutChanged();
        }

        // Naviagtion device list
        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= HandleWebSocketMessage; // Unsubscribe previous instance
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += HandleWebSocketMessage;
            _ = GetDataAndLoadToGridAsync();
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getDevices" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void HandleWebSocketMessage(string jsonData)
        {
            try
            {
                var item = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                string action = item.ContainsKey("action") ? item["action"]?.ToString() : null;

                if (action == "getDevices")
                {
                    var response = ResponseMessage<List<Device>>.FromJson(jsonData);
                    if (response?.Devices != null)
                    {
                        Devices = new BindingList<Device>(response.Devices);

                        _syncContext.Post(_ =>
                        {
                            gridControlDeviceManagement.DataSource = Devices;
                            gridViewDeviceManagement.BestFitColumns();

                            // Hide system columns
                            var columnsToHide = new List<string> { "EfficiencyPercent", "ActiveHoursToday", "PlantName", "IpAddress","DeviceID", "DepartmentID", "CreatedAt", "PlantID" , "IsActive"};
                            SetGridColumnVisibility(gridViewDeviceManagement, columnsToHide, false);

                            // add new column to overview cutting size
                            var lastCutTimeCol = gridViewDeviceManagement.Columns.ColumnByFieldName("LastCutTime");
                            if (lastCutTimeCol != null)
                            {
                                lastCutTimeCol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                                lastCutTimeCol.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
                            }

                            // Add efficiency column
                            var efficiencyCol = gridViewDeviceManagement.Columns.ColumnByFieldName("EfficiencyPercent");
                            if (efficiencyCol != null)
                            {
                                efficiencyCol.Caption = "Efficiency %";
                                efficiencyCol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                                efficiencyCol.DisplayFormat.FormatString = "P1"; // percentage with 1 decimal
                            }

                            // Add ActiveHours column
                            var activeHoursCol = gridViewDeviceManagement.Columns.ColumnByFieldName("ActiveHoursToday");
                            if (activeHoursCol != null)
                            {
                                activeHoursCol.Caption = "Active Hours";
                                activeHoursCol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                                activeHoursCol.DisplayFormat.FormatString = "N2"; // 2 decimals
                            }

                            TranslateHeaders(gridControlDeviceManagement);
                        }, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket message error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the visibility of specified columns in the gridViewDeviceManagement.
        /// </summary>
        /// <param name="gridView">The GridView where columns exist.</param>
        /// <param name="columnNames">List of column field names to show/hide.</param>
        /// <param name="isVisible">Whether the columns should be visible or hidden.</param>
        public void SetGridColumnVisibility(GridView gridView, IEnumerable<string> columnNames, bool isVisible)
        {
            foreach (var name in columnNames)
            {
                var column = gridView.Columns.ColumnByFieldName(name);
                if (column != null)
                    column.Visible = isVisible;
            }
        }
    }
}
