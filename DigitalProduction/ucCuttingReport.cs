using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid; // Include DevExpress GridControl
using DevExpress.XtraGrid.Views.Grid; // Include DevExpress GridView
using DigitalProduction.Models;

namespace DigitalProduction
{
    public partial class ucCuttingReport : UserControl
    {
        private DateTimePicker dateTimePicker;
        private GridControl gridControl_CuttingReport; // Use GridControl
        private GridView gridView_CuttingReport;      // Use GridView
        private TableLayoutPanel mainLayout;
        private Button btnLoadData;

        public ucCuttingReport()
        {
            InitializeComponent();
            SetupUI();
            _ = LoadCuttingReportAsync();
        }

        private void SetupUI()
        {
            // **Main Layout**
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(mainLayout);

            // **Top Panel (Date Picker + Load Button)**
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

            btnLoadData = new Button
            {
                Text = "Load Data",
                Width = 100,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGray
            };

            dateTimePicker.ValueChanged += async (s, e) => await LoadCuttingReportAsync();
            btnLoadData.Click += async (s, e) => await LoadCuttingReportAsync();

            topPanel.Controls.Add(dateTimePicker);
            topPanel.Controls.Add(btnLoadData);
            mainLayout.Controls.Add(topPanel, 0, 0);

            // **Setup GridControl**
            SetupGridControl();
            mainLayout.Controls.Add(gridControl_CuttingReport, 0, 1);
        }

        private void SetupGridControl()
        {
            // Initialize GridControl and GridView
            gridControl_CuttingReport = new GridControl
            {
                Dock = DockStyle.Fill // Ensure it fills remaining space
            };

            gridView_CuttingReport = new GridView(gridControl_CuttingReport)
            {
                OptionsView = { ShowGroupPanel = false } // Disable grouping panel
            };

            // Assign the GridView to the GridControl
            gridControl_CuttingReport.MainView = gridView_CuttingReport;

            // Set some basic appearance options if desired
            gridView_CuttingReport.OptionsBehavior.Editable = false; // Make it read-only
            gridView_CuttingReport.Appearance.Row.Font = new Font("Arial", 12, FontStyle.Regular);
        }

        private async Task LoadCuttingReportAsync()
        {
            try
            {
                int selectedYear = dateTimePicker.Value.Year;
                int selectedMonth = dateTimePicker.Value.Month;

                List<CuttingReportModel> data = await Task.Run(() => DbHelper.getProductionSummary(selectedYear, selectedMonth));

                if (gridControl_CuttingReport.InvokeRequired)
                {
                    gridControl_CuttingReport.Invoke(new Action(() => UpdateGrid(data)));
                }
                else
                {
                    UpdateGrid(data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates the GridControl with data or clears it when no data is found.
        /// </summary>
        private void UpdateGrid(List<CuttingReportModel> data)
        {
            if (data.Count > 0)
            {
                gridControl_CuttingReport.DataSource = data;
                TranslateHeaders();
            }
            else
            {
                gridControl_CuttingReport.DataSource = null; // Clear grid
                // Optionally, hide. Show message on the grid if needed.
                gridView_CuttingReport.ClearColumnsFilter();
                gridView_CuttingReport.Columns.Clear();
            }
        }

        private void TranslateHeaders()
        {
            if (gridControl_CuttingReport.DataSource is List<CuttingReportModel>)
            {
                if (gridView_CuttingReport.Columns.Count > 0)
                {
                    if (gridView_CuttingReport.Columns["CreatedAt"] != null)
                    {
                        gridView_CuttingReport.Columns["CreatedAt"].Caption = LocalizationManager.GetString("Timestamp");
                        gridView_CuttingReport.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                        gridView_CuttingReport.Columns["CreatedAt"].DisplayFormat.FormatString = "MM/dd/yyyy";
                    }

                    if (gridView_CuttingReport.Columns["MachineName"] != null)
                        gridView_CuttingReport.Columns["MachineName"].Caption = LocalizationManager.GetString("MachineName");

                    if (gridView_CuttingReport.Columns["SO"] != null)
                        gridView_CuttingReport.Columns["SO"].Caption = LocalizationManager.GetString("SO");

                    if (gridView_CuttingReport.Columns["OrderID"] != null)
                        gridView_CuttingReport.Columns["OrderID"].Caption = LocalizationManager.GetString("OrderID");

                    if (gridView_CuttingReport.Columns["OperatorName"] != null)
                        gridView_CuttingReport.Columns["OperatorName"].Caption = LocalizationManager.GetString("OperatorName");

                    if (gridView_CuttingReport.Columns["TotalActualCut"] != null)
                        gridView_CuttingReport.Columns["TotalActualCut"].Caption = LocalizationManager.GetString("TotalActualCut");

                    if (gridView_CuttingReport.Columns["TotalPieces"] != null)
                        gridView_CuttingReport.Columns["TotalPieces"].Caption = LocalizationManager.GetString("TotalPieces");

                    if (gridView_CuttingReport.Columns["TotalSizeQty"] != null)
                        gridView_CuttingReport.Columns["TotalSizeQty"].Caption = LocalizationManager.GetString("TotalSizeQty");
                }
            }
        }
    }
}