using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace DigitalProduction
{
    public partial class ucReportOrder : UserControl
    {
        private DateTimePicker dateTimePicker;
        private GridControl gridControl_OperatorReport; // Use GridControl
        private GridView gridView_OperatorReport;        // Use GridView
        private TableLayoutPanel mainLayout;

        public ucReportOrder()
        {
            InitializeComponent();
            SetupUI();
            _ = LoadOperatorReportAsync(); // Load initial data
        }

        private void SetupUI()
        {
            // Main Layout Panel
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Top panel fixed height
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // GridControl fills rest
            Controls.Add(mainLayout);

            // Top Panel for DateTimePicker
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };

            dateTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMMM yyyy",
                ShowUpDown = true,
                Font = new Font("Arial", 10, FontStyle.Regular),
                Width = 150
            };
            dateTimePicker.ValueChanged += async (s, e) => await LoadOperatorReportAsync();

            topPanel.Controls.Add(dateTimePicker);
            mainLayout.Controls.Add(topPanel, 0, 0); // Add DateTimePicker Panel at row 0

            // Setup GridControl and GridView
            SetupGridControl();
            mainLayout.Controls.Add(gridControl_OperatorReport, 0, 1); // Add GridControl at row 1
        }

        private void SetupGridControl()
        {
            // Initialize GridControl and GridView
            gridControl_OperatorReport = new GridControl
            {
                Dock = DockStyle.Fill // Ensure it fills remaining space
            };

            gridView_OperatorReport = new GridView(gridControl_OperatorReport)
            {
                // You can also customize the GridView here if needed
                OptionsView = { ShowGroupPanel = false }
            };

            // Assign the GridView to the GridControl
            gridControl_OperatorReport.MainView = gridView_OperatorReport;

            // Optionally set grid styling
            gridView_OperatorReport.Appearance.Row.Font = new Font("Arial", 12, FontStyle.Regular);
            gridView_OperatorReport.OptionsBehavior.Editable = false; // Make it read-only
        }

        private async Task LoadOperatorReportAsync()
        {
            try
            {
                int selectedYear = dateTimePicker.Value.Year;
                int selectedMonth = dateTimePicker.Value.Month;

                DataTable dt = await Task.Run(() => DbHelper.reportOrderbyOperator(selectedYear, selectedMonth));

                if (dt != null)
                {
                    if (gridControl_OperatorReport.InvokeRequired)
                    {
                        gridControl_OperatorReport.Invoke(new Action(() => gridControl_OperatorReport.DataSource = dt));
                    }
                    else
                    {
                        gridControl_OperatorReport.DataSource = dt;
                    }
                    TranslateHeaders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TranslateHeaders()
        {
            if (gridControl_OperatorReport.DataSource is DataTable dt)
            {
                if ((dt.Columns.Contains("CreatedAt")))
                    gridView_OperatorReport.Columns["CreatedAt"].Caption = LocalizationManager.GetString("Timestamp");

                if (dt.Columns.Contains("EmployeeID"))
                    gridView_OperatorReport.Columns["EmployeeID"].Caption = LocalizationManager.GetString("OperatorCode");

                if (dt.Columns.Contains("OperatorName"))
                    gridView_OperatorReport.Columns["OperatorName"].Caption = LocalizationManager.GetString("OperatorName");

                if (dt.Columns.Contains("OrderCount"))
                    gridView_OperatorReport.Columns["OrderCount"].Caption = LocalizationManager.GetString("NumberOfOrder");
            }
        }
    }
}