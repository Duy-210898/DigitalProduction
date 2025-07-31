using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
namespace DigitalProduction
{
    public partial class ucViewDistribution : UserControl
    {

        private readonly string[] columnsToHide = {"DeviceID", "OperatorID", "MaterialCode", "PartCode", "CreatedAt", "DepartmentID", "Factory", "OrderID", "LastNo", "PartSizeUnit", "SizeID", "MaterialUnit", "MaterialID", "Process", "PartId", "GroupSO", "InventoryQty", "PeicesPerPair", "CuttingDieQty", "MaterialLayer", "TotalPiecesPerPair", "VietnameseName" };
        private static List<string> selectedSalesOrders = new List<string>();

        public ucViewDistribution()
        {
            InitializeComponent();

            lblFilterDate.Text = LocalizationManager.GetString("FilterDate");
            lblSelectSO.Text = LocalizationManager.GetString("SelectSO");

            // Set default to the first day of the previous month
            DateTime today = DateTime.Today;
            DateTime firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);

            dateTimePickerViewSO.Value = firstDayLastMonth;
            dateTimePickerViewSO.Format = DateTimePickerFormat.Custom;
            dateTimePickerViewSO.CustomFormat = "MM/yyyy";
            dateTimePickerViewSO.ShowUpDown = true;

            // Load SOs for previous month
            DevExpress.XtraSplashScreen.SplashScreenManager.ShowForm(this, typeof(frmLoading), true, true);

            try
            {
                SetupSOGridLookUp(gridLookUpSOs, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);
            }
            finally
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
            }


            gridLookUpSOs.CloseUp += (s, e) =>
            {
                LoadProductionSchedulesBySelectedSOs(gridLookUpSOs, gridControlViewSO, gridViewSO);
                HideGridColumns();
            };

            gridLookUpSOs.EditValueChanged += (s, e) =>
            {
                LoadProductionSchedulesBySelectedSOs(gridLookUpSOs, gridControlViewSO, gridViewSO);
                HideGridColumns();
            };

            // Reload SOs when month changes
            dateTimePickerViewSO.ValueChanged += (s, e) =>
            {
                // Reload GridLookUpEdit with new SOs
                SetupSOGridLookUp(gridLookUpSOs, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);

                // Clear selected SO
                gridLookUpSOs.EditValue = null;

                // Clear data grid and reset label
                gridControlViewSO.DataSource = null;
                gridLookUpEdit1View.Columns.Clear(); // optional: clear columns too
            };
            var view = gridLookUpSOs.Properties.View;

            gridLookUpSOs.Properties.View.OptionsSelection.MultiSelect = true;
            gridLookUpSOs.Properties.View.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;

            gridLookUpSOs.Properties.View.SelectionChanged += (s, e) =>
            {
                selectedSalesOrders.Clear();

                foreach (int rowHandle in view.GetSelectedRows())
                {
                    var row = view.GetRow(rowHandle) as DataRowView;
                    if (row != null)
                    {
                        selectedSalesOrders.Add(row["SO"].ToString());
                    }
                }

                gridLookUpSOs.RefreshEditValue(); // Triggers CustomDisplayText
            };

            gridLookUpSOs.CustomDisplayText += (s, e) =>
            {
                if (selectedSalesOrders.Count > 0)
                {
                    e.DisplayText = string.Join(", ", selectedSalesOrders);
                }
                else
                {
                    e.DisplayText = string.Empty;
                }
            };

            InitializeSyncButton();
            gridViewSO.CustomDrawRowIndicator += (s, e) =>
            {
                if (e.Info.IsRowIndicator && e.RowHandle >= 0)
                {
                    e.Info.DisplayText = (e.RowHandle + 1).ToString();
                }
            };

