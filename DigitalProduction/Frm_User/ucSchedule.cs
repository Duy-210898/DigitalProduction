using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using DigitalProduction.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DigitalProduction
{
    public partial class ucSchedule : UserControl
    {
        private BindingList<ProductionSchedule> productionSchedules = new BindingList<ProductionSchedule>();
        private WebSocketClient _webSocketClient;
        private int? selectedYear = DateTime.Today.Year;
        private Label lblTotalRecords;
        private SimpleButton btnSendData, btnDevideData;
        // Keep track of selected items
        List<SalesOrder> selectedSalesOrders = new List<SalesOrder>();
        private readonly string[] columnsToHide = { "InventoryQty", "DepartmentID", "Factory", "OrderID", "LastNo", "PartSizeUnit", "SizeID", "MaterialUnit", "MaterialID", "Process", "PartId", "GroupSO" };

        public ucSchedule()
        {
            InitializeComponent();
            SetupGridControl();
            InitializeTotalLabel();
            InitializeMonthFilter();

            dateTimePickerSchedule.Format = DateTimePickerFormat.Custom;
            dateTimePickerSchedule.CustomFormat = "yyyy";
            dateTimePickerSchedule.ShowUpDown = true;

            lblFilterDate.Text = LocalizationManager.GetString("FilterDate");
            lblSelectSO.Text = LocalizationManager.GetString("SelectSO");
            lb_PartName.Text = LocalizationManager.GetString("SelectPart");
            lb_Size.Text = LocalizationManager.GetString("SelectSize");

            //  cboSO.EditValueChanged += cboSO_EditValueChanged;
            InitializeSyncButton();

            //cboSO.Properties.TextEditStyle = TextEditStyles.Standard;

            gridLookUpEditSO.Properties.View = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridLookUpEditSO.Properties.PopupFormSize = new Size(400, 300);
            gridLookUpEditSO.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

            GridView view = gridLookUpEditSO.Properties.View as GridView;
            view.Columns.Clear();

            view.Columns.AddVisible("SO", "SO");
            view.Columns.AddVisible("CreatedAt", LocalizationManager.GetString("CreatedAt"));
            view.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            view.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy";
            gridLookUpEditSO.Properties.PopupFilterMode = PopupFilterMode.Contains;

            // Optional: improve filtering UI
            view.OptionsView.ShowAutoFilterRow = true;
            view.Columns["SO"].OptionsFilter.AutoFilterCondition = AutoFilterCondition.Contains;

            // Setup checkbox selection in GridView
            view.OptionsView.ShowIndicator = false;
            view.OptionsView.ShowColumnHeaders = true;
            view.OptionsView.ShowGroupPanel = false;
            view.OptionsSelection.MultiSelect = true;
            view.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
            view.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsSelection.ShowCheckBoxSelectorInGroupRow = DevExpress.Utils.DefaultBoolean.False;
            view.BestFitColumns();

            // Optional: make popup open immediately and not editable
            gridLookUpEditSO.Properties.ImmediatePopup = true;
            gridLookUpEditSO.Properties.PopupView.OptionsBehavior.Editable = false;

            // Selection changed event
            view.SelectionChanged += (s, e) =>
            {
                selectedSalesOrders.Clear();
                var selectedRows = view.GetSelectedRows();
                foreach (int rowHandle in selectedRows)
                {
                    if (view.GetRow(rowHandle) is SalesOrder so)
                        selectedSalesOrders.Add(so);
                }

                _ = GetDataAndLoadToGridAsync();
                // Force update of display text
                gridLookUpEditSO.RefreshEditValue();
            };

            // Custom display text event
            gridLookUpEditSO.CustomDisplayText += (s, e) =>
            {
                if (selectedSalesOrders.Count > 0)
                {
                    e.DisplayText = string.Join(", ", selectedSalesOrders.Select(so => so.SO));
                }
            };
        }

        //private void cboSO_EditValueChanged(object sender, EventArgs e)
        //{
        //    selectedSOs = cboSO.Properties.Items
        //                        .GetCheckedValues()
        //                        .Cast<string>()
        //                        .ToList();

        //    // Do something with selectedSOs
        //    Console.WriteLine("Selected SOs: " + string.Join(", ", selectedSOs));

        //    if (selectedSOs != null && selectedSOs.Count > 0)
        //    {
        //        _ = GetDataAndLoadToGridAsync();
        //    }
        //    else {
        //        gridControlSchedule.DataSource = null;
        //    }
        //}


        public class ProductionScheduleComparer : IEqualityComparer<ProductionSchedule>
        {
            public bool Equals(ProductionSchedule x, ProductionSchedule y)
            {
                return x != null && y != null && x.PartId == y.PartId && x.SizeID == y.SizeID && x.OrderID == y.OrderID;
            }

            public int GetHashCode(ProductionSchedule obj)
            {
                return obj.GetHashCode();
            }
        }

        private async void BtnSendData_Click(object sender, EventArgs e, bool isDeviceData)
        {
            // avoid dupliacte
            List<ProductionSchedule> filteredSchedules = GetFilteredData().Distinct(new ProductionScheduleComparer()).ToList();

            if (filteredSchedules.Count == 0)
            {
                MessageBox.Show("No data available to send.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool allSame = filteredSchedules
                .GroupBy(s => new { s.Model })
                .Count() == 1;

            if (!allSame)
            {
                MessageBox.Show("Please sure Model is the same", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            HashSet<int> sizeIDs = new HashSet<int>(filteredSchedules.Select(s => s.SizeID));
            if (sizeIDs.Count > 6)
            {
                MessageBox.Show("Only allow minimun or equal to 6 sizes", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //if (filteredSchedules.Count >= 20) {
            //    MessageBox.Show("Only allow 20 SO", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            // Get reference to frmMain
            Form parentForm = this.FindForm();
            if (parentForm is frmMain mainForm)
            {
                // Switch to ucDistribution
                await mainForm.ShowUserControlAsync<ucDistribution>();

                // Send filtered data to ucDistribution
                if (mainForm._userControls.TryGetValue(typeof(ucDistribution), out UserControl userControl))
                {
                    if (userControl is ucDistribution distributionControl)
                    {
                        distributionControl.ReceiveFilteredData(filteredSchedules, isDeviceData);
                    }
                }

                // 🔥 Highlight "Distribution" in Accordion Menu
                mainForm.HighlightSelectedItem(mainForm.btnDistribution);
                MessageBox.Show($"Sent {filteredSchedules.Count} records to Distribution!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private List<ProductionSchedule> GetFilteredData()
        {
            var filteredData = new List<ProductionSchedule>();

            if (gridViewSchedule == null || gridViewSchedule.DataSource == null)
                return filteredData;

            // Ensure the grid view is refreshed to reflect the latest filter changes
            gridViewSchedule.RefreshData();

            // Iterate through filtered (visible) rows
            for (int rowHandle = 0; rowHandle < gridViewSchedule.DataRowCount; rowHandle++)
            {
                if (gridViewSchedule.IsDataRow(rowHandle))
                {
                    var row = gridViewSchedule.GetRow(rowHandle) as ProductionSchedule;
                    if (row != null)
                    {
                        filteredData.Add(row);
                    }
                }
            }

            return filteredData;
        }


        private void SetupGridControl()
        {
            gridControlSchedule.DataSource = productionSchedules;

            gridViewSchedule.OptionsBehavior.Editable = true;
            string[] columnsToHide = {
                "PeicesPerPair",
                "CuttingDieQty",
                "MaterialLayer",
                "TotalPiecesPerPair",
                "Status"
            };

            foreach (string colName in columnsToHide)
            {
                GridColumn col = gridViewSchedule.Columns.ColumnByFieldName(colName);
                if (col != null)
                {
                    col.Visible = false;
                }
                else
                {
                    Console.WriteLine($"Column not found: {colName}");
                }
            }
            // Appearance settings
            gridViewSchedule.Appearance.FilterPanel.Font = new Font("Segoe UI", 12F);
            gridViewSchedule.Appearance.FilterPanel.Options.UseFont = true;

            gridViewSchedule.Appearance.Row.Font = new Font("Segoe UI", 11);
            gridViewSchedule.Appearance.Row.Options.UseFont = true;

            gridViewSchedule.ColumnPanelRowHeight = 40;
            gridViewSchedule.RowHeight = 60;
            gridViewSchedule.Appearance.HeaderPanel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            gridViewSchedule.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridViewSchedule.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Optional row font and height
            // gridViewSchedule.Appearance.Row.Font = new Font("Segoe UI", 12F);
            gridViewSchedule.RowHeight = 30;

            // View options
            gridViewSchedule.OptionsView.ShowGroupPanel = true;
            gridViewSchedule.OptionsView.GroupDrawMode = DevExpress.XtraGrid.Views.Grid.GroupDrawMode.Office;
            gridViewSchedule.OptionsView.ShowGroupedColumns = true;

            // Important: avoid auto-expanding all groups for large datasets
            gridViewSchedule.OptionsBehavior.AutoExpandAllGroups = false;

            // Optional: expand top-level groups manually (if performance is acceptable)
            gridViewSchedule.ExpandAllGroups(); // Caution: use only with small to medium datasets

            gridViewSchedule.CustomDrawFooterCell += (s, e) =>
            {
                e.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
                e.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                e.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                e.Info.DisplayText += "\n"; // force line break

                e.Appearance.Options.UseFont = true;
                e.Appearance.Options.UseTextOptions = true;
            };

            // Setup columns and grouping
            GroupGridViewColumns();
            gridViewSchedule.LayoutChanged();
        }


        private void GroupGridViewColumns()
        {
            gridViewSchedule.ClearGrouping();

            GridColumn partNameColumn = gridViewSchedule.Columns["PartName"];
            GridColumn sizeColumn = gridViewSchedule.Columns["Size"];
            GridColumn soColumn = gridViewSchedule.Columns["SO"];
            if (soColumn != null)
            {
                soColumn.GroupIndex = 0;
            }
            if (partNameColumn != null)
            {
                partNameColumn.GroupIndex = 1;
            }

            if (sizeColumn != null)
            {
                sizeColumn.GroupIndex = 2;
            }

            gridViewSchedule.ExpandAllGroups(); // Expand all groups after setting
        }

        private void InitializeTotalLabel()
        {
            lblTotalRecords = new Label
            {
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                Text = $" {LocalizationManager.GetString("TotalRecords")} 0",
                AutoSize = true,
                BackColor = System.Drawing.Color.AntiqueWhite,
                ForeColor = System.Drawing.Color.Green,
                Padding = new Padding(5)
            };

            btnSendData = new SimpleButton
            {
                Text = LocalizationManager.GetString("SelectData"),
                Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightBlue,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0) // Adds space between label and button
            };
            btnSendData.Click += (s, e) => BtnSendData_Click(s, e, false);

            // Add components to FlowLayoutPanel
            bottomPanel.Controls.Add(lblTotalRecords);
            bottomPanel.Controls.Add(btnSendData);

            btnDevideData = new SimpleButton
            { 
                Text = LocalizationManager.GetString("SelectDevideData"),
                Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightBlue,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0) // Adds space between label and button
            };
            btnDevideData.Click += (s, e) => BtnSendData_Click(s, e, true);

            // Add components to FlowLayoutPanel
            bottomPanel.Controls.Add(btnDevideData);

            // Add to UserControl
            Controls.Add(bottomPanel);
        }


        private void InitializeMonthFilter()
        {
            dateTimePickerSchedule.ValueChanged += async (sender, e) =>
            {
                selectedYear = dateTimePickerSchedule.Value.Year;

                await GetListOfSOsByYearAsync();
            };
        }


        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage;
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            if (productionSchedules.Count == 0)
            {
                _ = GetListOfSOsByYearAsync();
                // _ = GetDataAndLoadToGridAsync();
            }
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new
            {
                app = Global.App,
                action = "getSchedule",
                so = selectedSalesOrders.Select(x => x.SO).ToList(),
                includeDistributed = true
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        public async Task GetListOfSOsByYearAsync()
        {
            var request = new
            {
                app = Global.App,
                action = "getListOfSOsByYear",
                year = selectedYear
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                string action = JObject.Parse(jsonData)["action"]?.ToString();
                if (!string.IsNullOrEmpty(action))
                {
                    switch (action)
                    {
                        case "getSchedule":
                            var scheduleResponse = JsonConvert.DeserializeObject<ResponseMessage<List<ProductionSchedule>>>(jsonData);
                            if (scheduleResponse?.Schedule != null)
                            {
                                IOverlaySplashScreenHandle handle = null;

                                try
                                {
                                    handle = SplashScreenManager.ShowOverlayForm(gridControlSchedule);

                                    // Thực hiện cập nhật dữ liệu
                                    SafeUpdateGrid(scheduleResponse.Schedule);
                                }
                                finally
                                {
                                    if (handle != null)
                                        SplashScreenManager.CloseOverlayForm(handle);
                                }
                            }
                            break;

                        case "getListOfSOsByYear":
                            var soListResponse = JsonConvert.DeserializeObject<ResponseMessage<List<SalesOrder>>>(jsonData);
                            if (soListResponse?.Data != null)
                            {
                                gridLookUpEditSO.Properties.DataSource = soListResponse.Data;
                                gridLookUpEditSO.Properties.DisplayMember = "SO";
                                gridLookUpEditSO.Properties.ValueMember = "SO";
                                // SafeUpdateSOList(soListResponse.Data);
                            }
                            else
                            {
                                gridLookUpEditSO.Properties.DataSource = null;
                                //cboSO.Properties.Items.Clear();
                            }
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    ShowMessage.ShowInfo("Cant not processing get WebSocket data");
                }
            }
            catch (Exception ex)
            {
                ConnectionManager.Instance.IsReconnecting = true;
                MessageBox.Show($"Error processing WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void SafeUpdateSOList(List<string> soList)
        //{
        //    if (InvokeRequired)
        //    {
        //        Invoke(new Action(() => SafeUpdateSOList(soList)));
        //        return;
        //    }

        //    cboSO.Properties.Items.BeginUpdate();
        //    try
        //    {
        //        cboSO.Properties.Items.Clear();
        //        foreach (var so in soList)
        //        {
        //            cboSO.Properties.Items.Add(so, CheckState.Unchecked, true);
        //        }
        //    }
        //    finally
        //    {
        //        cboSO.Properties.Items.EndUpdate();
        //    }
        //}

        // Helper method to safely update schedule grid on UI thread
        private void SafeUpdateGrid(List<ProductionSchedule> schedules)
        {
            if (InvokeRequired)
                Invoke(new Action(() => UpdateGrid(schedules)));
            else
                UpdateGrid(schedules);
        }

        private void UpdateGrid(List<ProductionSchedule> newSchedules)
        {
            productionSchedules.Clear();
            foreach (var schedule in newSchedules)
            {
                productionSchedules.Add(schedule);
            }

            ApplyMonthFilter();

            // Hide columns after data is bound
            HideGridColumns();
            gridViewSchedule.OptionsFilter.AllowMultiSelectInCheckedFilterPopup = true;
            gridViewSchedule.Columns["Size"].OptionsFilter.FilterPopupMode = FilterPopupMode.CheckedList;
            gridViewSchedule.Columns["SO"].OptionsFilter.FilterPopupMode = FilterPopupMode.CheckedList;
            gridViewSchedule.Columns["PartName"].OptionsFilter.FilterPopupMode = FilterPopupMode.CheckedList;
            gridViewSchedule.ShowFilterPopupCheckedListBox += (s, e) =>
            {
                if (e.Column.FieldName == "Size")
                {
                    var originalItems = e.CheckedComboBox.Items
                        .Cast<CheckedListBoxItem>()
                        .ToList();

                    var sortedItems = originalItems
                        .Where(item => double.TryParse(item.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        .OrderBy(item =>
                        {
                            double.TryParse(item.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double number);
                            return number;
                        })
                        .ToList();

                    // Clear the current items
                    e.CheckedComboBox.Items.Clear();

                    // Add sorted items with correct check state
                    foreach (var item in sortedItems)
                    {
                        e.CheckedComboBox.Items.Add(new CheckedListBoxItem(item.Value, item.Description, item.CheckState == CheckState.Checked ? CheckState.Checked : CheckState.Unchecked, item.Enabled));
                    }

                    // Optional UI styling
                    e.CheckedComboBox.BorderStyle = BorderStyles.Office2003;
                }
            };
            LoadSizeItemsToCheckedComboBox(comboSize, comboxPartName, gridViewSchedule);
        }

        private void HideGridColumns()
        {
            foreach (var columnName in columnsToHide)
            {
                var column = gridViewSchedule.Columns[columnName];
                if (column != null)
                {
                    column.Visible = false;
                }
            }

            TranslateHeaders();
        }


        private void TranslateHeaders()
        {
            if (gridControlSchedule.MainView is GridView gridView && gridView.Columns.Count > 0)
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
        }

        private void ApplyMonthFilter()
        {
            var filteredData = productionSchedules
                .Where(schedule => selectedYear == null ||
                                  (schedule.CreatedAt.Year == selectedYear)).ToList();

            // Update the grid control's data point
            gridControlSchedule.DataSource = filteredData;

            // Reset and apply grouping
            gridViewSchedule.ClearGrouping();
            GroupGridViewColumns();
            gridViewSchedule.ExpandAllGroups();
            gridViewSchedule.RefreshData();

            //if (!filteredData.Any())
            //{
            //    ShowMessage.ShowInfo(LocalizationManager.GetString("NoRecords"));
            //}

            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            if (gridViewSchedule == null)
                return;

            int totalCount = gridViewSchedule.DataRowCount; // Get only filtered rows
            lblTotalRecords.Text = $"{LocalizationManager.GetString("TotalRecords")} {totalCount}";
            lblTotalRecords.BackColor = Color.AntiqueWhite;
            lblTotalRecords.ForeColor = Color.Green;
        }

        private void InitializeSyncButton()
        {
            btnSync.Text = LocalizationManager.GetString("Sync");
            btnSync.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSync.Click += BtnSync_Click;
        }
        private void BtnSync_Click(object sender, EventArgs e)
        {
            _ = GetListOfSOsByYearAsync();
        }

        public class SalesOrder
        {
            public string SO { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private void LoadSizeItemsToCheckedComboBox(CheckedComboBoxEdit comboBoxSize, CheckedComboBoxEdit comboBoxPart, GridView gridView)
        {
            comboBoxSize.Properties.BeginUpdate();
            comboBoxPart.Properties.BeginUpdate();

            comboBoxSize.Properties.Items.Clear();
            comboBoxPart.Properties.Items.Clear();

            var list = gridView.DataSource as IEnumerable<object>;
            if (list == null)
            {
                comboBoxSize.Properties.EndUpdate();
                comboBoxPart.Properties.EndUpdate();
                return;
            }

            var sizeSet = new HashSet<string>();
            var partSet = new HashSet<string>();

            foreach (var item in list)
            {
                var type = item.GetType();

                var sizeValue = type.GetProperty("Size")?.GetValue(item)?.ToString();
                if (!string.IsNullOrEmpty(sizeValue))
                    sizeSet.Add(sizeValue);

                var partValue = type.GetProperty("PartName")?.GetValue(item)?.ToString();
                if (!string.IsNullOrEmpty(partValue))
                    partSet.Add(partValue);
            }

            // Sort numeric and non-numeric sizes
            var numericSizes = sizeSet
                .Where(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                .Select(s => new
                {
                    Original = s,
                    Parsed = double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)
                })
                .OrderBy(x => x.Parsed)
                .Select(x => x.Original)
                .ToList();

            var nonNumericSizes = sizeSet
                .Where(s => !double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var size in numericSizes.Concat(nonNumericSizes))
                comboBoxSize.Properties.Items.Add(size);

            foreach (var part in partSet.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                comboBoxPart.Properties.Items.Add(part);

            // Optional: Set drop-down row count for better display
            comboBoxSize.Properties.DropDownRows = Math.Min(10, comboBoxSize.Properties.Items.Count);
            comboBoxPart.Properties.DropDownRows = Math.Min(10, comboBoxPart.Properties.Items.Count);

            // Set larger popup size
            comboBoxSize.Properties.PopupFormMinSize = new Size(200, 350);
            comboBoxPart.Properties.PopupFormMinSize = new Size(200, 350);

            comboBoxSize.Properties.EndUpdate();
            comboBoxPart.Properties.EndUpdate();

            // Optional: auto-expand combo if items exist
            if (comboBoxSize.Properties.Items.Count > 0)
                comboBoxPart.ShowPopup();
        }



        //private void ApplyFilterFromCheckedComboBox(CheckedComboBoxEdit comboBox, GridView gridView, string fieldName, bool isNumeric = true)
        //{
        //    var checkedItems = comboBox.Properties.Items
        //        .Cast<CheckedListBoxItem>()
        //        .Where(item => item.CheckState == CheckState.Checked)
        //        .ToList();

        //    if (checkedItems.Count == 0)
        //    {
        //        gridView.ActiveFilter.Clear();
        //        return;
        //    }

        //    // Use CultureInvariant parsing
        //    IEnumerable<string> values;

        //    if (isNumeric && gridView.Columns[fieldName].ColumnType != typeof(string))
        //    {
        //        // Unquoted numbers
        //        values = checkedItems
        //            .Where(item => double.TryParse(item.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        //            .OrderBy(item =>
        //            {
        //                double.TryParse(item.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double number);
        //                return number;
        //            })
        //            .Select(item => item.Value.ToString());
        //    }
        //    else
        //    {
        //        // Quote values for string-based filters
        //        values = checkedItems
        //            .Select(item => $"'{item.Value?.ToString().Replace("'", "''")}'")
        //            .OrderBy(v => v);
        //    }

        //    string filter = $"[{fieldName}] IN ({string.Join(", ", values)})";
        //    gridView.ActiveFilterString = filter;
        //}

        private void ApplyCombinedFilters()
        {
            var sizeValues = GetCheckedValues(comboSize, gridViewSchedule, "Size");
            var partValues = GetCheckedValues(comboxPartName, gridViewSchedule, "PartName");

            List<string> filters = new List<string>();

            if (sizeValues.Any())
                filters.Add($"[Size] IN ({string.Join(", ", sizeValues)})");

            if (partValues.Any())
                filters.Add($"[PartName] IN ({string.Join(", ", partValues)})");

            gridViewSchedule.ActiveFilterString = string.Join(" AND ", filters);
        }

        private List<string> GetCheckedValues(CheckedComboBoxEdit comboBox, GridView gridView, string fieldName)
        {
            bool isNumericColumn = gridView.Columns[fieldName].ColumnType != typeof(string);

            return comboBox.Properties.Items
                .Cast<CheckedListBoxItem>()
                .Where(item => item.CheckState == CheckState.Checked)
                .Select(item => item.Value?.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v =>
                {
                    if (isNumericColumn && double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        return v; // keep unquoted
                    else
                        return $"'{v.Replace("'", "''")}'"; // quote for string
                })
                .ToList();
        }

        private void comboSize_EditValueChanged(object sender, EventArgs e)
        {
            ApplyCombinedFilters();
        }
        private void comboPartName_EditValueChanged(object sender, EventArgs e)
        {
            ApplyCombinedFilters();
        }

    }
}
