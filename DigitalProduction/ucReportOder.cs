using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
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
            SetupUI();
            btnSync.Text = LocalizationManager.GetString("Sync");
            btnSync.Click += SyncButton_Click;
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
        private void SetupUI()
        {
            // Initialize the BindingSource
            targetBindingSource = new BindingSource();
            gridViewPeformance.OptionsView.ShowGroupPanel = false;
            gridControlPerformance.DataSource = targetBindingSource;

            // Handle date change
            dateTimePicker.ValueChanged += async (s, e) => await ReloadDataAsync();
            // ⬇ Call once on load (initial fetch)
            _ = ReloadDataAsync();
            btnExport.Click += BtnExportAllRows_Click;
            gridViewPeformance.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            gridViewPeformance.Appearance.HeaderPanel.Options.UseFont = true;
            gridViewPeformance.RowCellStyle += GridViewPeformance_RowCellStyle;
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
                if (col.FieldName == "EmployeeId" || col.FieldName == "DepartmentId")
                {
                    col.Visible = false;
                }
            }
        }

        private void GridViewPeformance_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column.FieldName == "EfficiencyPercent")
            {
            //    e.Appearance.BackColor = System.Drawing.Color.LightGreen;
                e.Appearance.ForeColor = System.Drawing.Color.Green; // Optional: for contrast
                e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold); // Optional
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
            public int EmployeeId { get; set; }

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
