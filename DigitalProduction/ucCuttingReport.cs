using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;

namespace DigitalProduction
{
    public partial class ucCuttingReport : UserControl
    {
        private DateTimePicker dateTimePicker;
        private DataGridView dataGridView_CuttingReport;
        private TableLayoutPanel mainLayout;
        private Button btnLoadData;

        public ucCuttingReport()
        {
            InitializeComponent();
            SetupUI();
            _ = LoadCuttingReportAsync();
        }

        private void SetupUI()
        {
            // **Main Layout**
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(mainLayout);

            // **Top Panel (Date Picker + Load Button)**
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

            btnLoadData = new Button
            {
                Text = "Load Data",
                Width = 100,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGray
            };

            dateTimePicker.ValueChanged += async (s, e) => await LoadCuttingReportAsync();
            btnLoadData.Click += async (s, e) => await LoadCuttingReportAsync();

            topPanel.Controls.Add(dateTimePicker);
            topPanel.Controls.Add(btnLoadData);
            topPanel.Controls.SetChildIndex(dateTimePicker, 0);
            topPanel.Controls.SetChildIndex(btnLoadData, 1);

            mainLayout.Controls.Add(topPanel, 0, 0);

            // **Setup DataGridView**
            SetupDataGridView();
            mainLayout.Controls.Add(dataGridView_CuttingReport, 0, 1);
        }

        private void SetupDataGridView()
        {
            dataGridView_CuttingReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var headerStyle = new DataGridViewCellStyle
            {
                Font = new Font("Arial", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
            dataGridView_CuttingReport.ColumnHeadersDefaultCellStyle = headerStyle;
            dataGridView_CuttingReport.ColumnHeadersHeight = 40;
            dataGridView_CuttingReport.EnableHeadersVisualStyles = false;
        }

        private async Task LoadCuttingReportAsync()
        {
            try
            {
                int selectedYear = dateTimePicker.Value.Year;
                int selectedMonth = dateTimePicker.Value.Month;

                List<CuttingReportModel> data = await Task.Run(() => DbHelper.getProductionSummary(selectedYear, selectedMonth));

                if (dataGridView_CuttingReport.InvokeRequired)
                {
                    dataGridView_CuttingReport.Invoke(new Action(() => UpdateGrid(data)));
                }
                else
                {
                    UpdateGrid(data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates the DataGridView with data or clears it when no data is found.
        /// </summary>
        private void UpdateGrid(List<CuttingReportModel> data)
        {
            if (data.Count > 0)
            {
                dataGridView_CuttingReport.DataSource = data;
                TranslateHeaders();
            }
            else
            {
                dataGridView_CuttingReport.DataSource = null; // Clear grid
                dataGridView_CuttingReport.Rows.Clear();
                dataGridView_CuttingReport.Refresh();

                // Optionally, add a placeholder row (if columns exist)
                if (dataGridView_CuttingReport.Columns.Count > 0)
                {
                    dataGridView_CuttingReport.Rows.Add(new object[] { "No data available" });
                }
            }
        }


        private void TranslateHeaders()
        {
            if (dataGridView_CuttingReport.Columns.Count > 0)
            {
                if (dataGridView_CuttingReport.Columns.Contains("MachineName"))
                    dataGridView_CuttingReport.Columns["MachineName"].HeaderText = LocalizationManager.GetString("MachineName");

                if (dataGridView_CuttingReport.Columns.Contains("SO"))
                    dataGridView_CuttingReport.Columns["SO"].HeaderText = LocalizationManager.GetString("SO");

                if (dataGridView_CuttingReport.Columns.Contains("OrderID"))
                    dataGridView_CuttingReport.Columns["OrderID"].HeaderText = LocalizationManager.GetString("OrderID");

                if (dataGridView_CuttingReport.Columns.Contains("OperatorName"))
                    dataGridView_CuttingReport.Columns["OperatorName"].HeaderText = LocalizationManager.GetString("OperatorName");

                if (dataGridView_CuttingReport.Columns.Contains("TotalActualCut"))
                    dataGridView_CuttingReport.Columns["TotalActualCut"].HeaderText = LocalizationManager.GetString("TotalActualCut");

                if (dataGridView_CuttingReport.Columns.Contains("TotalPieces"))
                    dataGridView_CuttingReport.Columns["TotalPieces"].HeaderText = LocalizationManager.GetString("TotalPieces");

                if (dataGridView_CuttingReport.Columns.Contains("TotalSizeQty"))
                    dataGridView_CuttingReport.Columns["TotalSizeQty"].HeaderText = LocalizationManager.GetString("TotalSizeQty");
            }
        }
    }
}
