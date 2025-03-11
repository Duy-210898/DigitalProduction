using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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
        private TextBox txtSearch;


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

            // Use FlowLayoutPanel for better alignment
            FlowLayoutPanel filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            lblStartDate = new Label
            {
                Text = "Start Date:",
                AutoSize = true,
                Margin = new Padding(5, 15, 5, 5)
            };

            dtpStartDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 100,
                Margin = new Padding(5, 10, 10, 5)
            };

            lblEndDate = new Label
            {
                Text = "End Date:",
                AutoSize = true,
                Margin = new Padding(5, 15, 5, 5)
            };

            dtpEndDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 100,
                Margin = new Padding(5, 10, 10, 5)
            };

            txtSearch = new TextBox
            {
                Text = LocalizationManager.GetString("Search"),
                Width = 200,
                Margin = new Padding(10, 10, 10, 5)
            };

            txtSearch.Enter += TxtSearch_Enter;
            txtSearch.Leave += TxtSearch_Leave;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            dtpStartDate.ValueChanged += DateTimePicker_ValueChanged;
            dtpEndDate.ValueChanged += DateTimePicker_ValueChanged;

            // Add controls to filter panel
            filterPanel.Controls.Add(lblStartDate);
            filterPanel.Controls.Add(dtpStartDate);
            filterPanel.Controls.Add(lblEndDate);
            filterPanel.Controls.Add(dtpEndDate);
            filterPanel.Controls.Add(txtSearch);

            // Initialize DataGridView
            dgvProgressManagement = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                EnableHeadersVisualStyles = false,
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
            // Create a new TextBox column for "IsLeather"
            DataGridViewTextBoxColumn isLeatherTextColumn = new DataGridViewTextBoxColumn
            {
                Name = "IsLeather",
                HeaderText = "Material Type",
                DataPropertyName = "IsLeather"
            };

            // Insert "IsLeather" column at the last position
            dgvProgressManagement.Columns.Add(isLeatherTextColumn);

            dgvProgressManagement.CellFormatting += DgvProgressManagement_CellFormatting;

            // Initialize Pagination Panel
            // Pagination Panel
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

            // Add components to container panel
            containerPanel.Controls.Add(dgvProgressManagement);
            containerPanel.Controls.Add(filterPanel);
            containerPanel.Controls.Add(paginationPanel);

            // Add container panel to the UserControl
            this.Controls.Add(containerPanel);
        }

        // Event handlers for placeholder functionality
        private void TxtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == LocalizationManager.GetString("Search"))
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Black;
            }
        }

        private void TxtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = LocalizationManager.GetString("Search");
                txtSearch.ForeColor = Color.Gray;
            }
        }

        private void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            // Get current values from DateTimePickers
            DateTime newStartDate = dtpStartDate.Value.Date;
            DateTime newEndDate = dtpEndDate.Value.Date;

            // Check for invalid date range
            if (newStartDate > newEndDate)
            {
                AdjustDates(sender, ref newStartDate, ref newEndDate);
                ShowInvalidDateMessage();
            }
            else
            {
                // If dates are valid, filter data
                Console.WriteLine("Text search: " + txtSearch.Text);
                FilterData(newStartDate, newEndDate, txtSearch.Text == LocalizationManager.GetString("Search") ? "" : txtSearch.Text.Trim());
            }
        }

        private void AdjustDates(object sender, ref DateTime newStartDate, ref DateTime newEndDate)
        {
            // Temporarily unsubscribe from the ValueChanged event to prevent recursion
            if (sender == dtpStartDate)
            {
                dtpStartDate.ValueChanged -= DateTimePicker_ValueChanged;
                newStartDate = newEndDate; // Roll back to endDate
                dtpStartDate.Value = newStartDate;
                dtpStartDate.ValueChanged += DateTimePicker_ValueChanged;
            }
            else if (sender == dtpEndDate)
            {
                dtpEndDate.ValueChanged -= DateTimePicker_ValueChanged;
                newEndDate = newStartDate; // Roll back to startDate
                dtpEndDate.Value = newEndDate;
                dtpEndDate.ValueChanged += DateTimePicker_ValueChanged;
            }
        }

        private void ShowInvalidDateMessage()
        {
            MessageBox.Show("Invalid date range! Start date cannot be after End date.",
                            "Date Selection Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // This will ensure search text has no placeholders and is trimmed.
            string searchInput = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchInput) && searchInput != "Search...")
            {
                FilterData(dtpStartDate.Value.Date, dtpEndDate.Value.Date, searchInput);
            }
            else
            {
                FilterData(dtpStartDate.Value.Date, dtpEndDate.Value.Date, "");
            }
        }

        private void FilterData(DateTime startDate, DateTime endDate, string searchText)
        {
            endDate = endDate.AddDays(1).AddTicks(-1); // Include the full end day
            searchText = searchText.ToLower();

            var filteredData = new BindingList<Distribution>(
                distributionDataList.Where(distribution =>
                    distribution.CreatedAt >= startDate &&
                    distribution.CreatedAt <= endDate &&
                    (string.IsNullOrEmpty(searchText) ||
                    distribution.IpAddress.ToLower().Contains(searchText) ||
                    distribution.MachineName.ToLower().Contains(searchText) ||
                    distribution.OperatorName.ToLower().Contains(searchText) ||
                    distribution.EmployeeName.ToLower().Contains(searchText))
                ).ToList()
            );

            UpdateDataGridView(filteredData);
        }

        private void UpdateDataGridView(BindingList<Distribution> filteredData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateDataGridView(filteredData)));
                return;
            }

            dgvProgressManagement.DataSource = filteredData;
            lblPageInfo.Text = $"Total Records: {filteredData.Count}";
        }

        private void DgvProgressManagement_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                DataGridViewRow row = dgvProgressManagement.Rows[e.RowIndex];

                // Apply style if "Status" is "Complete"
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    DataGridViewColumn column = dgvProgressManagement.Columns[e.ColumnIndex];

                    // Apply green text only to the "Status" column
                    if (column.Name == "Status")
                    {
                        string status = e.Value?.ToString();
                        if (status == "Complete")
                        {
                            e.CellStyle.Font = new Font(dgvProgressManagement.Font, FontStyle.Bold);
                            e.CellStyle.ForeColor = Color.Green; // Apply green text only to "Status" column
                        }
                        else if (status == "Pending") {
                            // Reset style if not "Complete"
                            e.CellStyle.Font = new Font(dgvProgressManagement.Font, FontStyle.Bold);
                            e.CellStyle.ForeColor = Color.Orange;
                        }
                        else
                        {
                            // Reset style if not "Complete"
                            e.CellStyle.Font = new Font(dgvProgressManagement.Font, FontStyle.Bold);
                            e.CellStyle.ForeColor = Color.Red;
                        }
                        e.FormattingApplied = true;
                    }
                }
            }

            if (dgvProgressManagement.Columns[e.ColumnIndex].Name == "IsLeather")
            {
                e.Value = (Convert.ToBoolean(e.Value)) ? LocalizationManager.GetString("leatherMaterial") : LocalizationManager.GetString("rawMaterial");
                e.FormattingApplied = true;
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



        private bool _isDataLoaded = false; // Track if data has been loaded

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage; // Unsubscribe previous instance
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;

            if (!_isDataLoaded)
            {
                _ = GetDataAndLoadToGridAsync();
            }
        }


        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getDistributions" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            try
            {
                string response = await _webSocketClient.SendAsync(jsonRequest);
                if (string.IsNullOrEmpty(response))
                {
                    Console.WriteLine("Received null or empty response from WebSocket.");
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Console.WriteLine($"Response received: {response}");
                WebSocket_OnMessage(response); // Process WebSocket response

                _isDataLoaded = true; // Mark data as loaded (but we will reset it in Refresh)
            }
            catch (TimeoutException)
            {
                Console.WriteLine("WebSocket request timed out.");
                MessageBox.Show("Request timed out for UserControlA.");
            }
        }


        private void WebSocket_OnMessage(string jsonData)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return; // Prevent accessing disposed controls

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => WebSocket_OnMessage(jsonData)));
                return;
            }

            try
            {
                ResponseMessage<List<Distribution>> response = ResponseMessage<List<Distribution>>.FromJson(jsonData);

                if (response?.DistributionData != null && response.DistributionData.Count > 0)
                {
                    distributionDataList.Clear();
                    distributionDataList = new BindingList<Distribution>(response.DistributionData);

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (this.IsDisposed || !this.IsHandleCreated) return;

                        CreateLabelTotalControls();

                        dgvProgressManagement.DataSource = null;  // ✅ Prevent binding issues
                        dgvProgressManagement.DataSource = distributionDataList;
                        ConfigureDataGridView();
                    });
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

                try
                {
                    _isDataLoaded = false; // ✅ Allow data reload
                    await GetDataAndLoadToGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    refreshButton.Enabled = true;
                    refreshButton.Text = "Refresh";
                }
            };



            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);

            // Ensure pagination panel is added at the bottom
            this.Controls.Add(paginationPanel);
        }
        private void ConfigureDataGridView()
        {
            dgvProgressManagement.AutoGenerateColumns = false;
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
        public class Distribution
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
            public string EmployeeName { get; set; }
            public int InventoryQty { get; set; }
            public string Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsLeather { get; set; }
            public int? Note { get; set; }
        }
    }
}