using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using OfficeOpenXml;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace DigitalProduction
{
    public partial class ucReportOrder : UserControl
    {
        private BindingSource targetBindingSource;

        public ucReportOrder()
        {
            InitializeComponent();
            btnSync.Text = LocalizationManager.GetString("Sync");
            btnSync.Click += SyncButton_Click;
        }
        private async void ucReportOrder_Load(object sender, EventArgs e)
        {
            await SetupUIAsync();
        }

        private async void SyncButton_Click(object sender, EventArgs e)
        {
            try
            {
                await ReloadDataAsync(); // Fetch and load data from the server
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during data sync: {ex.Message}", "Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task SetupUIAsync()
        {
            var parentForm = this.FindForm();
            if (parentForm != null)
            {
                SplashScreenManager.ShowForm(parentForm, typeof(frmLoading), true, true, false);
            }

            try
            {
                // Simulate loading progress (optional)
                for (int i = 1; i <= 100; i += 20)
                {
                    if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                    {
                        SplashScreenManager.Default.SetWaitFormDescription($"Setting up UI... {i}%");
                    }
                    await Task.Delay(10); // async delay, don't block UI thread
                }

                // Initialize the BindingSource
                targetBindingSource = new BindingSource();
                gridViewPeformance.OptionsView.ShowGroupPanel = false;
                gridControlPerformance.DataSource = targetBindingSource;

                // Handle date change - async event handler
                dateTimePicker.ValueChanged += async (s, e) => await ReloadDataAsync();

                // Initial data fetch
                await ReloadDataAsync();

                btnExport.Click += BtnExportAllRows_Click;
                gridViewPeformance.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                gridViewPeformance.Appearance.HeaderPanel.Options.UseFont = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during UI setup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.CloseForm(false);
                }
            }
        }

        private async Task ReloadDataAsync()
        {
            int selectedYear = dateTimePicker.Value.Year;
            int selectedMonth = dateTimePicker.Value.Month;
            var data = await DbHelper.GetRealtimeTargetDataAsync(selectedMonth, selectedYear);


            // Attach event to each item
            foreach (var item in data)
            {
                item.TargetQuantityChanged += async (changedItem) =>
                {
                    try
                    {
                        await SaveTargetQuantityAsync(changedItem);
                    }
                    catch (Exception ex)
                    {
                        // Optionally handle errors here
                        MessageBox.Show($"Error saving target: {ex.Message}");
                    }
                };
            }
            targetBindingSource.DataSource = data;
            targetBindingSource.ResetBindings(false);
            TranslateHeaders();
            gridViewPeformance.RowCellStyle += GridViewPeformance_RowCellStyle;
        }


        private void TranslateHeaders()
        {
            var gridView = gridControlPerformance.MainView as GridView;
            if (gridView == null || gridView.Columns.Count == 0) return;

            foreach (GridColumn col in gridView.Columns)
            {
                var translatedText = LocalizationManager.GetString(col.FieldName);
                col.Caption = !string.IsNullOrEmpty(translatedText) ? translatedText : col.FieldName;

                // Hide specific fields
                if (col.FieldName == "OperatorID" || col.FieldName == "DepartmentId")
                {
                    col.Visible = false;
                }
            }
        }

        private void GridViewPeformance_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column.FieldName == "EfficiencyPercent")
            {
                GridView view = sender as GridView;
                if (view == null) return;

                object cellValue = view.GetRowCellValue(e.RowHandle, e.Column);

                if (cellValue != null)
                {
                    string percentText = cellValue.ToString().Trim().Replace("%", "");
                    if (decimal.TryParse(percentText, out decimal efficiency))
                    {
                        if (efficiency < 80)
                        {
                            e.Appearance.ForeColor = Color.Red;
                        }
                        else
                        {
                            e.Appearance.ForeColor = Color.Green;
                        }

                        e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold);
                    }
                }
            }
        }

        private void BtnExportAllRows_Click(object sender, EventArgs e)
        {
            var view = gridViewPeformance;
            int rowCount = view.DataRowCount;

            if (rowCount == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                sfd.FileName = $"AllTargets_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // Set EPPlus license context
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage())
                    {
                        ExcelWorksheet sheet = package.Workbook.Worksheets.Add("AllRows");

                        // Add headers (adjust if your column headers are different)
                        sheet.Cells[1, 1].Value = LocalizationManager.GetString("OperatorName");
                        sheet.Cells[1, 2].Value = LocalizationManager.GetString("Timestamp");
                        sheet.Cells[1, 3].Value = LocalizationManager.GetString("TargetQuantity");
                        sheet.Cells[1, 4].Value = LocalizationManager.GetString("TargetActualQuantity");
                        sheet.Cells[1, 5].Value = LocalizationManager.GetString("EfficiencyPercent");

                        int rowIndex = 2; // Start from second row for data
                        for (int handle = 0; handle < rowCount; handle++)
                        {
                            var rowData = view.GetRow(handle) as TargetRealtimeInfo;
                            if (rowData != null)
                            {
                                sheet.Cells[rowIndex, 1].Value = rowData.OperatorName;
                                sheet.Cells[rowIndex, 2].Value = rowData.Timestamp.ToString("g");
                                sheet.Cells[rowIndex, 3].Value = rowData.TargetQuantity;
                                sheet.Cells[rowIndex, 4].Value = rowData.TargetActualQuantity;
                                sheet.Cells[rowIndex, 5].Value = rowData.EfficiencyPercent;
                                rowIndex++;
                            }
                        }

                        // Auto-fit columns and wrap text
                        sheet.Cells.AutoFitColumns();
                        sheet.Cells.Style.WrapText = true;

                        // Set row height for data rows
                        for (int i = 2; i < rowIndex; i++)
                        {
                            sheet.Row(i).Height = 45;
                        }
                        // Save the file
                        File.WriteAllBytes(sfd.FileName, package.GetAsByteArray());
                        MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        public class TargetRealtimeInfo : INotifyPropertyChanged
        {

            public int DepartmentId { get; set; }
            public int OperatorID { get; set; }

            private string _operatorName;
            private DateTime _timestamp;
            private int _targetQuantity;
            private int _targetActualQuantity;

            public event Func<TargetRealtimeInfo, Task> TargetQuantityChanged;

            public string OperatorName
            {
                get => _operatorName;
                set
                {
                    if (_operatorName != value)
                    {
                        _operatorName = value;
                        NotifyPropertyChanged(nameof(OperatorName));
                    }
                }
            }

            public DateTime Timestamp
            {
                get => _timestamp;
                set
                {
                    if (_timestamp != value)
                    {
                        _timestamp = value;
                        NotifyPropertyChanged(nameof(Timestamp));
                    }
                }
            }

            public int TargetQuantity
            {
                get => _targetQuantity;
                set
                {
                    if (_targetQuantity != value)
                    {
                        _targetQuantity = value;
                        NotifyPropertyChanged(nameof(TargetQuantity));
                        _ = InvokeTargetQuantityChangedAsync();
                    }
                }
            }

            public int TargetActualQuantity
            {
                get => _targetActualQuantity;
                set
                {
                    if (_targetActualQuantity != value)
                    {
                        _targetActualQuantity = value;
                        NotifyPropertyChanged(nameof(TargetActualQuantity));
                    }
                }
            }

            public string EfficiencyPercent
            {
                get
                {
                    // Check if TargetQuantity is 0 to avoid division by zero
                    if (TargetQuantity == 0)
                        return "0%";

                    // Calculate efficiency and format it to 2 decimal places followed by '%'
                    return $"{Math.Round((decimal)TargetActualQuantity / TargetQuantity * 100, 2)}%";
                }
            }


            private async Task InvokeTargetQuantityChangedAsync()
            {
                if (TargetQuantityChanged != null)
                {
                    var invocationList = TargetQuantityChanged.GetInvocationList();
                    foreach (Func<TargetRealtimeInfo, Task> handler in invocationList)
                    {
                        try
                        {
                            await handler(this);
                        }
                        catch (Exception ex)
                        {
                            // Handle or log exception here safely
                            Console.Error.WriteLine($"Error in TargetQuantityChanged event: {ex}");
                        }
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private static async Task SaveTargetQuantityAsync(TargetRealtimeInfo changedItem)
        {
            if (changedItem == null)
                throw new ArgumentNullException(nameof(changedItem));

            try
            {
                // Example: Update the target quantity in your database
                changedItem.DepartmentId = Global.CurrentUser != null ? Global.CurrentUser.DepartmentID : 0;
                await DbHelper.SaveTargetQuantityAsync(changedItem);

                // Optionally: Refresh data or UI if needed here

            }
            catch (Exception ex)
            {
                // Log or rethrow depending on your error handling strategy
                throw new Exception("Failed to save target quantity.", ex);
            }
        }

    }
}
