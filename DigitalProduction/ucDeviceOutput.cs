using System;
using System.Drawing;
using System.Windows.Forms;
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

        public ucDeviceOutput()
        {
            InitializeComponent();
            _viewModel = new DeviceOutputListViewModel();
            initFilterDate();
            // Bind UI elements to ViewModel properties
            dataGrid_DeviceOutput.DataSource = _viewModel.BindingDeviceOutputs;
            dataGrid_DeviceOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_DeviceOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dataGrid_DeviceOutput.CellFormatting += DataGrid_DeviceOutput_CellFormatting;
            this.Resize += UcDeviceOutput_Resize;
            dataGrid_DeviceOutput.DataBindingComplete += DataGrid_DeviceOutput_DataBindingComplete;
            // Assuming _viewModel is your DeviceOutputListViewModel instance.
            dateTimePickerStart.DataBindings.Add("Value", _viewModel, "FilterStartDate", true, DataSourceUpdateMode.OnPropertyChanged);
            dateTimePickerEnd.DataBindings.Add("Value", _viewModel, "FilterEndDate", true, DataSourceUpdateMode.OnPropertyChanged);
            txtFilter.DataBindings.Add("Text", _viewModel, "FilterKeyword", false, DataSourceUpdateMode.OnPropertyChanged);
            LoadFilters();
        }

        private void LoadFilters()
        {
            // Assuming you have TextBoxes or DateTimePickers bound to these properties
            txtFilter.Text = FilterService.Instance.FilterKeyword;
            dateTimePickerStart.Value = FilterService.Instance.FilterStartDate ?? DateTime.Today;
            dateTimePickerEnd.Value = FilterService.Instance.FilterEndDate ?? DateTime.Today;
            // Similar for other controls
        }
        private void initFilterDate()
        {
            // Create a FlowLayoutPanel to hold the filter controls and dock it at the top.
            FlowLayoutPanel filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50, // Adjust height as needed.
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, // Keep controls on a single line.
                AutoSize = true
            };

            // Initialize DateTimePicker for Start Date.
            dateTimePickerStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Margin = new Padding(5)
            };

            // Initialize DateTimePicker for End Date.
            dateTimePickerEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Margin = new Padding(5)
            };

            // Initialize txtFilter for searching.
            txtFilter = new TextBox
            {
                Width = 150,
                Margin = new Padding(5),
                ForeColor = Color.Gray,
                Text = LocalizationManager.GetString("Search")
            };

            // Remove placeholder text when focused
            txtFilter.GotFocus += (s, e) =>
            {
                if (txtFilter.Text == LocalizationManager.GetString("Search"))
                {
                    txtFilter.Text = "";
                    txtFilter.ForeColor = Color.Black;
                }
            };

            // Restore placeholder if empty when losing focus
            txtFilter.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFilter.Text))
                {
                    txtFilter.Text = LocalizationManager.GetString("Search");
                    txtFilter.ForeColor = Color.Gray;
                }
            };

            // Add controls to the FlowLayoutPanel.
            filterPanel.Controls.Add(dateTimePickerStart);
            filterPanel.Controls.Add(dateTimePickerEnd);
            filterPanel.Controls.Add(txtFilter); // Add the text filter box

            // Add the panel to the top of the UserControl.
            this.Controls.Add(filterPanel);
        }


        private void DataGrid_DeviceOutput_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dataGrid_DeviceOutput.Columns.Contains("IsLeather"))
            {
                dataGrid_DeviceOutput.Columns["IsLeather"].Visible = false;
            }
            if (dataGrid_DeviceOutput.Columns.Contains("IsGroupHeader"))
            {
                dataGrid_DeviceOutput.Columns["IsGroupHeader"].Visible = false;
            }
        }

        private void UcDeviceOutput_Resize(object sender, EventArgs e)
        {
            if (dataGrid_DeviceOutput.Columns.Count > 0)
            {
                foreach (DataGridViewColumn column in dataGrid_DeviceOutput.Columns)
                {
                    column.Width = dataGrid_DeviceOutput.Width / dataGrid_DeviceOutput.Columns.Count;
                }
            }
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _viewModel.SetWebSocketClient(webSocketClient);
        }

        private void DataGrid_DeviceOutput_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                var row = dataGrid_DeviceOutput.Rows[e.RowIndex].DataBoundItem as DeviceOutput;
                if (row != null)
                {
                    if (row.IsGroupHeader)
                    {
                        e.CellStyle.BackColor = Color.LightGray;
                        e.CellStyle.Font = new Font(dataGrid_DeviceOutput.Font, FontStyle.Bold);
                    }
                    else if (dataGrid_DeviceOutput.Columns[e.ColumnIndex].Name == "IpAddress")
                    {
                        e.Value = string.Empty; // Hide IpAddress for normal rows
                        e.FormattingApplied = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowError("Exception: " + ex.Message);
            }
        }
    }
}
