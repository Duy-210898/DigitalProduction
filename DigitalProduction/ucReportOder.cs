using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DigitalProduction
{
    public partial class ucReportOrder : UserControl
    {
        private DateTimePicker dateTimePicker;
        private GridControl gridControl_OperatorReport;
        private GridView gridView_OperatorReport;
        private TableLayoutPanel mainLayout;
        private Button btnExportExcel; // Export Button

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

            // Top Panel for Controls (DateTimePicker & Button)
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };

            // 🕒 DateTimePicker
            dateTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMMM yyyy",
                ShowUpDown = true,
                Font = new Font("Arial", 10, FontStyle.Regular),
                Width = 150,
                Location = new Point(10, 10) // Fixed position
            };
            dateTimePicker.ValueChanged += async (s, e) => await LoadOperatorReportAsync();

            // 📤 Export Button
            btnExportExcel = new Button
            {
                Text = "Export to Excel",
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGreen,
                AutoSize = true,
                Height = dateTimePicker.Height, // Match DateTimePicker height
                Location = new Point(dateTimePicker.Right + 10, dateTimePicker.Top), // Align right with spacing
                Anchor = AnchorStyles.Top | AnchorStyles.Left // Keep alignment on resize
            };
            btnExportExcel.Click += (s, e) => ExportGridViewToExcel();

            // Add Controls to Panel
            topPanel.Controls.Add(dateTimePicker);
            topPanel.Controls.Add(btnExportExcel);

            // Add Panel to Layout
            mainLayout.Controls.Add(topPanel, 0, 0);

            // Setup GridControl and GridView
            SetupGridControl();
            mainLayout.Controls.Add(gridControl_OperatorReport, 0, 1);
        }

        private void SetupGridControl()
        {
            gridControl_OperatorReport = new GridControl
            {
                Dock = DockStyle.Fill
            };

            gridView_OperatorReport = new GridView(gridControl_OperatorReport)
            {
                OptionsView = { ShowGroupPanel = false }
            };

            gridControl_OperatorReport.MainView = gridView_OperatorReport;
            gridView_OperatorReport.Appearance.Row.Font = new Font("Arial", 12, FontStyle.Regular);
            gridView_OperatorReport.OptionsBehavior.Editable = false;
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

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            ExportGridViewToExcel();
        }

        private void ExportGridViewToExcel()
        {
            if (gridView_OperatorReport.DataRowCount == 0)
            {
                MessageBox.Show("No data available to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Save Report",
                FileName = $"Operator_Report_{dateTimePicker.Value:yyyy_MM}.xlsx"
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 🛠 Fix: Set EPPlus License Context
                        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                        using (ExcelPackage package = new ExcelPackage())
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Operator Report");

                            // Get GridView Data
                            DataTable dt = (DataTable)gridControl_OperatorReport.DataSource;

                            if (dt != null)
                            {
                                worksheet.Cells["A1"].LoadFromDataTable(dt, true);

                                // Style Header
                                using (ExcelRange headerRange = worksheet.Cells[1, 1, 1, dt.Columns.Count])
                                {
                                    headerRange.Style.Font.Bold = true;
                                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                                }

                                // ✅ Fix Timestamp Format (MM/dd/yyyy)
                                int timestampColumnIndex = -1;
                                for (int col = 1; col <= dt.Columns.Count; col++)
                                {
                                    if (dt.Columns[col - 1].ColumnName == "CreatedAt") // Match column name
                                    {
                                        timestampColumnIndex = col;
                                        worksheet.Column(timestampColumnIndex).Style.Numberformat.Format = "MM/dd/yyyy";
                                        break;
                                    }
                                }

                                // Auto-Fit Columns
                                worksheet.Cells.AutoFitColumns();
                            }

                            // Save File
                            File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());

                            MessageBox.Show("Report exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
