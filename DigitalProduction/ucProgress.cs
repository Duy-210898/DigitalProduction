using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucProgress : UserControl
    {
        private BindingList<Distribution> distributionDataList = new BindingList<Distribution>();
        private WebSocketClient _webSocketClient;
        private Panel paginationPanel;
        private Label lblPageInfo;
        private DataGridView dgvProgressManagement;
        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
        private Button btnFilter;
        private Label lblStartDate;
        private Label lblEndDate;

        public ucProgress()
        {
            InitializeComponent();
            LoadTextLabel();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Create a container panel to manage layout
            Panel containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Initialize filter controls at the top
            Panel filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50
            };

            lblStartDate = new Label
            {
                Text = "Start Date:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            dtpStartDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(lblStartDate.Right + 5, 10),
                Width = 100
            };

            lblEndDate = new Label
            {
                Text = "End Date:",
                Location = new Point(dtpStartDate.Right + 10, 15),
                AutoSize = true
            };

            dtpEndDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(lblEndDate.Right + 5, 10),
                Width = 100
            };

            btnFilter = new Button
            {
                Text = "Filter",
                Location = new Point(dtpEndDate.Right + 10, 10),
                Width = 80
            };

            btnFilter.Click += BtnFilter_Click;

            // Add filter controls to the filter panel
            filterPanel.Controls.Add(lblStartDate);
            filterPanel.Controls.Add(dtpStartDate);
            filterPanel.Controls.Add(lblEndDate);
            filterPanel.Controls.Add(dtpEndDate);
            filterPanel.Controls.Add(btnFilter);

            // Initialize DataGridView
            dgvProgressManagement = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                EnableHeadersVisualStyles = false, // Must be set before applying styles
                BackgroundColor = Color.White
            };

            var headerStyle = new DataGridViewCellStyle
            {
                Font = new Font("Arial", 12, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.AntiqueWhite,
                ForeColor = Color.Black
            };

            dgvProgressManagement.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvProgressManagement.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvProgressManagement.ColumnHeadersHeight = 35;

            // Ensure columns exist before applying styles
            dgvProgressManagement.DataBindingComplete += (s, e) =>
            {
                dgvProgressManagement.ColumnHeadersDefaultCellStyle = headerStyle;
                dgvProgressManagement.Refresh();
            };

            dgvProgressManagement.CellFormatting += DgvProgressManagement_CellFormatting;

            // Initialize Pagination Panel
            paginationPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            lblPageInfo = new Label
            {
                Text = "Total Records: 0",
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            Button refreshButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(250, 10)
            };

            refreshButton.Click += async (sender, e) =>
            {
                refreshButton.Enabled = false;
                refreshButton.Text = "Loading...";
                await GetDataAndLoadToGridAsync();
                refreshButton.Enabled = true;
                refreshButton.Text = "Refresh";
            };

            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);

            // Add everything to the container panel in proper order
            containerPanel.Controls.Add(dgvProgressManagement); // Fill remaining space
            containerPanel.Controls.Add(filterPanel); // Stays at the top
            containerPanel.Controls.Add(paginationPanel); // Stays at the bottom

            // Add the container panel to the UserControl
            this.Controls.Add(containerPanel);
        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            DateTime startDate = dtpStartDate.Value.Date;
            DateTime endDate = dtpEndDate.Value.Date.AddDays(1).AddTicks(-1); // Include the whole end day

            var filteredData = new BindingList<Distribution>(new List<Distribution>());

            foreach (var distribution in distributionDataList)
            {
                if (distribution.CreatedAt >= startDate && distribution.CreatedAt <= endDate)
                {
                    filteredData.Add(distribution);
                }
            }

            dgvProgressManagement.DataSource = filteredData;

            // Update the total record label
            lblPageInfo.Text = $"Total Records: {filteredData.Count}";
        }

        private void DgvProgressManagement_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Check if we're formatting the 'IsLeather' column to apply custom style.
            if (dgvProgressManagement.Columns[e.ColumnIndex].Name == "IsLeather")
            {
                if (dgvProgressManagement.Rows[e.RowIndex].Cells["IsLeather"].Value is bool isLeather && isLeather)
                {
                    // Highlight the row.
                    foreach (DataGridViewCell cell in dgvProgressManagement.Rows[e.RowIndex].Cells)
                    {
                        cell.Style.BackColor = Color.LightYellow;
                        cell.Style.Font = new Font(dgvProgressManagement.Font, FontStyle.Bold);
                    }
                }
            }

            // Format the 'Note' column.
            if (dgvProgressManagement.Columns[e.ColumnIndex].Name == "Note" && e.Value is int)
            {
                int noteValue = (int)e.Value;
                switch (noteValue)
                {
                    case 1:
                        e.Value = LocalizationManager.GetString("NotEnoughMaterials");
                        break;
                    case 2:
                        e.Value = LocalizationManager.GetString("ChangeOfPlan");
                        break;
                    case 3:
                        e.Value = LocalizationManager.GetString("ForgotToChooseSize");
                        break;
                    case 0:
                        e.Value = String.Empty;
                        break;
                    default:
                        e.Value = "N/A";
                        break;
                }
                e.FormattingApplied = true;
            }
        }


        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getDistributions" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                ResponseMessage<List<Distribution>> response = ResponseMessage<List<Distribution>>.FromJson(jsonData);

                if (response?.DistributionData != null && response.DistributionData.Count > 0)
                {
                    distributionDataList.Clear();
                    foreach (var distribution in response.DistributionData)
                    {
                        distributionDataList.Add(distribution);
                    }

                    CreateLabelTotalControls();
                    dgvProgressManagement.DataSource = distributionDataList;
                    ConfigureDataGridView();
                }
                else
                {
                    MessageBox.Show("No Data Found", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateLabelTotalControls()
        {
            if (paginationPanel != null)
            {
                this.Controls.Remove(paginationPanel);
                paginationPanel.Dispose();
            }

            paginationPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            lblPageInfo = new Label
            {
                Text = $"Total Records: {distributionDataList.Count}",
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            Button refreshButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(250, 10)
            };

            refreshButton.Click += async (sender, e) =>
            {
                refreshButton.Enabled = false;
                refreshButton.Text = "Loading...";
                await GetDataAndLoadToGridAsync();
                refreshButton.Enabled = true;
                refreshButton.Text = "Refresh";
            };

            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);

            // Ensure pagination panel is added at the bottom
            this.Controls.Add(paginationPanel);
        }
        private void ConfigureDataGridView()
        {
            dgvProgressManagement.Columns["DistributionID"].Visible = false;
            dgvProgressManagement.Columns["InventoryQty"].HeaderText = "Inventory Quantity";
            dgvProgressManagement.Columns["Status"].HeaderText = "Status";

            // Formatting DateTime columns
            foreach (DataGridViewColumn column in dgvProgressManagement.Columns)
            {
                if (column.Name == "CreatedAt")
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy hh:mm";
                }
            }

            // Optionally, set auto-resizing for rows
            dgvProgressManagement.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        private void LoadTextLabel()
        {
            // Assuming LocalizationManager returns strings based on your localization needs
            this.Text = LocalizationManager.GetString("ListOfDistributions");
        }

        private class Distribution
        {
            public int DistributionID { get; set; }
            public string IpAddress { get; set; }
            public string MachineName { get; set; }
            public string PartName { get; set; }
            public string Size { get; set; }
            public string Unit { get; set; }
            public double UnitUsage { get; set; }
            public int SizeQty { get; set; }
            public string MaterialName { get; set; }
            public string OperatorName { get; set; }
            public int InventoryQty { get; set; }
            public string Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsLeather { get; set; }
            public int? Note { get; set; }
        }
    }
}