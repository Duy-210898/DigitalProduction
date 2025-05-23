using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static DigitalProduction.ucProgress;

namespace DigitalProduction
{
    public partial class ucSchedule_View : UserControl
    {
        private BindingList<ProductionSchedule> productionSchedules = new BindingList<ProductionSchedule>();
        private WebSocketClient _webSocketClient;
        private DateTime? selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private Label lblTotalRecords;
        private Button btnSaveData;
        // Keep track of selected items
        List<SalesOrder> selectedSalesOrders = new List<SalesOrder>();
        private List<string> selectedSOs = new List<string>();
        private readonly string[] columnsToHide = { "DepartmentID", "Factory", "OrderID", "LastNo", "PartSizeUnit", "SizeID", "MaterialUnit", "MaterialID", "Process", "PartId", "GroupSO" };

        public ucSchedule_View()
        {
            InitializeComponent();
            SetupGridControl();
            InitializeTotalLabel();
            InitializeMonthFilter();

            lblFilterDate.Text = LocalizationManager.GetString("FilterDate");
            lblSelectSO.Text = LocalizationManager.GetString("SelectSO");

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

        private void BtnSaveData_Click(object sender, EventArgs e)
        {
            // avoid dupliacte
            List<ProductionSchedule> filteredSchedules = GetFilteredData().Distinct(new ProductionScheduleComparer()).ToList();
            DbHelper.SaveFilteredSchedulesToDatabase(filteredSchedules);
        }
        private List<ProductionSchedule> GetFilteredData()
        {
            var filteredData = new List<ProductionSchedule>();

            if (gridViewSchedule == null || gridViewSchedule.DataSource == null)
                return filteredData;

            // Ensure the grid view is refreshed to reflect the latest filter changes
            gridViewSchedule.RefreshData();

            // Iterate through filtered (visible) rows
            for (int i = 0; i < gridViewSchedule.RowCount; i++)
            {
                int rowHandle = gridViewSchedule.GetVisibleRowHandle(i);
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

            // Appearance settings
            gridViewSchedule.Appearance.FilterPanel.Font = new Font("Segoe UI", 10F);
            gridViewSchedule.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
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


            // Setup columns and grouping
            GroupGridViewColumns();

            // Update total when filters change
            gridViewSchedule.ColumnFilterChanged += (sender, e) => UpdateTotalLabel();
            // Subscribe to the RowStyle event
            gridViewSchedule.RowCellStyle += gridViewSchedule_RowCellStyle;
        }

        private void gridViewSchedule_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            var view = sender as GridView;
            if (view != null)
            {
                // Get the current row data
                var rowData = view.GetRow(e.RowHandle) as ProductionSchedule;

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
                        e.Appearance.BackColor = Color.LightSteelBlue; // Red for other statuses
                    }
                }
            }
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


            btnSaveData = new Button
            {
                Text = LocalizationManager.GetString("SaveListOfSO"),
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightBlue,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0) // Adds space between label and button
            };

            btnSaveData.Click += BtnSaveData_Click;
            // Add components to FlowLayoutPanel
            bottomPanel.Controls.Add(lblTotalRecords);
            bottomPanel.Controls.Add(btnSaveData);

            // Add to UserControl
            Controls.Add(bottomPanel);
        }


        private void InitializeMonthFilter()
        {
            dateTimePickerSchedule.ValueChanged += async (sender, e) =>
            {
                selectedMonth = new DateTime(dateTimePickerSchedule.Value.Year, dateTimePickerSchedule.Value.Month, 1);

                await GetListOfSOsByMonthYearAsync();
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
                _ = GetListOfSOsByMonthYearAsync();
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
                includeDistributed = false
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        public async Task GetListOfSOsByMonthYearAsync()
        {
            var request = new
            {
                app = Global.App,
                action = "getListOfSOsByMonthYear",
                month = selectedMonth?.Month,
                year = selectedMonth?.Year
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
                                SafeUpdateGrid(scheduleResponse.Schedule);
                            }
                            break;

                        case "getListOfSOsByMonthYear":
                            var soListResponse = JsonConvert.DeserializeObject<ResponseMessage<List<SalesOrder>>>(jsonData);
                            if (soListResponse?.Data != null)
                            {
                                gridLookUpEditSO.Properties.DataSource = soListResponse.Data;
                                gridLookUpEditSO.Properties.DisplayMember = "SO";
                                gridLookUpEditSO.Properties.ValueMember = "SO";
                               // SafeUpdateSOList(soListResponse.Data);
                            }
                            else {
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
                .Where(schedule => selectedMonth == null ||
                                  (schedule.CreatedAt.Year == selectedMonth.Value.Year &&
                                   schedule.CreatedAt.Month == selectedMonth.Value.Month))
                .ToList();

            // Update the grid control's data point
            gridControlSchedule.DataSource = filteredData;
            gridViewSchedule.PopulateColumns();

            // Reset and apply grouping
            gridViewSchedule.ClearGrouping();
            GroupGridViewColumns();
            gridViewSchedule.ExpandAllGroups();
            gridViewSchedule.RefreshData();

            if (!filteredData.Any())
            {
                ShowMessage.ShowInfo(LocalizationManager.GetString("NoRecords"));
            }

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
            _ = GetListOfSOsByMonthYearAsync();
        }

        public class SalesOrder
        {
            public string SO { get; set; }
            public DateTime CreatedAt { get; set; }
        }

    }
}
