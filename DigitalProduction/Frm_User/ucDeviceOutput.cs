using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Data;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using DigitalProduction.Models;
using DigitalProduction.ViewModels;

namespace DigitalProduction
{
    public partial class ucDeviceOutput : UserControl
    {
        private DeviceOutputListViewModel _viewModel;
        private DateEdit dateTimePickerStart;
        private DateEdit dateTimePickerEnd;
        private TextBox txtFilter;
        private GridControl gridControl_DeviceOutput;
        private GridView gridView_DeviceOutput;
        private Panel mainPanel;
        private FlowLayoutPanel filterPanel;
        private WebSocketClient _pendingWebSocketClient;
        private BindingSource viewModelBindingSource = new BindingSource();
        public ucDeviceOutput()
        {
            InitializeComponent();
            // Defer full loading until control is loaded
            this.Load += async (s, e) =>
            {
                 await LoadWithSplashAsync();
            };

            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                    _viewModel.IsLive = true;
                else
                    _viewModel.IsLive = false;
            };
        }
        private async Task LoadWithSplashAsync()
        {
            // Now safe to initialize _viewModel
            _viewModel = new DeviceOutputListViewModel();
            // Proceed to ViewModel-dependent setup
            InitializeViewModelUI();
            var parentForm = this.FindForm() ?? Application.OpenForms.Cast<Form>().FirstOrDefault();

            if (parentForm != null)
                SplashScreenManager.ShowForm(parentForm, typeof(frmLoading), true, true, false);

            // Simulate progress
            for (int i = 1; i <= 100; i += 20)
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.Default.SetWaitFormDescription($"Loading... {i}%");
                }
                await Task.Delay(10);
            }


            if (_pendingWebSocketClient != null)
            {
                _viewModel.SetWebSocketClient(_pendingWebSocketClient);
                _pendingWebSocketClient = null;
            }
            Console.WriteLine("Uc is loading");

            if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                SplashScreenManager.CloseForm(false);
        }
        private void InitializeViewModelUI()
        {
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            initFilterDate();
            LoadFilters();

            gridControl_DeviceOutput = new GridControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };

            gridView_DeviceOutput = new GridView(gridControl_DeviceOutput)
            {
                OptionsView = { ShowGroupPanel = false },
                OptionsBehavior = { Editable = false }
            };

            // Ensure MainView is correctly set
            gridControl_DeviceOutput.MainView = gridView_DeviceOutput;

            if (_viewModel == null)
            {
                MessageBox.Show("_viewModel is null before binding", "DEBUG");
                return;
            }

            // 1. Bind GridControl to ViewModel's list (e.g., BindingList<DeviceOutput>)
            gridControl_DeviceOutput.DataSource = _viewModel.BindingDeviceOutputs;

            // 2. Setup splash screen actions
            _viewModel.ShowLoadingAction = () =>
            {
                var form = this.FindForm() ?? Application.OpenForms.Cast<Form>().FirstOrDefault();
                if (form != null && !SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.ShowForm(form, typeof(frmLoading), true, true, false);
                }
            };

            _viewModel.HideLoadingAction = () =>
            {
                if (SplashScreenManager.Default?.IsSplashFormVisible == true)
                {
                    SplashScreenManager.CloseForm();
                }
            };

            // 3. Setup viewModelBindingSource and bind to IsLoading (use Inverse for Enabled)
            viewModelBindingSource.DataSource = _viewModel;

            // Optional: avoid adding multiple bindings
            gridControl_DeviceOutput.DataBindings.Clear();

            // Enable or disable grid based on IsLoading
            gridControl_DeviceOutput.DataBindings.Add("Enabled", viewModelBindingSource, "IsLoading", true, DataSourceUpdateMode.OnPropertyChanged);
            // Create the opaque overlay panel
            Panel overlayPanel = new Panel
            {
                Name = "overlayPanel",
                BackColor = Color.White, // Not transparent!
                Visible = false,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();

            // Create DevExpress ProgressPanel
            var progressPanelOverLay = new DevExpress.XtraWaitForm.ProgressPanel
            {
                AutoHeight = true,
                AutoWidth = true,
                Caption = "Please wait...",
                Description = "Loading data...",
                LookAndFeel = { UseDefaultLookAndFeel = true },
                BackColor = Color.Transparent
            };

            // Center manually
            progressPanelOverLay.Location = new Point(
                (overlayPanel.Width - progressPanelOverLay.Width) / 2,
                (overlayPanel.Height - progressPanelOverLay.Height) / 2
            );
            progressPanelOverLay.Anchor = AnchorStyles.None;

            overlayPanel.Controls.Add(progressPanelOverLay);

            // ✅ Bind visibility to ViewModel.IsLoading (NOT Enabled!)
            overlayPanel.DataBindings.Add("Visible", viewModelBindingSource, "IsLoading", true, DataSourceUpdateMode.OnPropertyChanged);

            _viewModel.InitPagingFooter(gridControl_DeviceOutput);
            gridView_DeviceOutput.RowStyle += GridView_DeviceOutput_RowStyle;
            gridView_DeviceOutput.CustomColumnDisplayText += GridView_DeviceOutput_CustomColumnDisplayText;
            this.Resize += UcDeviceOutput_Resize;

            // Bindings
            dateTimePickerStart.DataBindings.Add("EditValue", _viewModel, "FilterStartDate", true, DataSourceUpdateMode.OnPropertyChanged);
            dateTimePickerEnd.DataBindings.Add("EditValue", _viewModel, "FilterEndDate", true, DataSourceUpdateMode.OnPropertyChanged);
            txtFilter.DataBindings.Add("Text", _viewModel, "FilterKeyword", false, DataSourceUpdateMode.OnPropertyChanged);

            // Events
            dateTimePickerStart.EditValueChanged += dateEditStart_EditValueChanged;
            dateTimePickerEnd.EditValueChanged += dateEditEnd_EditValueChanged;


            mainPanel.Controls.Add(gridControl_DeviceOutput);
            this.Controls.Add(mainPanel);
            this.Controls.Add(filterPanel);

            gridControl_DeviceOutput.ForceInitialize();
            TranslateHeaders();

            var syncButton = new SimpleButton
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(150, 40),
                ImageOptions = { Image = Properties.Resources.sync_icon }
            };
            syncButton.Click += BtnSyncData_Click;
            filterPanel.Controls.Add(syncButton);

            gridView_DeviceOutput.OptionsSelection.MultiSelect = true;
            gridView_DeviceOutput.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;
            gridView_DeviceOutput.OptionsBehavior.EditorShowMode = EditorShowMode.MouseDown;
            gridView_DeviceOutput.OptionsSelection.EnableAppearanceFocusedCell = true;
            gridView_DeviceOutput.OptionsSelection.EnableAppearanceHideSelection = false;
            gridView_DeviceOutput.OptionsView.ShowFooter = true;
            gridView_DeviceOutput.OptionsSelection.EnableAppearanceFocusedRow = true;

            gridView_DeviceOutput.SelectionChanged += gridView_DeviceOutput_SelectionChanged;
            gridView_DeviceOutput.CustomSummaryCalculate += gridView_DeviceOutput_CustomSummaryCalculate;

            //var sizeColumn = gridView_DeviceOutput.Columns.ColumnByFieldName("ActualSizeQty");
            //if (sizeColumn != null)
            //{
            //    sizeColumn.Width = 120;
            //    sizeColumn.OptionsColumn.FixedWidth = true;
            //    sizeColumn.SummaryItem.SummaryType = SummaryItemType.Custom;
            //    sizeColumn.SummaryItem.DisplayFormat = $"{LocalizationManager.GetString("TotalRecords")}: {{0:N2}}";
            //}
            //else
            //{
            //    MessageBox.Show("Cột 'ActualCut' không tồn tại. Kiểm tra FieldName trong nguồn dữ liệu.");
            //}

            gridView_DeviceOutput.CustomDrawFooterCell += GridView_DeviceOutput_CustomDrawFooterCell;
            gridView_DeviceOutput.RowStyle += GridView_DeviceOutput_RowStyle;


            // hide text if child deviceoutput 
            gridView_DeviceOutput.CustomDrawCell += (s, e) =>
            {
                var row = gridView_DeviceOutput.GetRow(e.RowHandle) as DeviceOutput;
                if (row != null && !row.IsGroupHeader)
                {
                    if (e.Column.FieldName == "SO" || e.Column.FieldName == "OperatorName" ||
                        e.Column.FieldName == "PartName" || e.Column.FieldName == "MachineName" ||
                        e.Column.FieldName == "MaterialType")
                    {
                        e.DisplayText = "";
                    }
                }
            };
        }

        private void GridView_DeviceOutput_CustomDrawFooterCell(object sender, FooterCellCustomDrawEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "ActualSizeQty")
            {
                e.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                e.Appearance.BackColor = Color.LightYellow;
                e.Appearance.ForeColor = Color.DarkBlue;
                e.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
            }
        }

        private void gridView_DeviceOutput_RowStyle(object sender, RowStyleEventArgs e)
        {
            GridView view = sender as GridView;

            if (
                e.RowHandle >= 0)
            {
                // Focused row
                if (view.FocusedRowHandle == e.RowHandle)
                {
                    e.Appearance.BackColor = Color.LightSkyBlue;
                    e.Appearance.ForeColor = Color.Black;
                }

                if (view.IsRowSelected(e.RowHandle))
                {
                    e.Appearance.BackColor = Color.LightGreen;
                    e.Appearance.ForeColor = Color.Black;
                }
            }
        }

        private int totalSummary = 0;
        private void gridView_DeviceOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridView view = sender as GridView;
            totalSummary = 0;

            var selectedCells = view.GetSelectedCells();
            foreach (GridCell cell in selectedCells)
            {
                if (cell.Column.FieldName == "ActualSizeQty")
                {
                    object val = view.GetRowCellValue(cell.RowHandle, cell.Column);
                    if (val != DBNull.Value && val != null)
                        totalSummary += (int) val;
                }
            }

            gridView_DeviceOutput.UpdateSummary();   // Gọi lại để footer cập nhật giá trị mới
        }

        private void gridView_DeviceOutput_CustomSummaryCalculate(object sender, CustomSummaryEventArgs e)
        {
            if (e.SummaryProcess == CustomSummaryProcess.Finalize && e.IsTotalSummary)
            {
                if (e.Item is GridSummaryItem item && item.FieldName == "ActualSizeQty")
                {
                    e.TotalValue = totalSummary;
                }
            }
        }

        private void BtnSyncData_Click(object sender, EventArgs e)
        {
            TranslateHeaders();
            _ = _viewModel.SyncDataAsync();
        }

        private void LoadFilters()
        {
            txtFilter.Text = FilterService.Instance.FilterKeyword;
            dateTimePickerStart.EditValue = FilterService.Instance.FilterStartDate ?? DateTime.Today;
            dateTimePickerEnd.EditValue = FilterService.Instance.FilterEndDate ?? DateTime.Today;
        }

        private void dateEditStart_EditValueChanged(object sender, EventArgs e)
        {
            if (dateTimePickerStart.EditValue != null && dateTimePickerEnd.EditValue != null)
            {
                DateTime startDate = (DateTime)dateTimePickerStart.EditValue;
                DateTime endDate = (DateTime)dateTimePickerEnd.EditValue;

                if (startDate > endDate)
                {
                    dateTimePickerStart.EditValue = endDate;
                    MessageBox.Show("Start date cannot be after End date!", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                gridView_DeviceOutput.RefreshData();
            }
        }

        private void dateEditEnd_EditValueChanged(object sender, EventArgs e)
        {
            if (dateTimePickerStart.EditValue != null && dateTimePickerEnd.EditValue != null)
            {
                DateTime startDate = (DateTime)dateTimePickerStart.EditValue;
                DateTime endDate = (DateTime)dateTimePickerEnd.EditValue;

                if (endDate < startDate)
                {
                    dateTimePickerEnd.EditValue = startDate;
                    MessageBox.Show("End date cannot be before Start date!", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                gridView_DeviceOutput.RefreshData();
            }
        }

        private void initFilterDate()
        {
            filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            dateTimePickerStart = new DateEdit
            {
                Properties =
                {
                    CalendarTimeProperties = { },
                    DisplayFormat = { FormatType = FormatType.DateTime, FormatString = "d" },
                    EditFormat = { FormatType = FormatType.DateTime, FormatString = "d" },
                },
                Margin = new Padding(5),
                Width = 200
            };

            dateTimePickerEnd = new DateEdit {
                Properties =
                {
                    CalendarTimeProperties = { },
                    DisplayFormat = { FormatType = FormatType.DateTime, FormatString = "d" },
                    EditFormat = { FormatType = FormatType.DateTime, FormatString = "d" },
                },
                Margin = new Padding(5),
                Width = 200
            };

            txtFilter = new TextBox { Width = 150, Margin = new Padding(5), ForeColor = Color.Gray, Text = LocalizationManager.GetString("Search") };

            txtFilter.GotFocus += (s, e) =>
            {
                if (txtFilter.Text == LocalizationManager.GetString("Search"))
                {
                    txtFilter.Text = "";
                    txtFilter.ForeColor = Color.Black;
                }
            };

            txtFilter.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFilter.Text))
                {
                    txtFilter.Text = LocalizationManager.GetString("Search");
                    txtFilter.ForeColor = Color.Gray;
                }
            };

            filterPanel.Controls.Add(dateTimePickerStart);
            filterPanel.Controls.Add(dateTimePickerEnd);
            filterPanel.Controls.Add(txtFilter);
        }

        private void GridView_DeviceOutput_RowStyle(object sender, RowStyleEventArgs e)
        {
            var row = gridView_DeviceOutput.GetRow(e.RowHandle) as DeviceOutput;
            if (row != null && row.IsGroupHeader)
            {
                e.Appearance.BackColor = Color.LightGray;
                e.Appearance.Font = new Font(gridView_DeviceOutput.Appearance.Row.Font, FontStyle.Regular);
            }
        }

        private void GridView_DeviceOutput_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "IpAddress" && e.ListSourceRowIndex >= 0)
            {
                e.DisplayText = string.Empty; // Hide IpAddress
            }
        }

        private void UcDeviceOutput_Resize(object sender, EventArgs e)
        {
            gridView_DeviceOutput.BestFitColumns();
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_viewModel != null)
            {
                _viewModel.SetWebSocketClient(webSocketClient);
            }
            else
            {
                _pendingWebSocketClient = webSocketClient;
            }
        }


        private void TranslateHeaders()
        {
            var gridView = gridControl_DeviceOutput.MainView as GridView;
            if (gridView == null || gridView.Columns.Count == 0) return;

            foreach (GridColumn col in gridView.Columns)
            {
                var translatedText = LocalizationManager.GetString(col.FieldName);
                col.Caption = !string.IsNullOrEmpty(translatedText) ? translatedText : col.FieldName;

            }

            gridView.Columns["IsLeather"]?.SetVisible(false);
            gridView.Columns["IsGroupHeader"]?.SetVisible(false);
            gridView.Columns["_isRecentlyUpdated"]?.SetVisible(false);
            gridView.Columns["IsRecentlyUpdated"]?.SetVisible(false);

            StyleNumericColumn(gridView.Columns["PartName"], 5, bold: true);
            StyleNumericColumn(gridView.Columns["ActualSizeQty"], 7);
            StyleNumericColumn(gridView.Columns["TotalPiecesPerPair"], 13, Color.Blue, bold: true);
            StyleNumericColumn(gridView.Columns["ActualCut"], 0, bold: true);
            StyleNumericColumn(gridView.Columns["ActualPieces"], 0, bold: true);
            StyleNumericColumn(gridView.Columns["InventoryQty"], 0, bold: true);
            StyleNumericColumn(gridView.Columns["CuttingDieQty"], 9, bold: true);
            StyleNumericColumn(gridView.Columns["MaterialLayer"], 0, bold: true);
            StyleNumericColumn(gridView.Columns["SizeQty"], 6, bold: true);
            StyleNumericColumn(gridView.Columns["PiecesPerPair"], 0, bold: true);


            gridView.OptionsView.ShowAutoFilterRow = true;
            gridView.OptionsCustomization.AllowFilter = false;
            gridView.OptionsCustomization.AllowSort = false;
            gridView.OptionsMenu.ShowAutoFilterRowItem = false;
            gridView.Columns["MachineName"].Width = 110;
            gridView.Columns["ActualSizeQty"].Width = 100;
            gridView.Columns["OperatorName"].Width = 130;
            gridView.Columns["Size"].Width = 60;
            gridView.Columns["PartName"].Width = 100;
            gridView.Columns["SO"].Width = 80;
            gridView.Columns["MaterialType"].Width = 100;
            gridView.Columns["SizeQty"].Width = 70;
            gridView.Columns["SO"].VisibleIndex = 0;
           // gridView.Columns["TotalPiecesPerPair"].VisibleIndex = 12;
            gridView.Columns["Timestamp"].Visible= false;
            gridView.Columns["UpdatedAt"].Visible = false;

            gridView.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView.Appearance.HeaderPanel.Font = new Font(gridView.Appearance.Row.Font, FontStyle.Bold);
            gridView.Appearance.HeaderPanel.TextOptions.HAlignment = HorzAlignment.Center;

            gridView.RowCellStyle -= GridView_DeviceOutput_RowCellStyle;
            gridView.RowCellStyle += GridView_DeviceOutput_RowCellStyle;

            gridView.LayoutChanged();
        }

        void StyleNumericColumn(GridColumn col, int visibleIndex, Color? fontColor = null, bool bold = false)
        {
            if (col == null) return;

            if (visibleIndex != 0)
            {
                col.VisibleIndex = visibleIndex;
            }
            col.DisplayFormat.FormatType = FormatType.Numeric;
            col.DisplayFormat.FormatString = "{0:#}";
            col.OptionsColumn.AllowEdit = false;

            // === Force Appearance for Cell ===
            col.AppearanceCell.Options.UseTextOptions = true;
            col.AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;

            if (fontColor.HasValue)
            {
                col.AppearanceCell.Options.UseForeColor = true;
                col.AppearanceCell.ForeColor = fontColor.Value;
            }

            if (bold)
            {
                col.AppearanceCell.Options.UseFont = true;
                Font currentFont = gridView_DeviceOutput.Appearance.Row.Font ?? SystemFonts.DefaultFont;
                col.AppearanceCell.Font = new Font(currentFont, FontStyle.Bold);
            }

            // === Header Appearance ===
            col.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
            col.AppearanceHeader.Options.UseTextOptions = true;

            if (fontColor.HasValue)
            {
                col.AppearanceHeader.Options.UseForeColor = true;
                col.AppearanceHeader.ForeColor = fontColor.Value;
            }

            if (bold)
            {
                col.AppearanceHeader.Options.UseFont = true;
                Font currentHeaderFont = gridView_DeviceOutput.Appearance.HeaderPanel.Font ?? SystemFonts.DefaultFont;
                col.AppearanceHeader.Font = new Font(currentHeaderFont, FontStyle.Bold);
            }
        }

        private void GridView_DeviceOutput_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column.FieldName == "ActualSizeQty")
            {
                e.Appearance.ForeColor = Color.ForestGreen;
                e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold);
            }
            var view = sender as GridView;
            if (view == null || e.RowHandle < 0) return;

            var row = view.GetRow(e.RowHandle) as DeviceOutput;
            if (row.IsRecentlyUpdated
                )
            {
                e.Appearance.BackColor = Color.LightYellow;
                e.Appearance.BackColor2 = Color.LightYellow;
            }
            else
            {
                e.Appearance.BackColor = Color.Transparent;
                e.Appearance.BackColor2 = Color.Transparent;
            }
        }
    }
    public static class GridColumnExtensions
    {
        public static void SetVisible(this GridColumn column, bool isVisible)
        {
            if (column != null)
            {
                column.Visible = isVisible;
            }
        }
    }
}
