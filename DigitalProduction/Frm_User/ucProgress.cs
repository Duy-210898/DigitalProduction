using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Localization;
using DevExpress.XtraGrid.Menu;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using GridviewHelp;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucProgress : UserControl
    {
        private BindingList<Distribution> distributionDataList = new BindingList<Distribution>();
        private WebSocketClient _webSocketClient;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox noteComboBoxEditor;
        private Rectangle _reasonHeaderCheckBoxRect;
        private bool _reasonHeaderChecked = false;
        private int currentPage = 1;
        private int pageSize = 100;
        private int totalCount = 0;
        private ComboBoxEdit cmbPageSize;
        private SimpleButton btnPrev;
        private SimpleButton btnNext;
        private LabelControl lblPagingInfo;
        private Dictionary<int, List<SubDistribution>> _subDistributionCache = new Dictionary<int, List<SubDistribution>>();
        public ucProgress()
        {
            InitializeComponent();
            LoadTextLabel();
            this.Load += ucProgress_Load;
        }
        private void ucProgress_Load(object sender, EventArgs e)
        {
            dtpStartDate.EditValue = DateTime.Today;
            dtpEndDate.EditValue = DateTime.Today;
            // Make sure controls are created first
            InitializeControls();
            InitPagingFooter(gridProgressManagement);
        }

        private void InitializeControls()
        {
            // Sync Data button
            syncButton.ImageOptions.Image = Properties.Resources.sync_icon;
            syncButton.Text = LocalizationManager.GetString("Sync");
            syncButton.Click += async (sender, e) =>
            {
                try
                {
                    syncButton.Enabled = false;
                    syncButton.Text = "Loading...";

                    await Task.Run(async () => await GetDataAndLoadToGridAsync());

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    syncButton.Enabled = true;
                    syncButton.Text = LocalizationManager.GetString("Sync");
                }
            };

            dtpEndDate.EditValueChanged += DateTimePicker_ValueChanged;
            dtpStartDate.EditValueChanged += DateTimePicker_ValueChanged;

            btnApplyDevice.Click += BtnApplyDevice_Click;

            gridViewProgressManagement.OptionsBehavior.Editable = true;
            gridViewProgressManagement.OptionsView.ShowGroupPanel = false;
            gridProgressManagement.MainView = gridViewProgressManagement;

            // Set grid control columns
            ConfigureGridControl();

            // Subscribe to the RowStyle event
            gridViewProgressManagement.RowCellStyle += GridViewProgressManagement_RowCellStyle;
        }

        private void BtnApplyDevice_Click(object sender, EventArgs e)
        {
            if (gridLookUpDevice.EditValue == null || Convert.ToInt32(gridLookUpDevice.EditValue) == 0)
            {
                MessageBox.Show("Please select a valid device.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedDeviceId = Convert.ToInt32(gridLookUpDevice.EditValue);
            string selectedDeviceName = gridLookUpDevice.Text;

            int appliedCount = 0;

            for (int rowHandle = 0; rowHandle < gridViewProgressManagement.RowCount; rowHandle++)
            {
                var distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;
                if (distribution != null && distribution.Status == "Pending")
                {
                    bool success = DbHelper.UpdateDistributionDevice(distribution.DistributionID, selectedDeviceId);

                    if (success)
                    {
                        distribution.DeviceID = selectedDeviceId;
                        gridViewProgressManagement.SetRowCellValue(rowHandle, "DeviceID", selectedDeviceId);
                        appliedCount++;
                    }
                }
            }

            MessageBox.Show(
                appliedCount > 0
                    ? $"Device '{selectedDeviceName}' applied to {appliedCount} row(s) with status 'Pending'."
                    : "No 'Pending' rows found in the grid.",
                "Result",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void loadDeviceDistribution()
        {
            List<Device> machines = DbHelper.getlistMachines();

            gridLookUpDevice.Properties.DataSource = machines;
            gridLookUpDevice.Properties.DisplayMember = "MachineName";
            gridLookUpDevice.Properties.ValueMember = "DeviceID";

            // Optional: Disable typing if you want DropDownList style
            gridLookUpDevice.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            // Hide all columns except "MachineName"
            gridLookUpDevice.Properties.View.Columns.Clear();
            gridLookUpDevice.Properties.PopulateViewColumns();
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in gridLookUpDevice.Properties.View.Columns)
            {
                column.Visible = column.FieldName == "MachineName";
            }
            // Enable autocomplete & search
            gridLookUpDevice.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            gridLookUpDevice.Properties.AutoComplete = true;

            // Enable incremental search
            gridLookUpDevice.Properties.ImmediatePopup = true;
            gridLookUpDevice.Properties.PopupFilterMode = DevExpress.XtraEditors.PopupFilterMode.Contains;
            gridLookUpDevice.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            gridLookUpDevice.Properties.NullText = "";

            // Filter mode
            gridLookUpDevice.Properties.View.OptionsView.ShowAutoFilterRow = true;
            gridLookUpDevice.Properties.View.ActiveFilterEnabled = true;
        }


        private void ConfigureGridControl()
        {
            // Clear existing columns
            gridViewProgressManagement.Columns.Clear();
            gridViewProgressManagement.Appearance.HeaderPanel.Font = new Font(gridViewProgressManagement.Appearance.Row.Font, FontStyle.Bold);
            gridViewProgressManagement.Appearance.HeaderPanel.BackColor = Color.AntiqueWhite;
            gridViewProgressManagement.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridViewProgressManagement.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Set data source
            gridProgressManagement.DataSource = distributionDataList;

            // Đánh số thứ tự
            gridViewProgressManagement.CustomDrawRowIndicator += (s, e) => { GridViewHelper.GridView_CustomDrawRowIndicator(s, e, gridProgressManagement, gridViewProgressManagement); };
            // thêm menu vào gridview
            gridViewProgressManagement.PopupMenuShowing += (s, e) => { GridViewHelper.AddFontAndColortoPopupMenuShowing(s, e, gridProgressManagement, this.Name); };
            // Hide the "Drag a column here to group" panel
            gridViewProgressManagement.OptionsView.ShowGroupPanel = false;

            // Show the Find (search) panel
            gridViewProgressManagement.OptionsFind.AlwaysVisible = true;

            this.Load += (s, e) =>
            {
                GridViewHelper.SaveAndRestoreLayout(gridProgressManagement, this.Name);
            };

            GridViewHelper.CustomizeGroupText(gridViewProgressManagement);
            gridViewProgressManagement.BestFitColumns();
            gridViewProgressManagement.Columns["OperatorName"].VisibleIndex = 2;
            //gridViewProgressManagement.Columns["SO"].Width += 50;
            var memoEdit = new DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit();
            gridProgressManagement.RepositoryItems.Add(memoEdit);

            foreach (GridColumn column in gridViewProgressManagement.Columns)
            {
                column.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                column.ColumnEdit = memoEdit;
            }

            //gridViewProgressManagement.CustomDrawGroupRow += gridViewProgressManagement_CustomDrawGroupRow;
        }
        //private void gridViewProgressManagement_CustomDrawGroupRow(object sender, RowObjectCustomDrawEventArgs e)
        //{
        //    var view = sender as GridView;
        //    if (view == null) return;

        //    var info = e.Info as GridGroupRowInfo;
        //    if (info == null) return;

        //    int groupRowHandle = e.RowHandle;

        //    // ✅ Get the first child row in this group
        //    int childHandle = view.GetChildRowHandle(groupRowHandle, 0);
        //    if (childHandle < 0) return;

        //    // ✅ Get your data object
        //    var data = view.GetRow(childHandle) as DeviceOutput;
        //    if (data == null) return;

        //    // ✅ Build group display text
        //    string partName = data.PartName;
        //    string status = view.GetRowCellDisplayText(childHandle, view.Columns["Status"]);            // Or map status from code to text

        //    info.GroupText = $"PartName: {partName} - Status: {status}";

        //    e.Painter.DrawObject(e.Info);
        //    e.Handled = true;
        //}

        private void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            DateTime newStartDate = dtpStartDate.DateTime;
            DateTime newEndDate = dtpEndDate.DateTime;

            if (newStartDate > newEndDate)
            {
                AdjustDates(sender, ref newStartDate, ref newEndDate);
                ShowInvalidDateMessage();
            }
            else
            {
                FilterData(newStartDate, newEndDate);
            }
        }

        private void AdjustDates(object sender, ref DateTime newStartDate, ref DateTime newEndDate)
        {
            if (sender == dtpEndDate)
            {
                dtpEndDate.EditValueChanged -= DateTimePicker_ValueChanged;
                newStartDate = newEndDate;
                dtpEndDate.EditValue = newStartDate;
                dtpEndDate.EditValueChanged += DateTimePicker_ValueChanged;
            }
            else if (sender == dtpStartDate)
            {
                dtpStartDate.EditValueChanged -= DateTimePicker_ValueChanged;
                newEndDate = newStartDate;
                dtpStartDate.EditValue = newEndDate;
                dtpStartDate.EditValueChanged += DateTimePicker_ValueChanged;
            }
        }

        private void ShowInvalidDateMessage()
        {
            MessageBox.Show("Invalid date range! Start date cannot be after End date.", "Date Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void FilterData(DateTime startDate, DateTime endDate)
        {
            Task.Run(async () => await GetDataAndLoadToGridAsync());
            endDate = endDate.AddDays(1).AddTicks(-1);

            var filteredData = new BindingList<Distribution>(
                distributionDataList.Where(distribution =>
                    distribution.CreatedAt >= startDate &&
                    distribution.CreatedAt <= endDate
                ).ToList()
            );

            _ = UpdateGridControlAsync(filteredData);
        }


        private async Task UpdateGridControlAsync(BindingList<Distribution> filteredData)
        {
            if (this.InvokeRequired)
            {
                _ = this.Invoke(new Action(async () => await UpdateGridControlAsync(filteredData)));
                return;
            }
            _subDistributionCache.Clear();

            // 2. Preload all SubDistributions into the cache
            foreach (var distribution in filteredData)
            {
                var subList = await DbHelper.GetSubDistributions(distribution.DistributionID);
                _subDistributionCache[distribution.DistributionID] = subList;
            }

            gridProgressManagement.DataSource = filteredData;
            TranslateHeaders();
            // lblPageInfo.Text = $"{LocalizationManager.GetString("TotalRecords")} {filteredData.Count}";
        }

        private bool _isDataLoaded = false;

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage;
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            if (!_isDataLoaded)
            {
                _ = GetDataAndLoadToGridAsync();
            }
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            if (dtpStartDate == null || dtpEndDate == null || _webSocketClient == null)
            {
                MessageBox.Show("Required controls or WebSocket client not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pageNumber = currentPage;
            int selectedPageSize = 100;

            if (cmbPageSize?.SelectedItem != null && int.TryParse(cmbPageSize.SelectedItem.ToString(), out int parsedSize))
                selectedPageSize = parsedSize;

            var request = new
            {
                app = Global.App,
                action = "getDistributions",
                filter = new
                {
                    startDate = dtpStartDate.DateTime.ToString("yyyy-MM-dd"),
                    endDate = dtpEndDate.DateTime.ToString("yyyy-MM-dd"),
                    pageNumber = pageNumber,
                    pageSize = selectedPageSize
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(request);

            try
            {
                string response = await _webSocketClient.SendAsync(jsonRequest);
                if (response != null)
                {
                    if (string.IsNullOrEmpty(response))
                        WebSocket_OnMessage(response);
                    _isDataLoaded = true;
                }
                else
                {

                    //  MessageBox.Show("InvalidOperation or No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //ConnectionManager.Instance.IsReconnecting = true;
                    // Retry logic or callback method
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR EX => " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void WebSocket_OnMessage(string jsonData)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(WebSocket_OnMessage), jsonData);
                return;
            }

            var parentForm = this.FindForm(); // Ensure this is within a UserControl or Form

            try
            {
                if (parentForm != null)
                {
                    SplashScreenManager.ShowForm(parentForm, typeof(frmLoading), true, true, false);
                }

                // Simulate progress (optional)
                for (int i = 1; i <= 100; i += 20)
                {
                    if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                    {
                        SplashScreenManager.Default.SetWaitFormDescription($"Loading... {i}%");
                    }
                    await Task.Delay(10); // async delay, avoid blocking UI thread
                }

                var response = ResponseMessage<List<Distribution>>.FromJson(jsonData);
                Console.WriteLine(jsonData);

                if (response?.DistributionData?.Any() == true)
                {
                    distributionDataList.Clear();

                    foreach (var distribution in response.DistributionData)
                    {
                        if (distribution != null)
                            distributionDataList.Add(distribution);
                    }

                    _ = UpdateGridControlAsync(new BindingList<Distribution>(distributionDataList));
                    loadDeviceDistribution();
                }
                else
                {
                    Console.WriteLine("No Data Found or DistributionData is null");
                }
                totalCount = response.TotalCount;
                UpdatePagingLabel();
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Invalid JSON format: {jsonEx.Message}", "JSON Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving WebSocket data:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.CloseForm(false);
                }
            }
        }

        // Implement the RowStyle event handler
        private void GridViewProgressManagement_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            var view = sender as GridView;
            if (view != null)
            {
                // Get the current row data
                var rowData = view.GetRow(e.RowHandle) as Distribution;

                // Ensure rowData is valid and the column is "Status"
                if (rowData != null && e.Column.FieldName == "Status")
                {
                    // Customize only the "Status" column background color
                    if (rowData.Status == "Complete")
                    {
                        e.Appearance.BackColor = Color.LightGreen; // Green for complete
                    }
                    else if (rowData.Status == "Pending")
                    {
                        e.Appearance.BackColor = Color.LightYellow; // Yellow for pending
                    }
                    else
                    {
                        e.Appearance.BackColor = Color.LightCoral; // Red for other statuses
                    }
                }
            }
        }


        private void TranslateHeaders()
        {
            if (gridProgressManagement.MainView is GridView gridView && gridView.Columns.Count > 0)
            {
                foreach (GridColumn col in gridView.Columns)
                {
                    string translatedText = LocalizationManager.GetString(col.FieldName);
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        col.Caption = translatedText;
                    }
                }
                gridView.LayoutChanged(); // Force update to reflect changes
            }
            gridViewProgressManagement.Columns["DeviceID"].Visible = false;
            gridViewProgressManagement.Columns["Note"].Visible = false;
            gridViewProgressManagement.Columns["NoteReason"].Visible = false;
            gridViewProgressManagement.Columns["IpAddress"].Visible = false;
            gridViewProgressManagement.Columns["DistributionID"].Visible = false;
            gridViewProgressManagement.Columns["IsLeather"].Visible = false;
            gridViewProgressManagement.Columns["MaterialName"].Visible = false;
            gridViewProgressManagement.Columns["EmployeeName"].Visible = false;
            gridViewProgressManagement.Columns["CreatedAt"].Visible = false;
            gridViewProgressManagement.Columns["UpdatedAt"].Visible = false;
            gridViewProgressManagement.Columns["MaterialType"].Caption = LocalizationManager.GetString("MaterialType");

            gridViewProgressManagement.Columns["SO"].Width = 130;
            gridViewProgressManagement.Columns["MachineName"].Width = 110;
            gridViewProgressManagement.Columns["OperatorName"].Width = 150;
            gridViewProgressManagement.Columns["PartName"].Width = 130;

            gridViewProgressManagement.PopupMenuShowing += (s, e) =>
            {
                if (e.MenuType == GridMenuType.Column)
                {
                    GridViewColumnMenu menu = e.Menu as GridViewColumnMenu;
                    foreach (DXMenuItem item in menu.Items)
                    {
                        item.Caption = LocalizationManager.GetString(item.Caption);
                    }
                }
            };

            var existedNoted = gridViewProgressManagement.Columns.ColumnByFieldName(LocalizationManager.GetString("Reason"));
            if (existedNoted == null)
            {
                // Initialize 
                noteComboBoxEditor = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
                Dictionary<int, string> noteDescriptions = new Dictionary<int, string>
                {
                    { 0, "1 - " + LocalizationManager.GetString("NotEnoughMaterials") },
                    { 1, "2 - " + LocalizationManager.GetString("ChangeOfPlan") },
                    { 2, "3 - " + LocalizationManager.GetString("ForgotToChooseSize") }
                };
                noteComboBoxEditor.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
                foreach (var pair in noteDescriptions)
                {
                    noteComboBoxEditor.Items.Add(pair.Value);
                }
                // Initialize the GridColumn
                GridColumn noteColumn = new GridColumn
                {
                    FieldName = LocalizationManager.GetString("Reason"),
                    Visible = true
                };
                noteColumn.ColumnEdit = noteComboBoxEditor;
                noteColumn.OptionsColumn.AllowEdit = true;

                // Add the column to the grid view
                gridViewProgressManagement.Columns.Add(noteColumn);
                gridViewProgressManagement.ShowingEditor += GridViewProgressManagement_ShowingEditor;
                gridProgressManagement.Refresh();

                noteColumn.UnboundType = DevExpress.Data.UnboundColumnType.String;
                gridViewProgressManagement.CustomUnboundColumnData += (s, e) =>
                {
                    if (e.Column.FieldName == LocalizationManager.GetString("Reason"))
                    {
                        var model = (Distribution)e.Row;
                        if (e.IsGetData)
                        {
                            if (model.Note.HasValue && noteDescriptions.TryGetValue(model.Note.Value, out string description))
                            {
                                e.Value = description;
                            }
                        }
                        else if (e.IsSetData)
                        {
                            var desc = e.Value?.ToString();
                            var match = noteDescriptions.FirstOrDefault(x => x.Value == desc);
                            model.Note = match.Key;
                        }
                    }
                };
                gridViewProgressManagement.CustomDrawColumnHeader += GridViewProgressManagement_CustomDrawColumnHeader;
                gridViewProgressManagement.MouseDown += GridViewProgressManagement_MouseDown;

                gridViewProgressManagement.ShownEditor += (s, e) =>
                {
                    if (gridViewProgressManagement.FocusedColumn.FieldName == LocalizationManager.GetString("Reason"))
                    {
                        int rowHandle = gridViewProgressManagement.FocusedRowHandle;
                        Distribution distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;

                        // Check if the distribution object exists and its status is not "Complete"
                        if (distribution != null && distribution.Status != "Complete")
                        {
                            ComboBoxEdit editor = gridViewProgressManagement.ActiveEditor as ComboBoxEdit;
                            if (editor != null)
                            {
                                // Handle the SelectedIndexChanged event
                                editor.SelectedIndexChanged += (s2, e2) =>
                                {
                                    int selectedIndex = editor.SelectedIndex;
                                    if (noteDescriptions.TryGetValue(selectedIndex, out string selectedDesc))
                                    {
                                        int distributionId = distribution.DistributionID;

                                        // update status distribution
                                        DbHelper.UpdateDistributionNoteAndStatus(distributionId, selectedIndex, "Stop");
                                        Console.WriteLine($"Selected Note Key: {selectedIndex}, Description: {selectedDesc}, DistributionID: {distributionId}");
                                    }
                                };
                            }
                        }
                        else
                        {
                            // not allow eduit when status complete
                            gridViewProgressManagement.HideEditor();
                            Console.WriteLine("Editing is disabled for rows with status 'Complete'.");
                        }
                    }
                };

                // Handle the ShowingEditor event to conditionally enable or disable editing for the Inventory column
                gridViewProgressManagement.ShowingEditor += (s, e) =>
                {
                    if (gridViewProgressManagement.FocusedColumn.FieldName == "InventoryQty")
                    {
                        int rowHandle = gridViewProgressManagement.FocusedRowHandle;
                        Distribution distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;

                        if (distribution != null)
                        {
                            // Enable editing for Inventory column only if the status is "Stop"
                            if (distribution.Status != "Stop")
                            {
                                // Disable the editor if the status is not "Stop"
                                e.Cancel = true;
                                Console.WriteLine("Editing is disabled for Inventory column when status is not 'Stop'.");
                            }
                            else
                            {
                                e.Cancel = false;
                            }
                        }
                    }
                };

                // Handle the editor's value change event for Inventory column if needed
                gridViewProgressManagement.CellValueChanged += (s, e) =>
                {
                    if (e.Column.FieldName == "InventoryQty")
                    {
                        int rowHandle = e.RowHandle;
                        Distribution distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;

                        if (distribution != null)
                        {
                            if (distribution.InventoryQty + distribution.ActualSizeQty < distribution.SizeQty)
                            {
                                DbHelper.UpdateInventoryQty(distribution.DistributionID, Convert.ToInt32(e.Value), "Stop");
                                Console.WriteLine($"Updated InventoryQty for DistributionID: {distribution.DistributionID} to {e.Value}");
                            }
                            else if (distribution.InventoryQty + distribution.ActualSizeQty == distribution.SizeQty)
                            {
                                DbHelper.UpdateInventoryQty(distribution.DistributionID, Convert.ToInt32(e.Value), "Complete");
                                Console.WriteLine($"Updated InventoryQty for DistributionID: {distribution.DistributionID} to {e.Value}");
                            }
                            else
                            {
                                Console.WriteLine($"Can't update InventoryQty for DistributionID: {distribution.DistributionID} to {e.Value}");
                            }
                        }
                    }
                };
            }
            // 1. Ensure button visibility mode is set
            //gridViewProgressManagement.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;

            //// 2. Create unbound column
            //GridColumn colDetail = new GridColumn
            //{
            //    Caption = "Detail",
            //    Name = "colDetail",
            //    FieldName = "btnDetail",
            //    UnboundType = DevExpress.Data.UnboundColumnType.String,
            //    Visible = true,
            //    Width = 100
            //};

            //// 3. Create the button editor
            //var btnDetail = new RepositoryItemButtonEdit
            //{
            //    TextEditStyle = TextEditStyles.HideTextEditor
            //};

            //btnDetail.Buttons.Clear();
            //btnDetail.Buttons.Add(new EditorButton(ButtonPredefines.Glyph)
            //{
            //    Caption = "Show Detail", // Show this text on the button
            //    Kind = ButtonPredefines.Glyph
            //});


            //// 4. Add repository to the grid
            //gridProgressManagement.RepositoryItems.Add(btnDetail);
            //colDetail.ColumnEdit = btnDetail;

            //// 5. Add the column if it doesn't exist
            //if (gridViewProgressManagement.Columns.ColumnByFieldName("btnDetail") == null)
            //{
            //    gridViewProgressManagement.Columns.Add(colDetail);
            //}
            // Create detail view
            gridViewProgressManagement.OptionsDetail.EnableMasterViewMode = true;
            gridViewProgressManagement.OptionsDetail.ShowDetailTabs = false;
            gridViewProgressManagement.OptionsDetail.AllowOnlyOneMasterRowExpanded = true;

            // Register level
            gridProgressManagement.LevelTree.Nodes.Clear(); // optional, clean old levels
            gridProgressManagement.LevelTree.Nodes.Add("SubDistributions", CreateSubDistributionView());

            gridViewProgressManagement.MasterRowGetRelationCount += (s, e) =>
            {
                e.RelationCount = 1;
            };

            gridViewProgressManagement.MasterRowGetRelationName += (s, e) =>
            {
                e.RelationName = "SubDistributions";
            };

            gridViewProgressManagement.MasterRowEmpty += (s, e) =>
            {
                var view = s as GridView;
                Distribution masterRow = view.GetRow(e.RowHandle) as Distribution;

                if (masterRow == null || !_subDistributionCache.TryGetValue(masterRow.DistributionID, out var subList) || subList.Count == 0)
                {
                    e.IsEmpty = true;  // Hide "+"
                }
                else
                {
                    e.IsEmpty = false; // Show "+"
                }
            };

            gridViewProgressManagement.MasterRowGetChildList += (s, e) =>
            {
                var view = s as GridView;
                Distribution masterRow = view.GetRow(e.RowHandle) as Distribution;

                if (masterRow != null &&
                    _subDistributionCache.TryGetValue(masterRow.DistributionID, out var subList))
                {
                    e.ChildList = subList;
                }
                else
                {
                    e.ChildList = null;
                }
            };
            gridViewProgressManagement.RefreshData();
            //gridViewProgressManagement.RowCellClick += (s, e) =>
            //{
            //    if (e.Column.FieldName == "btnDetail")
            //    {
            //        int rowHandle = e.RowHandle;
            //        if (rowHandle >= 0)
            //        {
            //            var distributionID = Convert.ToInt32(gridViewProgressManagement.GetRowCellValue(rowHandle, "DistributionID"));
            //            if (_subDistributionCache.TryGetValue(distributionID, out var hasSub) && hasSub)
            //            {
            //                ShowSubDistributionDetails(distributionID);
            //            }
            //        }
            //    }
            //};
            // 6. Set data for unbound column
            //gridViewProgressManagement.CustomUnboundColumnData += (s, e) =>
            //{
            //    if (e.IsGetData && e.Column.FieldName == "btnDetail")
            //    {
            //        var distributionID = Convert.ToInt32(gridViewProgressManagement.GetListSourceRowCellValue(e.ListSourceRowIndex, "DistributionID"));
            //        e.Value = _subDistributionCache.TryGetValue(distributionID, out var hasSub) && hasSub ? "Show Detail" : "";
            //    }
            //};

            //// 7. Conditionally show or hide the button
            //gridViewProgressManagement.CustomRowCellEdit += (s, e) =>
            //{
            //    if (e.Column.FieldName == "btnDetail")
            //    {
            //        int distributionID = Convert.ToInt32(gridViewProgressManagement.GetRowCellValue(e.RowHandle, "DistributionID"));
            //        if (_subDistributionCache.TryGetValue(distributionID, out var hasSub) && hasSub)
            //        {
            //            e.RepositoryItem = btnDetail;
            //        }
            //        else
            //        {
            //            e.RepositoryItem = null;
            //        }
            //    }
            //};

        }
        private GridView CreateSubDistributionView()
        {
            string GetTranslation(string key)
            {
                return LocalizationManager.GetString(key); // Or your custom method
            }

            GridView detailView = new GridView(gridProgressManagement)
            {
                ViewCaption = "Sub-Distributions"
            };
            detailView.OptionsView.ShowGroupPanel = false;
            detailView.OptionsBehavior.Editable = true;

            detailView.Columns.AddVisible("MachineName", GetTranslation("MachineName"));
            detailView.Columns.AddVisible("OperatorName", GetTranslation("OperatorName"));
            detailView.Columns.AddVisible("SizeQty", GetTranslation("SizeQty"));
            detailView.Columns.AddVisible("Status", GetTranslation("Status"));
            detailView.Columns.AddVisible("InventoryQty", GetTranslation("InventoryQty"));

            detailView.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Status")
                {
                    string status = e.CellValue?.ToString();

                    if (status == "Complete")
                    {
                        e.Appearance.BackColor = Color.LightGreen;
                    }
                    else if (status == "Pending")
                    {
                        e.Appearance.BackColor = Color.LightYellow;
                    }
                    else if (status == "Stop")
                    {
                        e.Appearance.BackColor = Color.LightCoral;
                    }
                }
            };

            return detailView;
        }


        private void GridViewProgressManagement_CustomDrawColumnHeader(object sender, ColumnHeaderCustomDrawEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == LocalizationManager.GetString("Reason"))
            {
                e.Info.InnerElements.Clear();
                e.Painter.DrawObject(e.Info);
                e.Handled = true;

                // Draw checkbox
                _reasonHeaderCheckBoxRect = new Rectangle(e.Bounds.X + e.Bounds.Width - 20, e.Bounds.Y + 5, 15, 15);
                ButtonState state = _reasonHeaderChecked ? ButtonState.Checked : ButtonState.Normal;
                ControlPaint.DrawCheckBox(e.Graphics, _reasonHeaderCheckBoxRect, state);
            }
        }

        // mode stop or pending
        private void GridViewProgressManagement_MouseDown(object sender, MouseEventArgs e)
        {

            if (_reasonHeaderCheckBoxRect.Contains(e.Location))
            {
                _reasonHeaderChecked = !_reasonHeaderChecked;
                gridViewProgressManagement.InvalidateColumnHeader(gridViewProgressManagement.Columns[LocalizationManager.GetString("Reason")]);
                for (int i = 0; i < gridViewProgressManagement.RowCount; i++)
                {
                    var row = gridViewProgressManagement.GetRow(i) as Distribution;
                    if (row != null && row.Status != "Complete")
                    {
                        row.Note = _reasonHeaderChecked ? 1 : (int?)null;
                        row.Status = _reasonHeaderChecked ? "Stop" : "Pending";
                        DbHelper.UpdateDistributionNoteAndStatus(row.DistributionID, row.Note ?? -1, row.Status);
                    }
                }
                gridViewProgressManagement.RefreshData();
            }
        }

        private void GridViewProgressManagement_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GridView view = sender as GridView;

            // Check if the current column is the noteColumn
            if (view.FocusedColumn.FieldName == LocalizationManager.GetString("Reason"))
            {
                e.Cancel = false; // Allow editing if the focused column is "Note"
            }
            else
            {
                e.Cancel = true; // Prevent editing for all other columns
            }
        }
        private void LoadTextLabel()
        {
            lblFilterDate.Text = LocalizationManager.GetString("FilterDate");
            btnApplyDevice.Text = LocalizationManager.GetString("TransferDevice");
            this.Text = LocalizationManager.GetString("ListOfDistributions");
        }
        private void InitPagingFooter(Control gridControl)
        {
            var pagingPanel = new DevExpress.XtraEditors.PanelControl
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };

            cmbPageSize = new DevExpress.XtraEditors.ComboBoxEdit
            {
                Width = 70,
                Location = new Point(10, 8)
            };
            cmbPageSize.Properties.Items.AddRange(new object[] { 50, 100, 200, 500 });
            cmbPageSize.SelectedIndexChanged += async (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int newSize))
                {
                    pageSize = newSize;
                    currentPage = 1;
                    await LoadCurrentPageAsync();
                }
            };
            cmbPageSize.SelectedIndex = 1;
            pagingPanel.Controls.Add(cmbPageSize);

            btnNext = new DevExpress.XtraEditors.SimpleButton
            {
                Text = LocalizationManager.GetString("Next") + " »",
                Location = new Point(cmbPageSize.Right + 10, 6)
            };
            btnNext.Click += async (s, e) =>
            {
                if (currentPage < GetTotalPages())
                {
                    currentPage++;
                    await LoadCurrentPageAsync();
                }
            };
            pagingPanel.Controls.Add(btnNext);

            btnPrev = new DevExpress.XtraEditors.SimpleButton
            {
                Text = "« " + LocalizationManager.GetString("Previous"),
                Location = new Point(btnNext.Right + 10, 6)
            };
            btnPrev.Click += async (s, e) =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    await LoadCurrentPageAsync();
                }
            };
            pagingPanel.Controls.Add(btnPrev);

            lblPagingInfo = new DevExpress.XtraEditors.LabelControl
            {
                Text = LocalizationManager.GetString("Page") + "0 / 0",
                Location = new Point(btnPrev.Right + 20, 10)
            };
            pagingPanel.Controls.Add(lblPagingInfo);

            gridControl.Controls.Add(pagingPanel);
            pagingPanel.BringToFront();
        }
        private async Task LoadCurrentPageAsync()
        {
            SetWebSocketClient(WebSocketClient.Instance);
            await GetDataAndLoadToGridAsync();
            UpdatePagingLabel();
            // return Task.CompletedTask;
        }

        private int GetTotalPages()
        {
            return (int)Math.Ceiling((double)totalCount / pageSize);
        }

        private void UpdatePagingLabel()
        {
            int totalPages = GetTotalPages();
            int start = (currentPage - 1) * pageSize + 1;
            int end = Math.Min(currentPage * pageSize, totalCount);

            if (lblPagingInfo != null)
            {
                lblPagingInfo.Text = $" {LocalizationManager.GetString("Page")} {currentPage} / {totalPages} ({LocalizationManager.GetString("TotalRecords")} {totalCount} rows)";
            }

            gridViewProgressManagement.OptionsView.ShowFooter = true;
            gridViewProgressManagement.Columns[0].SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
            gridViewProgressManagement.Columns[0].SummaryItem.DisplayFormat = $"Showing {start}–{end} of {totalCount}";
        }

        public class Distribution
        {
            public int DistributionID { get; set; }
            public string SO { get; set; }
            public int? DeviceID { get; set; }
            public string IpAddress { get; set; }
            public string MachineName { get; set; }
            public string PartName { get; set; }
            public string VietnameseName { get; set; }
            public string Size { get; set; }
            public string Unit { get; set; }
            // public double UnitUsage { get; set; }
            public int SizeQty { get; set; }
            public string MaterialName { get; set; }
            public string OperatorName { get; set; }
            public string EmployeeName { get; set; }
            public int? ActualSizeQty { get; set; }
            public int InventoryQty { get; set; }
            public string Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public bool IsLeather { get; set; }
            // New read-only property
            public string MaterialType => IsLeather ? LocalizationManager.GetString("leatherMaterial") : LocalizationManager.GetString("rawMaterial");

            public int? Note { get; set; }

            public string NoteReason
            {
                get
                {
                    if (!Note.HasValue)
                        return string.Empty;

                    switch (Note.Value)
                    {
                        case 0:
                            return "1 - " + LocalizationManager.GetString("NotEnoughMaterials");
                        case 1:
                            return "2 - " + LocalizationManager.GetString("ChangeOfPlan");
                        case 2:
                            return "3 - " + LocalizationManager.GetString("ForgotToChooseSize");
                        default:
                            return string.Empty;
                    }
                }
            }

        }
    }
}