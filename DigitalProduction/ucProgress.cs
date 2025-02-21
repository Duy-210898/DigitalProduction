using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Extensions;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucProgress : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<Distribution> distributionDataList = new BindingList<Distribution>();
        private WebSocketClient _webSocketClient;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;

        public ucProgress()
        {
            InitializeComponent();
            LoadTextLable();
            gridView_ProgressManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            gridView_ProgressManagement.OptionsFind.ShowFindButton = false;
            gridView_ProgressManagement.RowHeight = 50;
            gridView_ProgressManagement.RowCellStyle += gridView_ProgressManagement_RowCellStyle;
        }

        private void gridView_ProgressManagement_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (view == null) return;

            // Get IsLeather value for the current row
            bool isLeather = Convert.ToBoolean(view.GetRowCellValue(e.RowHandle, "IsLeather"));

            // Apply custom style if IsLeather is true
            if (isLeather)
            {
                e.Appearance.BackColor = Color.LightYellow; // Highlight cell
                e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold); // Make it bold
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
            // WebSocket request to get distribution data from the server
            var request = new { app = Global.App, action = "getDistributions" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                // Deserialize the response message into a list of distribution data
                ResponseMessage<List<Distribution>> response = ResponseMessage<List<Distribution>>.FromJson(jsonData);

                if (response?.DistributionData != null && response.DistributionData.Count > 0)
                {
                    // Clear current distribution data list
                    distributionDataList.Clear();

                    // Add the distribution data to the list
                    foreach (var distribution in response.DistributionData)
                    {
                        distributionDataList.Add(distribution);
                    }

                    // Refresh pagination and grid view after loading new data
                    CreatelabelTotalControls();
                    gridControl_ProgressManagement.DataSource = distributionDataList;
                    ConfigureGridView();
                    gridView_ProgressManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                    Extentions.showEditModeCellGridView(gridControl_ProgressManagement, gridView_ProgressManagement, "ucProgress");
                }
                else
                {
                    ShowMessage.ShowInfo("No Data Found");
                }
            }
            catch (Exception ex)
            {
                ShowMessage.ShowError($"Error receiving WebSocket data: {ex.Message}");
            }
        }

        private void CreatelabelTotalControls()
        {
            // Remove any existing panel to prevent duplication
            if (paginationPanel != null)
            {
                this.Controls.Remove(paginationPanel);
                paginationPanel.Dispose();
            }

            // Create a new PanelControl for pagination at the bottom
            paginationPanel = new PanelControl()
            {
                Dock = DockStyle.Bottom,
                Height = 50, // Adjust height
                Padding = new Padding(10)
            };

            // Create Label for page info (Total Records)
            lblPageInfo = new LabelControl()
            {
                Text = $"Total Records: {distributionDataList.Count}",
                Size = new Size(200, 30),
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(20, 10)
            };
            //lblPageInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            // Create a "Refresh" Button
            SimpleButton refreshButton = new SimpleButton()
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(250, 10) // Adjust positioning
            };
            // Style the Refresh Button
            refreshButton.Appearance.BackColor = Color.LightBlue; // Change background color
            refreshButton.Appearance.Font = new Font("Arial", 9f, FontStyle.Bold);
            refreshButton.Appearance.Options.UseBackColor = true;

            // Add click event to refresh the data
            refreshButton.Click += async (sender, e) =>
            {
                // Disable the button while loading
                refreshButton.Enabled = false;
                refreshButton.Text = "Loading..."; // Provide user feedback

                await GetDataAndLoadToGridAsync(); // Load the data

                // Re-enable the button after data is loaded
                refreshButton.Enabled = true;
                refreshButton.Text = "Refresh"; // Reset button text
            };

            // Add components to the panel
            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(refreshButton);

            // Add the panel to the form (make sure it's added correctly)
            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0); // Ensures it appears at the bottom
        }

        // Event to handle refresh button click
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            _ = GetDataAndLoadToGridAsync();
        }

        private void ConfigureGridView()
        {
            // Hide sensitive columns
            gridView_ProgressManagement.Columns["DistributionID"].Visible = false;
            // Apply sorting, headers, and format
            gridView_ProgressManagement.Columns["InventoryQty"].Caption = "Inventory Quantity";
            gridView_ProgressManagement.Columns["Status"].Caption = "Status";
            gridView_ProgressManagement.SortInfo.Clear();
            gridView_ProgressManagement.SortInfo.Add(new GridColumnSortInfo(gridView_ProgressManagement.Columns["Status"], DevExpress.Data.ColumnSortOrder.Ascending));

            // Format DateTime columns, if necessary
            gridView_ProgressManagement.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_ProgressManagement.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:mm";
            gridView_ProgressManagement.BestFitColumns();
            groupDataRawOrLeather("MaterialName");
        }

        private void groupDataRawOrLeather(string name)
        {
            gridView_ProgressManagement.Columns[name].GroupIndex = 0; // Group by PartName
            gridView_ProgressManagement.ExpandAllGroups();
            gridView_ProgressManagement.CustomDrawGroupRow += (sender, e) =>
            {
                GridView view = sender as GridView;
                GridGroupRowInfo groupInfo = e.Info as GridGroupRowInfo;

                if (groupInfo.Column.FieldName == name) // Ensure correct grouping
                {
                    // Get name from the group
                    object nameCol = view.GetGroupRowValue(e.RowHandle, groupInfo.Column);

                    // Get IP Address from the first row in the group
                    object ipAddress = view.GetRowCellValue(view.GetDataRowHandleByGroupRowHandle(e.RowHandle), "IpAddress");

                    // Modify group row text
                    groupInfo.GroupText = $"📦 {name}: {nameCol} - 🌐 IP: {ipAddress}";

                    // Optional: Change group row text color
                    e.Appearance.ForeColor = Color.Blue;
                }
            };
        }

        private void gridView_CustomDrawGroupPanel(object sender, CustomDrawEventArgs e)
        {
            e.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            e.Appearance.Font = new Font("Tahoma", 13, FontStyle.Bold);
            e.Appearance.ForeColor = Color.Blue;
        }

        private void LoadTextLable()
        {
            gridView_ProgressManagement.GroupPanelText = LocalizationManager.GetString("ListOfDistributions");
            gridView_ProgressManagement.OptionsFind.FindNullPrompt = LocalizationManager.GetString("Find");
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
        }
    }
}
