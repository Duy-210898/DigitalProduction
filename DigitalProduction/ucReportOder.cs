using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalProduction
{
    public partial class ucReportOrder : UserControl
    {
        private DateTimePicker dateTimePicker;
        private DataGridView dataGridView_OperatorReport;
        private TableLayoutPanel mainLayout; // Ensures proper structure

        public ucReportOrder()
        {
            InitializeComponent();
            SetupUI();
            _ = LoadOperatorReportAsync(); // Load initial data
        }

        private void SetupUI()
        {
            // **Main Layout Panel**
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Top panel fixed height
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView fills rest
            Controls.Add(mainLayout);

            // **Top Panel for DateTimePicker**
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
                Width = 150
            };
            dateTimePicker.ValueChanged += async (s, e) => await LoadOperatorReportAsync();

            topPanel.Controls.Add(dateTimePicker);
            mainLayout.Controls.Add(topPanel, 0, 0); // Add DateTimePicker Panel at row 0

            // **Setup DataGridView**
            SetupDataGridView();
            mainLayout.Controls.Add(dataGridView_OperatorReport, 0, 1); // Add DataGridView at row 1
        }

        private void SetupDataGridView()
        {
            dataGridView_OperatorReport = new DataGridView
            {
                Dock = DockStyle.Fill, // Ensure it fills remaining space
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };

            var headerStyle = new DataGridViewCellStyle
            {
                Font = new Font("Arial", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
            dataGridView_OperatorReport.ColumnHeadersDefaultCellStyle = headerStyle;
            dataGridView_OperatorReport.ColumnHeadersHeight = 40;
            dataGridView_OperatorReport.EnableHeadersVisualStyles = false;
        }

        private async Task LoadOperatorReportAsync()
        {
            try
            {
                int selectedYear = dateTimePicker.Value.Year;
                int selectedMonth = dateTimePicker.Value.Month;

                DataTable dt = await Task.Run(() => DbHelper.reportOrderbyOperator(selectedYear, selectedMonth));

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
                    TranslateHeaders();
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
                if (dataGridView_OperatorReport.Columns.Contains("EmployeeID"))
                    dataGridView_OperatorReport.Columns["EmployeeID"].HeaderText = LocalizationManager.GetString("OperatorCode");

                if (dataGridView_OperatorReport.Columns.Contains("OperatorName"))
                    dataGridView_OperatorReport.Columns["OperatorName"].HeaderText = LocalizationManager.GetString("OperatorName");

                if (dataGridView_OperatorReport.Columns.Contains("OrderCount"))
                    dataGridView_OperatorReport.Columns["OrderCount"].HeaderText = LocalizationManager.GetString("NumberOfOrder");
            }
        }
    }
}
