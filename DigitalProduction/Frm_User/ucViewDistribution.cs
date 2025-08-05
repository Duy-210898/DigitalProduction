using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraExport.Helpers;
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
            };

            gridLookUpSOs.EditValueChanged += (s, e) =>
            {
                LoadProductionSchedulesBySelectedSOs(gridLookUpSOs, gridControlViewSO, gridViewSO);
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
                gridLookUpEdit1View.Columns.Clear();
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
                DevExpress.XtraSplashScreen.SplashScreenManager.ShowForm(this, typeof(frmLoading), true, true);

                // Step 1: Load flat list
                var flatSchedules = DbHelper.GetSchedulesBySOList(selectedSalesOrders);

                // Step 2: Convert to nested structure: SO ➝ Size ➝ Details
                var groupedSchedules = flatSchedules
                    .GroupBy(s => s.SO)
                    .Select(soGroup => new ScheduleGroup
                    {
                        SO = soGroup.Key,
                        Sizes = soGroup
                            .GroupBy(s => s.Size)
                            .Select(sizeGroup => new SizeGroup
                            {
                                Size = sizeGroup.Key,
                                Details = sizeGroup.ToList()
                            }).ToList()
                    }).ToList();

                // Step 3: Bind to grid
                gridControl.DataSource = groupedSchedules;
                gridView.PopulateColumns();

                // Optional: Hide virtual property if any
                var detailsColumn = gridView.Columns["Details"];
                if (detailsColumn != null)
                    detailsColumn.Visible = false;

                // Step 4: Setup master-detail: SO ➝ Size
                gridView.OptionsDetail.EnableMasterViewMode = true;
                gridView.OptionsDetail.ShowDetailTabs = false;
                gridView.OptionsDetail.SmartDetailExpand = true;

                gridView.OptionsDetail.EnableMasterViewMode = true;
                // Level 0 → SO → Sizes
                gridView.MasterRowGetRelationCount += (s, e) => { e.RelationCount = 1; };
                gridView.MasterRowGetRelationName += (s, e) => { e.RelationName = "Sizes"; };
                gridView.MasterRowGetChildList += (s, e) =>
                {
                    var soGroup = gridView.GetRow(e.RowHandle) as ScheduleGroup;
                    e.ChildList = soGroup?.Sizes;
                };

                // Level 1 → Sizes → Details
                gridView.MasterRowGetLevelDefaultView += (s, e) =>
                {
                    if (e.RelationIndex == 0)
                    {
                        var sizeView = new GridView(gridControl);
                        gridControl.ViewCollection.Add(sizeView);

                        sizeView.OptionsDetail.EnableMasterViewMode = true;
                        sizeView.OptionsBehavior.Editable = false;
                        sizeView.OptionsView.ShowGroupPanel = false;

                        // Provide relation count (1 for next level)
                        sizeView.MasterRowGetRelationCount += (sender, args) =>
                        {
                            args.RelationCount = 1;
                        };

                        // Provide relation name
                        sizeView.MasterRowGetRelationName += (sender, args) =>
                        {
                            args.RelationName = "Details";
                        };

                        // Provide child list for the detail level (SizeDetail list)
                        sizeView.MasterRowGetChildList += (sender, args) =>
                        {
                            var sizeGroup = sizeView.GetRow(args.RowHandle) as SizeGroup;
                            args.ChildList = sizeGroup?.Details; // List<SizeDetail>
                        };

                        // Define the final detail view
                        sizeView.MasterRowGetLevelDefaultView += (sender2, args2) =>
                        {
                            var detailView = new GridView(gridControl);
                            gridControl.ViewCollection.Add(detailView);

                            detailView.OptionsView.ShowGroupPanel = false;
                            detailView.OptionsBehavior.Editable = false;

                            // Manually call PopulateColumns after setting dummy DataSource to generate columns
                            var dummyList = new List<ProductionSchedule>();
                            detailView.PopulateColumns(dummyList); // avoids null columns

                            args2.DefaultView = detailView;

                            // Call your custom logic after columns are created
                            HideGridColumns(detailView);
                        };

                        e.DefaultView = sizeView;
                    }
                };
                gridView.MasterRowExpanded += (s, e) =>
                {
                    gridView.BestFitColumns();
                };


                gridView.CustomColumnDisplayText += (s, e) =>
                {
                    if (e.Column.FieldName == "SO")
                    {
                        if (e.ListSourceRowIndex < 0)
                            return;

                        var row = gridView.GetRow(e.ListSourceRowIndex) as ScheduleGroup;
                        if (row == null || row.Sizes == null)
                            return;

                        bool hasPending = row.Sizes.SelectMany(sz => sz.Details).Any(d => d.Status == "Pending");
                        bool allComplete = row.Sizes.SelectMany(sz => sz.Details).All(d => d.Status == "Complete");

                        string statusText = hasPending ? "Pending" : (allComplete ? "Complete" : "Pending");
                        e.DisplayText = $"{row.SO} || {statusText}";
                    }
                };

                gridView.ExpandAllGroups();
                gridView.RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (DevExpress.XtraSplashScreen.SplashScreenManager.Default != null)
                    DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
            }
        }

        private void HideGridColumns(GridView gridView)
        {
            foreach (var columnName in columnsToHide)
            {
                var column = gridView.Columns[columnName];
                if (column != null)
                {
                    column.Visible = false;
                }
            }

            TranslateHeaders(gridView);
            // Subscribe to the RowStyle event
            gridView.RowCellStyle += gridViewSO_RowCellStyle;
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
        private void TranslateHeaders(GridView gridView)
        {

            if (gridView.Columns.Count > 0)
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