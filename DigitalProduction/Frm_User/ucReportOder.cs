using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
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
                dateTimePickerSchedule.EditValueChanged += async (s, e) => await ReloadDataAsync();

                dateTimePickerSchedule.Properties.VistaCalendarViewStyle = DevExpress.XtraEditors.VistaCalendarViewStyle.YearView;
                dateTimePickerSchedule.Properties.CalendarView = DevExpress.XtraEditors.Repository.CalendarView.Vista;
                dateTimePickerSchedule.Properties.VistaDisplayMode = DevExpress.Utils.DefaultBoolean.True;

                dateTimePickerSchedule.Properties.Mask.EditMask = "yyyy/MM";
                dateTimePickerSchedule.Properties.Mask.UseMaskAsDisplayFormat = true;

                dateTimePickerSchedule.Properties.DisplayFormat.FormatString = "yyyy/MM";
                dateTimePickerSchedule.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                dateTimePickerSchedule.Properties.EditFormat.FormatString = "yyyy/MM";
                dateTimePickerSchedule.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;

                dateTimePickerSchedule.EditValue = DateTime.Now;

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
            int selectedYear = dateTimePickerSchedule.DateTime.Year;
            int selectedMonth = dateTimePickerSchedule.DateTime.Month;

            var rawData = await DbHelper.GetRealtimeTargetDataAsync(selectedMonth, selectedYear);

            var groupedData = GetGroupedData(rawData); // 👈 Apply grouping

            foreach (var operatorSummary in groupedData)
            {
                operatorSummary.TargetQuantityChanged += async (changedSummary) =>
                {
                    try
                    {
                        await SaveTargetQuantityAsync(changedSummary);
                        gridViewPeformance.RefreshData(); // refresh grid master
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving target: {ex.Message}");
                    }
                };
            }
            targetBindingSource.DataSource = groupedData;
            targetBindingSource.ResetBindings(false);
            gridViewPeformance.RowCellStyle -= GridViewPeformance_RowCellStyle;
            gridViewPeformance.RowCellStyle += GridViewPeformance_RowCellStyle;
            TranslateHeaders();
            SetupMasterDetailView(); // 👈 Setup master-detail logic
        }


        private void SetupMasterDetailView()
        {
            gridViewPeformance.BeginUpdate();
            gridViewPeformance.Columns["TargetQuantity"].FieldName = "TargetQuantity";
            gridViewPeformance.OptionsDetail.EnableMasterViewMode = true;
            gridViewPeformance.OptionsDetail.ShowDetailTabs = false;
            gridViewPeformance.OptionsDetail.SmartDetailExpand = true;
            gridViewPeformance.OptionsBehavior.Editable = true;

            gridViewPeformance.MasterRowGetRelationCount += (s, e) => e.RelationCount = 1;
            gridViewPeformance.MasterRowGetRelationName += (s, e) => e.RelationName = "PartSizeDetails";
            gridViewPeformance.MasterRowGetChildList += (s, e) =>
            {
                var row = (OperatorDailySummary)gridViewPeformance.GetRow(e.RowHandle);
                e.ChildList = row?.Details;
            };

            gridViewPeformance.MasterRowExpanded += (s, e) =>
            {
                if (gridViewPeformance.GetDetailView(e.RowHandle, e.RelationIndex) is GridView detailView)
                {
                    detailView.PopulateColumns();
                    TranslateAllDetailViews(gridControlPerformance);
                    detailView.BestFitColumns();
                }
            };

            gridViewPeformance.EndUpdate();
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
        private void TranslateAllDetailViews(GridControl grid)
        {
            var mainView = grid.MainView as GridView;
            if (mainView == null) return;

            // Duyệt từng dòng master
            for (int rowHandle = 0; rowHandle < mainView.RowCount; rowHandle++)
            {
                // Kiểm tra có phải dòng master và đã mở detail
                if (mainView.IsMasterRow(rowHandle) && mainView.GetVisibleDetailView(rowHandle) is GridView detailView)
                {
                    TranslateGridViewHeaders(detailView);
                }
            }
        }
        private void TranslateGridViewHeaders(GridView gridView)
        {
            foreach (GridColumn col in gridView.Columns)
            {
                var translatedText = LocalizationManager.GetString(col.FieldName);
                col.Caption = !string.IsNullOrEmpty(translatedText) ? translatedText : col.FieldName;

                // Ẩn một số cột
                if (col.FieldName == "OperatorID"  || col.FieldName == "OperatorName" || col.FieldName == "DepartmentId" || col.FieldName == "TargetQuantity" || col.FieldName == "EfficiencyPercent" || col.FieldName == "Timestamp")
                    col.Visible = false;
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
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage())
                    {
                        ExcelWorksheet sheet = package.Workbook.Worksheets.Add("AllRows");

                        // Header for master row
                        sheet.Cells[1, 1].Value = LocalizationManager.GetString("OperatorName");
                        sheet.Cells[1, 2].Value = LocalizationManager.GetString("Timestamp");
                        sheet.Cells[1, 3].Value = LocalizationManager.GetString("TargetQuantity");
                        sheet.Cells[1, 4].Value = LocalizationManager.GetString("TargetActualQuantity");
                        sheet.Cells[1, 5].Value = LocalizationManager.GetString("EfficiencyPercent");

                        // Header for detail row (optional, shown if detail rows exist)
                        sheet.Cells[1, 6].Value = LocalizationManager.GetString("PartName");
                        sheet.Cells[1, 7].Value = LocalizationManager.GetString("SizeName");
                        sheet.Cells[1, 8].Value = LocalizationManager.GetString("ActualQuantity");

                        int rowIndex = 2;

                        for (int handle = 0; handle < view.RowCount; handle++)
                        {
                            var rowData = view.GetRow(handle) as OperatorDailySummary;
                            if (rowData != null)
                            {
                                // Export master row
                                sheet.Cells[rowIndex, 1].Value = rowData.OperatorName;
                                sheet.Cells[rowIndex, 2].Value = rowData.Timestamp.ToString("g");
                                sheet.Cells[rowIndex, 3].Value = rowData.TargetQuantity;
                                sheet.Cells[rowIndex, 4].Value = rowData.TargetActualQuantity;
                                sheet.Cells[rowIndex, 5].Value = rowData.EfficiencyPercent;

                                rowIndex++;

                                // Export detail rows (assuming a property List<PartDetail> Details)
                                if (rowData.Details != null && rowData.Details.Any())
                                {
                                    foreach (var detail in rowData.Details)
                                    {
                                        sheet.Cells[rowIndex, 6].Value = detail.PartName;
                                        sheet.Cells[rowIndex, 7].Value = detail.Size;
                                        sheet.Cells[rowIndex, 8].Value = detail.TargetActualQuantity;
                                        rowIndex++;
                                    }
                                }
                            }
                        }

                        // Format and save
                        sheet.Cells.AutoFitColumns();
                        sheet.Cells.Style.WrapText = true;

                        for (int i = 2; i < rowIndex; i++)
                        {
                            sheet.Row(i).Height = 45;
                        }

                        File.WriteAllBytes(sfd.FileName, package.GetAsByteArray());
                        MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        public class OperatorDailySummary : INotifyPropertyChanged
        {
            public string OperatorName { get; set; }
            public DateTime Timestamp { get; set; }
            public int DepartmentId { get; set; }
            public int OperatorID { get; set; }
            private int _targetQuantity;
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
            private int _targetActualQuantity;
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

            public string EfficiencyPercent =>
                TargetQuantity == 0 ? "0%" :
                $"{Math.Round((decimal)TargetActualQuantity / TargetQuantity * 100, 2)}%";

            public List<TargetRealtimeInfo> Details { get; set; }

            public event Func<OperatorDailySummary, Task> TargetQuantityChanged;

            private async Task InvokeTargetQuantityChangedAsync()
            {
                if (TargetQuantityChanged != null)
                {
                    foreach (Func<OperatorDailySummary, Task> handler in TargetQuantityChanged.GetInvocationList())
                    {
                        try { await handler(this); }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error in TargetQuantityChanged event: {ex}");
                        }
                    }
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private List<OperatorDailySummary> GetGroupedData(List<TargetRealtimeInfo> data)
        {
            return data
                .GroupBy(x => new { x.OperatorID, x.TargetQuantity, x.OperatorName, Date = x.Timestamp.Date })
                .Select(g => new OperatorDailySummary
                {
                    OperatorID = g.Key.OperatorID,
                    OperatorName = g.Key.OperatorName,
                    Timestamp = g.Key.Date,
                    TargetActualQuantity = g.Sum(x => x.TargetActualQuantity),
                    TargetQuantity = g.Key.TargetQuantity,
                    Details = g.ToList()
                })
                .ToList();
        }

        public class TargetRealtimeInfo : INotifyPropertyChanged
        {
            private string _operatorName;
            private string _modelName;
            private DateTime _timestamp;
            private int _targetQuantity;
            public int OperatorID { get; set; }

            private string _size;
            private string _partName;

            public event Func<OperatorDailySummary, Task> TargetQuantityChanged;
            public string Model { get => _modelName; set { _modelName = value; NotifyPropertyChanged(nameof(Model)); } }
            public string OperatorName { get => _operatorName; set { _operatorName = value; NotifyPropertyChanged(nameof(OperatorName)); } }
            public DateTime Timestamp { get => _timestamp; set { _timestamp = value; NotifyPropertyChanged(nameof(Timestamp)); } }
            public int TargetQuantity
            {
                get => _targetQuantity;
                set
                {
                    if (_targetQuantity != value)
                    {
                        _targetQuantity = value;
                        NotifyPropertyChanged(nameof(TargetQuantity));
                    }
                }
            }
            private int _targetActualQuantity;
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

            public string Size
            {
                get => _size;
                set { _size = value; NotifyPropertyChanged(nameof(Size)); }
            }

            public string PartName
            {
                get => _partName;
                set { _partName = value; NotifyPropertyChanged(nameof(PartName)); }
            }
            public string EfficiencyPercent =>
              TargetQuantity == 0 ? "0%" :
              $"{Math.Round((decimal)TargetActualQuantity / TargetQuantity * 100, 2)}%";

            //private async Task InvokeTargetQuantityChangedAsync()
            //{
            //    if (TargetQuantityChanged != null)
            //    {
            //        foreach (Func<TargetRealtimeInfo, Task> handler in TargetQuantityChanged.GetInvocationList())
            //        {
            //            try { await handler(this); }
            //            catch (Exception ex)
            //            {
            //                Console.Error.WriteLine($"Error in TargetQuantityChanged event: {ex}");
            //            }
            //        }
            //    }
            //}
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private static async Task SaveTargetQuantityAsync(OperatorDailySummary changedItem)
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
