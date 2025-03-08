using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalProduction
{
    public partial class ucReportOder : UserControl
    {
        public ucReportOder()
        {
            InitializeComponent();
            SetupDataGridView();
            _ = LoadOperatorReportAsync(); // Run asynchronously to prevent UI freezing
        }

        private void SetupDataGridView()
        {
            // Initialize DataGridView
            dataGridView_OperatorReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Header styling
            var headerStyle = new DataGridViewCellStyle
            {
                Font = new Font("Arial", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.AntiqueWhite,
                ForeColor = Color.Black
            };
            dataGridView_OperatorReport.ColumnHeadersDefaultCellStyle = headerStyle;
            dataGridView_OperatorReport.ColumnHeadersHeight = 35;
            dataGridView_OperatorReport.EnableHeadersVisualStyles = false;

            // Add DataGridView to UserControl
            Controls.Add(dataGridView_OperatorReport);
        }

        private async Task LoadOperatorReportAsync()
        {
            try
            {
                DataTable dt = await Task.Run(() => DbHelper.reportOrderbyOperator()); // Load in background
                if (dt != null)
                {
                    if (dataGridView_OperatorReport.InvokeRequired)
                    {
                        dataGridView_OperatorReport.Invoke(new Action(() => dataGridView_OperatorReport.DataSource = dt));
                    }
                    else
                    {
                        dataGridView_OperatorReport.DataSource = dt;
                    }
                    TranslateHeaders(); // Rename column headers
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TranslateHeaders()
        {
            if (dataGridView_OperatorReport.Columns.Count > 0)
            {
                // Ensure the correct column names are used
                if (dataGridView_OperatorReport.Columns.Contains("OperatorID"))
                    dataGridView_OperatorReport.Columns["OperatorID"].HeaderText = LocalizationManager.GetString("OperatorCode");

                if (dataGridView_OperatorReport.Columns.Contains("OperatorName"))
                    dataGridView_OperatorReport.Columns["OperatorName"].HeaderText = LocalizationManager.GetString("OperatorName");

                if (dataGridView_OperatorReport.Columns.Contains("OrderCount"))
                    dataGridView_OperatorReport.Columns["OrderCount"].HeaderText = LocalizationManager.GetString("NumberOfOrder");
            }
        }
    }
}
