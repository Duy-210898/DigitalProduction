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
            // Assuming dataGridView1 is your DataGridView
            dataGrid_DeviceOutput.Columns["IsLeather"].Visible = false;
            dataGrid_DeviceOutput.Columns["IsGroupHeader"].Visible = false;

            dataGrid_DeviceOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGrid_DeviceOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dataGrid_DeviceOutput.CellFormatting += DataGrid_DeviceOutput_CellFormatting;
            this.Resize += UcDeviceOutput_Resize;
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