            gridViewSO.IndicatorWidth = 50; // Optional: make room for STT
            gridViewSO.OptionsBehavior.Editable = false;
        }


        private void SetupSOGridLookUp(DevExpress.XtraEditors.GridLookUpEdit cboSO, DateTime selectedDate, int departmentId)
        {
            // 1. Get list of SOs
            List<string> soList = DbHelper.GetDistinctSOListByMonthAndDepartment(
                selectedDate.Month, selectedDate.Year, departmentId
            );

            // 2. Create a DataTable for binding
            DataTable dt = new DataTable();
            dt.Columns.Add("SO", typeof(string));

            foreach (var so in soList)
            {
                if (!string.IsNullOrWhiteSpace(so))
                    dt.Rows.Add(so);
            }

            // 3. Bind to GridLookUpEdit
            cboSO.Properties.DataSource = dt;
            cboSO.Properties.DisplayMember = "SO";
            cboSO.Properties.ValueMember = "SO";

            // 4. Configure the view
            var view = cboSO.Properties.View;
            view.Columns.Clear();
            view.Columns.AddVisible("SO", "Sales Order");
            view.OptionsView.ShowAutoFilterRow = true;
            view.OptionsView.ShowIndicator = false;
            view.OptionsView.ShowGroupPanel = false;
            view.OptionsBehavior.Editable = false;
            view.BestFitColumns();

            // 5. Configure popup
            cboSO.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cboSO.Properties.ImmediatePopup = true;
            cboSO.Properties.PopupFilterMode = DevExpress.XtraEditors.PopupFilterMode.Contains;

            int itemHeight = 24;
            int maxItems = 10;
            int popupHeight = Math.Min(soList.Count, maxItems) * itemHeight + 50;
            cboSO.Properties.PopupFormSize = new Size(cboSO.Width + 100, popupHeight);
        }


        private void LoadProductionSchedulesBySelectedSOs(DevExpress.XtraEditors.GridLookUpEdit cboSO, GridControl gridControl, GridView gridView)
        {
            if (selectedSalesOrders == null || selectedSalesOrders.Count == 0)
            {
                gridControl.DataSource = null;
                return;
            }

            try
            {
                // Show loading/wait form
                DevExpress.XtraSplashScreen.SplashScreenManager.ShowForm(this, typeof(frmLoading), true, true);
                // 1. Query schedules
                var allSchedules = DbHelper.GetSchedulesBySOList(selectedSalesOrders);

                // 2. Bind to grid
                gridControl.DataSource = allSchedules;

                // 3. Rebuild grid view
                gridView.PopulateColumns();

                // 4. Group by SO
                GridColumn soColumn = gridView.Columns["SO"];
                if (soColumn != null)
                {
                    soColumn.GroupIndex = 0;
                    soColumn.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
                    gridView.ExpandAllGroups();
                }

                // 5. Refresh and update
                gridView.RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the loading form
                if (DevExpress.XtraSplashScreen.SplashScreenManager.Default != null)
                    DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
            }
        }


        private void HideGridColumns()
        {
            foreach (var columnName in columnsToHide)
            {
                var column = gridViewSO.Columns[columnName];
                if (column != null)
                {
                    column.Visible = false;
                }
            }

            TranslateHeaders();
            // Subscribe to the RowStyle event
            gridViewSO.RowCellStyle += gridViewSO_RowCellStyle;
        }

        private void gridViewSO_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
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
                        e.Appearance.BackColor = System.Drawing.Color.LightGreen; // Green for complete
                    }
                    else if (rowData.Status == "Pending")
                    {
                        e.Appearance.BackColor = System.Drawing.Color.LightYellow; // Yellow for pending
                    }
                    else
                    {
                        e.Appearance.BackColor = System.Drawing.Color.LightSteelBlue; // Red for other statuses
                    }
                }
            }
        }
        private void TranslateHeaders()
        {

            if (gridControlViewSO.MainView is GridView gridView && gridView.Columns.Count > 0)
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
        private void InitializeSyncButton()
        {
            btnSync.Text = LocalizationManager.GetString("Sync");
            btnSync.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSync.Click += BtnSync_Click;
        }
        private void BtnSync_Click(object sender, EventArgs e)
        {
            // Reload SO list
            SetupSOGridLookUp(gridLookUpSOs, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);
        }
    }
}