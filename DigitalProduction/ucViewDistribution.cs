using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
namespace DigitalProduction
{
    public partial class ucViewDistribution : UserControl
    {

        private readonly string[] columnsToHide = {"DepartmentID", "Factory", "OrderID", "LastNo", "PartSizeUnit", "SizeID", "MaterialUnit", "MaterialID", "Process", "PartId", "GroupSO"};
        private Label lblTotalRecords;

        public ucViewDistribution()
        {
            InitializeComponent();
            InitializeTotalLabel();

            lblFilterDate.Text = LocalizationManager.GetString("FilterDate");
            lblSelectSO.Text = LocalizationManager.GetString("SelectSO");

            SetupSOCheckedComboBox(cboSO, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);
            cboSO.CloseUp += (s, e) =>
            {
                LoadProductionSchedulesBySelectedSOs(cboSO, gridControlViewSO, gridViewSO);
                HideGridColumns();
            };

            cboSO.EditValueChanged += (s, e) =>
            {
                LoadProductionSchedulesBySelectedSOs(cboSO, gridControlViewSO, gridViewSO);
                HideGridColumns();
            };
            gridViewSO.ColumnFilterChanged += (s, e) => UpdateTotalLabel();

            dateTimePickerViewSO.ValueChanged += (s, e) =>
            {
                // Reload the SO list for the selected month/year
                SetupSOCheckedComboBox(cboSO, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);

                // Optionally reset selection and grid
                cboSO.SetEditValue(string.Empty);
                gridControlViewSO.DataSource = null;
                lblTotalRecords.Text = $"{LocalizationManager.GetString("TotalRecords")} 0";
            };

            InitializeSyncButton();
        }
        private void UpdateTotalLabel()
        {
            if (gridViewSO != null)
            {
                int totalCount = gridViewSO.DataRowCount;
                lblTotalRecords.Text = $"{LocalizationManager.GetString("TotalRecords")} {totalCount}";
            }
        }

        private void InitializeTotalLabel()
        {
            lblTotalRecords = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Green,
                BackColor = Color.AntiqueWhite,
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Bottom,
                Text = $"{LocalizationManager.GetString("TotalRecords")} 0"
            };

            this.Controls.Add(lblTotalRecords);
            this.Controls.SetChildIndex(lblTotalRecords, 0); // Ensure it appears on top
        }

        private void SetupSOCheckedComboBox(DevExpress.XtraEditors.CheckedComboBoxEdit cboSO, DateTime selectedDate, int departmentId)
        {
            var soList = DbHelper.GetDistinctSOListByMonthAndDepartment(selectedDate.Month, selectedDate.Year, departmentId);

            cboSO.Properties.Items.Clear();
            foreach (var so in soList)
            {
                cboSO.Properties.Items.Add(so, false);
            }

            cboSO.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            // Set popup height to fit items responsively
            int itemCount = soList.Count;
            int itemHeight = 100;
            int verticalPadding = 10;

            int maxVisibleItems = 10;
            int calculatedHeight = Math.Min(itemCount, maxVisibleItems) * itemHeight + verticalPadding;

            cboSO.Properties.PopupFormSize = new Size(cboSO.Width, calculatedHeight);
            cboSO.Properties.DropDownRows = Math.Min(itemCount, maxVisibleItems);

            if (itemCount > 0)
            {
                cboSO.Focus();
                cboSO.ShowPopup();
            }
        }

        private void LoadProductionSchedulesBySelectedSOs(DevExpress.XtraEditors.CheckedComboBoxEdit cboSO, GridControl gridControl, GridView gridView)
        {
            var selectedSOs = cboSO.Properties.Items
                .Cast<DevExpress.XtraEditors.Controls.CheckedListBoxItem>()
                .Where(item => item.CheckState == CheckState.Checked)
                .Select(item => item.Value.ToString())
                .ToList();

            //if (selectedSOs.Count == 0)
            //{
            //    MessageBox.Show("Please select at least one SO.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            var allSchedules = DbHelper.GetSchedulesBySOList(selectedSOs);
            gridControl.DataSource = allSchedules;

            gridView.PopulateColumns();
            // gridView.BestFitColumns();

            // Group by SO
            GridColumn soColumn = gridView.Columns["SO"];
            if (soColumn != null)
            {
                soColumn.GroupIndex = 0;
                soColumn.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
                gridView.ExpandAllGroups();
            }

            // Refresh and update
            gridView.RefreshData();
            UpdateTotalLabel();
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
            SetupSOCheckedComboBox(cboSO, dateTimePickerViewSO.Value, Global.CurrentUser.DepartmentID);
        }
    }
}