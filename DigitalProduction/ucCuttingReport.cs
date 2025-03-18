using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style; // For styling

namespace DigitalProduction
{
    public partial class ucCuttingReport : UserControl
    {
        private DateTimePicker dateTimePicker;
        private GridControl gridControl_CuttingReport;
        private GridView gridView_CuttingReport;
        private TableLayoutPanel mainLayout;
        private Button btnLoadData, btnExportExcel;

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

            // **Top Panel**
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
                Width = 150,
                Location = new Point(10, 10)
            };

            btnLoadData = new Button
            {
                Text = "Load Data",
                Width = 100,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGray,
                Location = new Point(dateTimePicker.Right + 10, dateTimePicker.Top)
            };

            btnExportExcel = new Button
            {
                Text = "Export to Excel",
                Width = 130,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGreen,
                Location = new Point(btnLoadData.Right + 10, dateTimePicker.Top)
            };

            dateTimePicker.ValueChanged += async (s, e) => await LoadCuttingReportAsync();
            btnLoadData.Click += async (s, e) => await LoadCuttingReportAsync();
            btnExportExcel.Click += (s, e) => ExportToExcel();

            topPanel.Controls.Add(dateTimePicker);
            topPanel.Controls.Add(btnLoadData);
            topPanel.Controls.Add(btnExportExcel);
            mainLayout.Controls.Add(topPanel, 0, 0);

            // **Setup GridControl**
            SetupGridControl();
            mainLayout.Controls.Add(gridControl_CuttingReport, 0, 1);
        }

        private void SetupGridControl()
        {
            gridControl_CuttingReport = new GridControl { Dock = DockStyle.Fill };
            gridView_CuttingReport = new GridView(gridControl_CuttingReport)
            {
                OptionsView = { ShowGroupPanel = false }
            };

            gridControl_CuttingReport.MainView = gridView_CuttingReport;
            gridView_CuttingReport.OptionsBehavior.Editable = false;
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

        private void UpdateGrid(List<CuttingReportModel> data)
        {
            if (data.Count > 0)
            {
                gridControl_CuttingReport.DataSource = data;
                TranslateHeaders();
            }
            else
            {
                gridControl_CuttingReport.DataSource = null;
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

        private void ExportToExcel()
        {
            if (gridControl_CuttingReport.DataSource is List<CuttingReportModel> data && data.Count > 0)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", Title = "Save Report", FileName = $"Cutting_Report_{dateTimePicker.Value:yyyy_MM}.xlsx" })
                {
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        FileInfo fileInfo = new FileInfo(saveFileDialog.FileName);
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Fix license issue

                        using (ExcelPackage package = new ExcelPackage(fileInfo))
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Cutting Report");

                            // **Header Row**
                            worksheet.Cells[1, 1].Value = "Timestamp";
                            worksheet.Cells[1, 2].Value = "Machine Name";
                            worksheet.Cells[1, 3].Value = "SO";
                            worksheet.Cells[1, 4].Value = "Order ID";
                            worksheet.Cells[1, 5].Value = "Operator Name";
                            worksheet.Cells[1, 6].Value = "Total Actual Cut";
                            worksheet.Cells[1, 7].Value = "Total Pieces";
                            worksheet.Cells[1, 8].Value = "Total Size Qty";

                            using (ExcelRange range = worksheet.Cells[1, 1, 1, 8])
                            {
                                range.Style.Font.Bold = true;
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            }

                            // **Data Rows**
                            for (int i = 0; i < data.Count; i++)
                            {
                                worksheet.Cells[i + 2, 1].Value = data[i].CreatedAt; // Keep it as DateTime
                                worksheet.Cells[i + 2, 1].Style.Numberformat.Format = "MM/dd/yyyy"; // Apply Excel date format
                                worksheet.Cells[i + 2, 2].Value = data[i].MachineName;
                                worksheet.Cells[i + 2, 3].Value = data[i].SO;
                                worksheet.Cells[i + 2, 4].Value = data[i].OrderID;
                                worksheet.Cells[i + 2, 5].Value = data[i].OperatorName;
                                worksheet.Cells[i + 2, 6].Value = data[i].TotalActualCut;
                                worksheet.Cells[i + 2, 7].Value = data[i].TotalPieces;
                                worksheet.Cells[i + 2, 8].Value = data[i].TotalSizeQty;
                            }

                            worksheet.Cells.AutoFitColumns();
                            package.Save();
                        }

                        MessageBox.Show("Report saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("No data available to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
