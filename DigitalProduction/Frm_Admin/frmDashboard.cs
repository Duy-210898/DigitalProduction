using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;
using static DigitalProduction.ucProgress;

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
            Button btnDeleteSelected = new Button();
            btnDeleteSelected.Text = "🗑 Delete Selected";
            btnDeleteSelected.AutoSize = true;
            btnDeleteSelected.BackColor = Color.LightCoral;
            btnDeleteSelected.ForeColor = Color.White;
            btnDeleteSelected.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDeleteSelected.FlatStyle = FlatStyle.Flat;
            btnDeleteSelected.FlatAppearance.BorderSize = 0;
            btnDeleteSelected.Cursor = Cursors.Hand;
            btnDeleteSelected.Anchor = AnchorStyles.Right;
            btnDeleteSelected.Click += btnDeleteSelected_Click;

            btnDeleteSelected.Dock = DockStyle.Right;
            topPanel.Controls.Add(btnDeleteSelected);

            // select default
            SelectButtonByTag("Ad1");
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
        }
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
                        device.LastCutQty = activity.ActualCut;
                        device.LastSizeQty = activity.SizeQty;
                        device.RefreshCuttingStatus();
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
                }
            }
            // Navigate to corresponding page
            switch (tag)
            {
                case "Ad1":
                    navigationFrame1.SelectedPage = navigationPage1;
                    break;
                case "Ad2":
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
            List<Distribution> data = DbHelper.GetDistributionData(fromDate, toDate, selectedDeviceId, so, status);

            // Bind to grid or other control
            gridDistribution.DataSource = data;

            // Subscribe to the RowStyle event
            gridViewDítribution.RowCellStyle += GridViewProgressManagement_RowCellStyle;
            gridViewDítribution.Columns["MaterialType"].Caption = LocalizationManager.GetString("MaterialType");
            // hide column specific
            HideGridColumns(gridDistribution, "DistributionID", "UserID", "IsDelete", "IsLeather", "OperatorID", "PartSizeOrderID", "DeviceID", "PartID", "ProductID", "ActualSizeQty", "Note", "InventoryQty");
            TranslateHeaders();
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
                if (row is Distribution item)
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
                Distribution rowData = view.GetRow(e.RowHandle) as Distribution;

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
        private void TranslateHeaders()
        {
            if (gridDistribution.MainView is GridView gridView && gridView.Columns.Count > 0)
            {
                foreach (GridColumn col in gridView.Columns)
                {
                    string translatedText = LocalizationManager.GetString(col.FieldName);
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        col.Caption = translatedText;
                    }
                }
                gridViewDítribution.Columns["NoteReason"].Caption = LocalizationManager.GetString("Reason");
                gridView.LayoutChanged();
            }
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
                            var columnsToHide = new List<string> { "DeviceID", "DepartmentID", "CreatedAt", "PlantID" };
                            SetGridColumnVisibility(gridViewDeviceManagement, columnsToHide, false);

                            // add new column to overview cutting size
                            var lastCutTimeCol = gridViewDeviceManagement.Columns.ColumnByFieldName("LastCutTime");
                            if (lastCutTimeCol != null)
                            {
                                lastCutTimeCol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                                lastCutTimeCol.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
                            }
                            var lastSizeCol = gridViewDeviceManagement.Columns["LastSizeID"];
                            if (lastSizeCol != null)
                            {
                                lastSizeCol.Caption = "Size ID";
                            }

                            var qtyCol = gridViewDeviceManagement.Columns["LastCutQty"];
                            if (qtyCol != null)
                            {
                                qtyCol.Caption = "Cut Qty";
                                qtyCol.AppearanceCell.BackColor = Color.LightYellow;
                            }
                            var sizeQtyCol = gridViewDeviceManagement.Columns["LastSizeQty"];
                            if (sizeQtyCol != null)
                            {
                                sizeQtyCol.Caption = "Size Qty";
                            }

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
