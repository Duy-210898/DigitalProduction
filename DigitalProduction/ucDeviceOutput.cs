using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using DigitalProduction.ViewModels;

namespace DigitalProduction
{
    public partial class ucDeviceOutput : UserControl
    {
        private DeviceOutputListViewModel _viewModel;
        private DateTimePicker dateTimePickerStart;
        private DateTimePicker dateTimePickerEnd;
        private TextBox txtFilter;
        private GridControl gridControl_DeviceOutput;
        private GridView gridView_DeviceOutput;
        private Panel mainPanel;
        private FlowLayoutPanel filterPanel;

        public ucDeviceOutput()
        {
            InitializeComponent();
            _viewModel = new DeviceOutputListViewModel();
            Console.WriteLine("Uc is loading");

            // Create a main panel for layout management
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5) // Add some padding for better spacing
            };

            // Initialize filter controls
            initFilterDate();
            LoadFilters();

            // Initialize DevExpress GridControl and GridView
            gridControl_DeviceOutput = new GridControl
            {
                Dock = DockStyle.Fill, // Fill remaining space
                Margin = new Padding(5) // Add margin to separate from filters
            };

            gridView_DeviceOutput = new GridView(gridControl_DeviceOutput)
            {
                OptionsView = { ShowGroupPanel = false },
                OptionsBehavior = { Editable = false }
            };

            gridControl_DeviceOutput.MainView = gridView_DeviceOutput;
         
            gridControl_DeviceOutput.DataSource = _viewModel.BindingDeviceOutputs;
            gridView_DeviceOutput.RowStyle += GridView_DeviceOutput_RowStyle;
            gridView_DeviceOutput.CustomColumnDisplayText += GridView_DeviceOutput_CustomColumnDisplayText;
            this.Resize += UcDeviceOutput_Resize;

            dateTimePickerStart.DataBindings.Add("Value", _viewModel, "FilterStartDate", true, DataSourceUpdateMode.OnPropertyChanged);
            dateTimePickerEnd.DataBindings.Add("Value", _viewModel, "FilterEndDate", true, DataSourceUpdateMode.OnPropertyChanged);
            txtFilter.DataBindings.Add("Text", _viewModel, "FilterKeyword", false, DataSourceUpdateMode.OnPropertyChanged);
            dateTimePickerStart.ValueChanged += DateTimePickerStart_ValueChanged;
            dateTimePickerEnd.ValueChanged += DateTimePickerEnd_ValueChanged;

            // Add controls to the main panel
            mainPanel.Controls.Add(gridControl_DeviceOutput);
            this.Controls.Add(mainPanel);
            this.Controls.Add(filterPanel); // Ensure filters stay at the top

            gridControl_DeviceOutput.ForceInitialize();
            // Translate headers (after binding data)
            TranslateHeaders();

            // refresh data
            SimpleButton syncButton = new SimpleButton()
            {
                Text = LocalizationManager.GetString("Sync"),
                Size = new Size(100, 40)
            };
            syncButton.Click += BtnSyncData_Click;
            syncButton.ImageOptions.Image = Properties.Resources.sync_icon;
            filterPanel.Controls.Add(syncButton); 

        }
        private void BtnSyncData_Click(object sender, EventArgs e)
        {
            TranslateHeaders();
           // _viewModel.SyncData();
        }

        private void LoadFilters()
        {
            txtFilter.Text = FilterService.Instance.FilterKeyword;
            dateTimePickerStart.Value = FilterService.Instance.FilterStartDate ?? DateTime.Today;
            dateTimePickerEnd.Value = FilterService.Instance.FilterEndDate ?? DateTime.Today;
        }

        private void DateTimePickerStart_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePickerStart.Value > dateTimePickerEnd.Value)
            {
                dateTimePickerStart.Value = dateTimePickerEnd.Value;
                MessageBox.Show("Start date cannot be after End date!", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DateTimePickerEnd_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePickerEnd.Value < dateTimePickerStart.Value)
            {
                dateTimePickerEnd.Value = dateTimePickerStart.Value;
                MessageBox.Show("End date cannot be before Start date!", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            dateTimePickerStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Margin = new Padding(5) };
            dateTimePickerEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Margin = new Padding(5) };
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

        private void GridView_DeviceOutput_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
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
            _viewModel.SetWebSocketClient(webSocketClient);
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
            gridView.Columns["IsRecentlyUpdated"]?.SetVisible(false);

            StyleNumericColumn(gridView.Columns["ActualSizeQty"], 9);
            StyleNumericColumn(gridView.Columns["TotalPiecesPerPair"], 14, Color.Blue, bold: true);


            gridView.OptionsView.ShowAutoFilterRow = false;
            gridView.OptionsCustomization.AllowFilter = false;
            gridView.OptionsCustomization.AllowSort = false;
            gridView.OptionsMenu.ShowAutoFilterRowItem = false;
            gridView.ActiveFilter.Clear();

            gridView.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView.Appearance.HeaderPanel.Font = new Font(gridView.Appearance.Row.Font, FontStyle.Bold);
            gridView.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            gridView.RowCellStyle -= GridView_DeviceOutput_RowCellStyle;
            gridView.RowCellStyle += GridView_DeviceOutput_RowCellStyle;

            gridView.LayoutChanged();
        }

        void StyleNumericColumn(GridColumn col, int visibleIndex, Color? fontColor = null, bool bold = false)
        {
            if (col == null) return;

            col.VisibleIndex = visibleIndex;
            //col.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
            //col.DisplayFormat.FormatString = "{0:#}";
            //col.OptionsColumn.AllowEdit = false;
            //col.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            //col.AppearanceCell.Options.UseTextOptions = true;
            //col.AppearanceCell.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
            //col.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            if (fontColor.HasValue)
                col.AppearanceCell.ForeColor = fontColor.Value;

            if (bold)
                col.AppearanceCell.Font = new Font(gridView_DeviceOutput.Appearance.Row.Font, FontStyle.Bold);
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
            if (row != null && row.IsRecentlyUpdated)
            {
                e.Appearance.BackColor = Color.LightYellow;
            }
            else
            {
                e.Appearance.BackColor = Color.Transparent;           
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
