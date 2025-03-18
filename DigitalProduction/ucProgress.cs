using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucProgress : UserControl
    {
        private BindingList<Distribution> distributionDataList = new BindingList<Distribution>();
        private WebSocketClient _webSocketClient;
        private Panel paginationPanel;
        private Label lblPageInfo;
        private GridControl gridProgressManagement; // Use GridControl
        private GridView gridViewProgressManagement; // Use GridView
        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
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

            // Initialize GridControl
            gridProgressManagement = new GridControl
            {
                Dock = DockStyle.Fill
            };

            gridViewProgressManagement = new GridView(gridProgressManagement)
            {
                OptionsBehavior = { Editable = false },
                OptionsView = { ShowGroupPanel = false }
            };

            gridProgressManagement.MainView = gridViewProgressManagement;

            // Set grid control columns
            ConfigureGridControl();

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
                try
                {
                    refreshButton.Enabled = false;
                    refreshButton.Text = "Loading...";

                    await Task.Run(async () => await GetDataAndLoadToGridAsync());

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    refreshButton.Enabled = true;
                    refreshButton.Text = "Refresh";
                }
            };

            paginationPanel.Controls.Add(refreshButton);
            paginationPanel.Controls.Add(lblPageInfo);

            // Add components to container panel
            containerPanel.Controls.Add(gridProgressManagement);
            containerPanel.Controls.Add(filterPanel);
            containerPanel.Controls.Add(paginationPanel);

            // Add container panel to the UserControl
            this.Controls.Add(containerPanel);

            // Subscribe to the RowStyle event
            gridViewProgressManagement.RowCellStyle += GridViewProgressManagement_RowCellStyle;
        }

        private void ConfigureGridControl()
        {
            // Clear existing columns
            gridViewProgressManagement.Columns.Clear();

            gridViewProgressManagement.Appearance.HeaderPanel.Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Bold);
            gridViewProgressManagement.Appearance.HeaderPanel.BackColor = System.Drawing.Color.AntiqueWhite;
            gridViewProgressManagement.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.Black;
            gridViewProgressManagement.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            // Set data source
            gridProgressManagement.DataSource = distributionDataList;

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
                FilterData(dtpStartDate.Value.Date, dtpEndDate.Value.Date, "");
            }
        }

        private void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            DateTime newStartDate = dtpStartDate.Value.Date;
            DateTime newEndDate = dtpEndDate.Value.Date;

            if (newStartDate > newEndDate)
            {
                AdjustDates(sender, ref newStartDate, ref newEndDate);
                ShowInvalidDateMessage();
            }
            else
            {
                FilterData(newStartDate, newEndDate, txtSearch.Text == LocalizationManager.GetString("Search") ? "" : txtSearch.Text.Trim());
            }
        }

        private void AdjustDates(object sender, ref DateTime newStartDate, ref DateTime newEndDate)
        {
            if (sender == dtpStartDate)
            {
                dtpStartDate.ValueChanged -= DateTimePicker_ValueChanged;
                newStartDate = newEndDate;
                dtpStartDate.Value = newStartDate;
                dtpStartDate.ValueChanged += DateTimePicker_ValueChanged;
            }
            else if (sender == dtpEndDate)
            {
                dtpEndDate.ValueChanged -= DateTimePicker_ValueChanged;
                newEndDate = newStartDate;
                dtpEndDate.Value = newEndDate;
                dtpEndDate.ValueChanged += DateTimePicker_ValueChanged;
            }
        }

        private void ShowInvalidDateMessage()
        {
            MessageBox.Show("Invalid date range! Start date cannot be after End date.", "Date Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchInput = txtSearch.Text.Trim();
            FilterData(dtpStartDate.Value.Date, dtpEndDate.Value.Date, string.IsNullOrEmpty(searchInput) || searchInput == "Search..." ? "" : searchInput);
        }

        private void FilterData(DateTime startDate, DateTime endDate, string searchText)
        {
            endDate = endDate.AddDays(1).AddTicks(-1);
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

            UpdateGridControl(filteredData);
        }


        private void UpdateGridControl(BindingList<Distribution> filteredData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateGridControl(filteredData)));
                return;
            }

            gridProgressManagement.DataSource = filteredData;
            gridProgressManagement.Refresh();  // Ensure UI updates
            TranslateHeaders();
            lblPageInfo.Text = $"{LocalizationManager.GetString("TotalRecords")} {filteredData.Count}";
        }

        private bool _isDataLoaded = false;

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessage;
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
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                WebSocket_OnMessage(response);
                _isDataLoaded = true;
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Request timed out.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(WebSocket_OnMessage), jsonData);
                return;
            }

            try
            {
                var response = ResponseMessage<List<Distribution>>.FromJson(jsonData);
                if (response?.DistributionData != null && response.DistributionData.Count > 0)
                {
                    distributionDataList.Clear();  // Ensure the list is cleared only when necessary

                    foreach (var distribution in response.DistributionData)
                    {
                        if (distribution != null)
                        {
                            distributionDataList.Add(distribution);
                        }
                    }

                    UpdateGridControl(new BindingList<Distribution>(distributionDataList));
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
        // Implement the RowStyle event handler
        private void GridViewProgressManagement_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            var view = sender as GridView;
            if (view != null)
            {
                // Get the current row data
                var rowData = view.GetRow(e.RowHandle) as Distribution;

                // Ensure rowData is valid and the column is "Status"
                if (rowData != null && e.Column.FieldName == "Status")
                {
                    // Customize only the "Status" column background color
                    if (rowData.Status == "Complete")
                    {
                        e.Appearance.BackColor = Color.LightGreen; // Green for complete
                    }
                    else if (rowData.Status == "Pending")
                    {
                        e.Appearance.BackColor = Color.LightYellow; // Yellow for pending
                    }
                    else
                    {
                        e.Appearance.BackColor = Color.LightCoral; // Red for other statuses
                    }

                }
            }
        }


        private void TranslateHeaders()
        {
            if (gridProgressManagement.MainView is GridView gridView && gridView.Columns.Count > 0)
            {
                foreach (GridColumn col in gridView.Columns)
                {
                    string translatedText = LocalizationManager.GetString(col.FieldName);
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        col.Caption = translatedText;
                    }
                }
                gridView.LayoutChanged(); // Force update to reflect changes
            }
            gridViewProgressManagement.Columns["Note"].Visible = false;
            gridViewProgressManagement.Columns["DistributionID"].Visible = false;
            gridViewProgressManagement.Columns["IsLeather"].Visible = false;
            gridViewProgressManagement.Columns["MaterialType"].Caption = LocalizationManager.GetString("MaterialType");
        }

        private void LoadTextLabel()
        {
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
            // New read-only property
            public string MaterialType => IsLeather ? LocalizationManager.GetString("leatherMaterial") : LocalizationManager.GetString("rawMaterial");

            public int? Note { get; set; }

            // New read-only property for NoteDescription
            public string NoteDescription
            {
                get
                {
                    switch (Note)
                    {
                        case 1: return LocalizationManager.GetString("NotEnoughMaterials");
                        case 2: return LocalizationManager.GetString("ChangeOfPlan");
                        case 3: return LocalizationManager.GetString("ForgotToChooseSize");
                        case 0: return string.Empty;
                        default: return string.Empty;
                    }
                }
            }

        }
    }
}