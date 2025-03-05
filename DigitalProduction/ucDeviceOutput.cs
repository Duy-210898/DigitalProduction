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

        public ucDeviceOutput()
        {
            InitializeComponent();
            _viewModel = new DeviceOutputListViewModel();

            // Bind UI elements to ViewModel properties
            dataGrid_DeviceOutput.DataSource = _viewModel.BindingDeviceOutputs;
            dataGrid_DeviceOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_DeviceOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dataGrid_DeviceOutput.CellFormatting += DataGrid_DeviceOutput_CellFormatting;
            this.Resize += UcDeviceOutput_Resize;
            dataGrid_DeviceOutput.DataBindingComplete += DataGrid_DeviceOutput_DataBindingComplete;
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
    }
}
