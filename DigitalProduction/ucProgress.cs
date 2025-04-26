using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
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
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox noteComboBoxEditor;
        private Rectangle _reasonHeaderCheckBoxRect;
        private bool _reasonHeaderChecked = false;
        private System.Windows.Forms.ComboBox cbxDevice;


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
                Text = LocalizationManager.GetString("StartDate"),
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
                Text = LocalizationManager.GetString("EndDate"),
                AutoSize = true,
                Margin = new Padding(5, 15, 5, 5)
            };

            dtpEndDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 100,
                Margin = new Padding(5, 10, 10, 5)
            };

            // Sync Data button
            SimpleButton syncButton = new SimpleButton()
            {
                Text = LocalizationManager.GetString("Sync"),
                Width = 90,
                Height = 35,
                Margin = new Padding(10, 10, 10, 5)
            };
            syncButton.ImageOptions.Image = Properties.Resources.sync_icon;
            syncButton.Click += async (sender, e) =>
            {
                try
                {
                    syncButton.Enabled = false;
                    syncButton.Text = "Loading...";

                    await Task.Run(async () => await GetDataAndLoadToGridAsync());

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    syncButton.Enabled = true;
                    syncButton.Text = LocalizationManager.GetString("Sync");
                }
            };

            dtpStartDate.ValueChanged += DateTimePicker_ValueChanged;
            dtpEndDate.ValueChanged += DateTimePicker_ValueChanged;

            cbxDevice = new System.Windows.Forms.ComboBox
            {
                Width = 150,
                Margin = new Padding(5, 10, 10, 5)
            };

            SimpleButton btnApplyDevice = new SimpleButton
            {
                Text = LocalizationManager.GetString("TransferDevice"),
                Width = 100,
                Height = 35,
                Margin = new Padding(5, 10, 10, 5)
            };
            btnApplyDevice.Click += BtnApplyDevice_Click;

            // Add controls to filter panel
            filterPanel.Controls.Add(lblStartDate);
            filterPanel.Controls.Add(dtpStartDate);
            filterPanel.Controls.Add(lblEndDate);
            filterPanel.Controls.Add(dtpEndDate);
            filterPanel.Controls.Add(syncButton);
            filterPanel.Controls.Add(cbxDevice);
            filterPanel.Controls.Add(btnApplyDevice);

            // Initialize GridControl
            gridProgressManagement = new GridControl
            {
                Dock = DockStyle.Fill
            };

            gridViewProgressManagement = new GridView(gridProgressManagement)
            {
                OptionsBehavior = { Editable = true },
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

        private void BtnApplyDevice_Click(object sender, EventArgs e)
        {
            if (cbxDevice.SelectedValue == null || (int)cbxDevice.SelectedValue == 0)
            {
                MessageBox.Show("Please select a valid device.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedDeviceId = (int)cbxDevice.SelectedValue;
            string selectedDeviceName = cbxDevice.Text;

            int appliedCount = 0;

            for (int rowHandle = 0; rowHandle < gridViewProgressManagement.RowCount; rowHandle++)
            {
                var distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;
                if (distribution != null && distribution.Status == "Pending")
                {
                    bool success = DbHelper.UpdateDistributionDevice(distribution.DistributionID, selectedDeviceId);

                    if (success)
                    {
                        distribution.DeviceID = selectedDeviceId;
                        gridViewProgressManagement.SetRowCellValue(rowHandle, "DeviceID", selectedDeviceId);
                        appliedCount++;
                    }
                }
            }

            MessageBox.Show(
                appliedCount > 0
                    ? $"Device '{selectedDeviceName}' applied to {appliedCount} row(s) with status 'Pending'."
                    : "No 'Pending' rows found in the grid.",
                "Result",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void loadDeviceDistribution()
        {
            List<Device> machines = DbHelper.getlistMachines();
            machines.Insert(0, new Device { DeviceID = 0, MachineName = "" });
            cbxDevice.DataSource = machines;
            cbxDevice.DisplayMember = "MachineName";
            cbxDevice.ValueMember = "DeviceID";
            cbxDevice.SelectedIndex = 0;
            cbxDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void ConfigureGridControl()
        {
            // Clear existing columns
            gridViewProgressManagement.Columns.Clear();

            gridViewProgressManagement.Appearance.HeaderPanel.Font = new Font(gridViewProgressManagement.Appearance.Row.Font, FontStyle.Bold);
            gridViewProgressManagement.Appearance.HeaderPanel.BackColor = System.Drawing.Color.AntiqueWhite;
            gridViewProgressManagement.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.Black;
            gridViewProgressManagement.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Set data source
            gridProgressManagement.DataSource = distributionDataList;
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
                FilterData(newStartDate, newEndDate);
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

        private void FilterData(DateTime startDate, DateTime endDate)
        {
            Task.Run(async () => await GetDataAndLoadToGridAsync());
            endDate = endDate.AddDays(1).AddTicks(-1);

            var filteredData = new BindingList<Distribution>(
                distributionDataList.Where(distribution =>
                    distribution.CreatedAt >= startDate &&
                    distribution.CreatedAt <= endDate
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
            var request = new { app = Global.App,
                action = "getDistributions",
                filter = new
                {
                    // Format dates as "yyyy-MM-dd" or adjust as required.
                    startDate = dtpStartDate.Value.ToString("yyyy-MM-dd"),
                    endDate = dtpEndDate.Value.ToString("yyyy-MM-dd"),
                }
            };
            string jsonRequest = JsonConvert.SerializeObject(request);

            try
            {
                string response = await _webSocketClient.SendAsync(jsonRequest);
                if (string.IsNullOrEmpty(response))
                {
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ConnectionManager.Instance.IsConnected = false;
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
                    loadDeviceDistribution();
                }
                else
                {
                    Console.WriteLine("No Data Found");
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
            gridViewProgressManagement.Columns["DeviceID"].Visible = false;
            gridViewProgressManagement.Columns["Note"].Visible = false;
            gridViewProgressManagement.Columns["IpAddress"].Visible = false;
            gridViewProgressManagement.Columns["DistributionID"].Visible = false;
            gridViewProgressManagement.Columns["IsLeather"].Visible = false;
            gridViewProgressManagement.Columns["MaterialType"].Caption = LocalizationManager.GetString("MaterialType");

            var existedNoted = gridViewProgressManagement.Columns.ColumnByFieldName(LocalizationManager.GetString("Reason"));
            if (existedNoted == null)
            {
                // Initialize 
                noteComboBoxEditor = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
                Dictionary<int, string> noteDescriptions = new Dictionary<int, string>
                {
                    { 0, "1 - " + LocalizationManager.GetString("NotEnoughMaterials") },
                    { 1, "2 - " + LocalizationManager.GetString("ChangeOfPlan") },
                    { 2, "3 - " + LocalizationManager.GetString("ForgotToChooseSize") }
                };
                noteComboBoxEditor.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
                foreach (var pair in noteDescriptions)
                {
                    noteComboBoxEditor.Items.Add(pair.Value);
                }
                // Initialize the GridColumn
                GridColumn noteColumn = new GridColumn
                {
                    FieldName = LocalizationManager.GetString("Reason"),
                    Visible = true
                };
                noteColumn.ColumnEdit = noteComboBoxEditor;
                noteColumn.OptionsColumn.AllowEdit = true;

                // Add the column to the grid view
                gridViewProgressManagement.Columns.Add(noteColumn);
                gridViewProgressManagement.ShowingEditor += GridViewProgressManagement_ShowingEditor;
                gridProgressManagement.Refresh();

                noteColumn.UnboundType = DevExpress.Data.UnboundColumnType.String;
                gridViewProgressManagement.CustomUnboundColumnData += (s, e) =>
                {
                    if (e.Column.FieldName == LocalizationManager.GetString("Reason"))
                    {
                        var model = (Distribution)e.Row;
                        if (e.IsGetData)
                        {
                            if (model.Note.HasValue && noteDescriptions.TryGetValue(model.Note.Value, out string description))
                            {
                                e.Value = description;
                            }
                        }
                        else if (e.IsSetData)
                        {
                            var desc = e.Value?.ToString();
                            var match = noteDescriptions.FirstOrDefault(x => x.Value == desc);
                            model.Note = match.Key;
                        }
                    }
                };
                gridViewProgressManagement.CustomDrawColumnHeader += GridViewProgressManagement_CustomDrawColumnHeader;
                gridViewProgressManagement.MouseDown += GridViewProgressManagement_MouseDown;

                gridViewProgressManagement.ShownEditor += (s, e) =>
                {
                    if (gridViewProgressManagement.FocusedColumn.FieldName == LocalizationManager.GetString("Reason"))
                    {
                        int rowHandle = gridViewProgressManagement.FocusedRowHandle;
                        var distribution = gridViewProgressManagement.GetRow(rowHandle) as Distribution;

                        // Check if the distribution object exists and its status is not "Complete"
                        if (distribution != null && distribution.Status != "Complete")
                        {
                            var editor = gridViewProgressManagement.ActiveEditor as ComboBoxEdit;
                            if (editor != null)
                            {
                                // Handle the SelectedIndexChanged event
                                editor.SelectedIndexChanged += (s2, e2) =>
                                {
                                    int selectedIndex = editor.SelectedIndex;
                                    if (noteDescriptions.TryGetValue(selectedIndex, out string selectedDesc))
                                    {
                                        int distributionId = distribution.DistributionID;

                                        // update status distribution
                                        DbHelper.UpdateDistributionNoteAndStatus(distributionId, selectedIndex, "Stop");
                                        Console.WriteLine($"Selected Note Key: {selectedIndex}, Description: {selectedDesc}, DistributionID: {distributionId}");
                                    }
                                };
                            }
                        }
                        else
                        {
                            // not allow eduit when status complete
                            gridViewProgressManagement.HideEditor();
                            Console.WriteLine("Editing is disabled for rows with status 'Complete'.");
                        }
                    }
                };
            }
        }
        private void GridViewProgressManagement_CustomDrawColumnHeader(object sender, ColumnHeaderCustomDrawEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == LocalizationManager.GetString("Reason"))
            {
                e.Info.InnerElements.Clear();
                e.Painter.DrawObject(e.Info);
                e.Handled = true;

                // Draw checkbox
                _reasonHeaderCheckBoxRect = new Rectangle(e.Bounds.X + e.Bounds.Width - 20, e.Bounds.Y + 5, 15, 15);
                ButtonState state = _reasonHeaderChecked ? ButtonState.Checked : ButtonState.Normal;
                ControlPaint.DrawCheckBox(e.Graphics, _reasonHeaderCheckBoxRect, state);
            }
        }

        // mode stop or pending
        private void GridViewProgressManagement_MouseDown(object sender, MouseEventArgs e)
        {

            if (_reasonHeaderCheckBoxRect.Contains(e.Location))
            {
                _reasonHeaderChecked = !_reasonHeaderChecked;
                gridViewProgressManagement.InvalidateColumnHeader(gridViewProgressManagement.Columns[LocalizationManager.GetString("Reason")]);

                for (int i = 0; i < gridViewProgressManagement.RowCount; i++)
                {
                    var row = gridViewProgressManagement.GetRow(i) as Distribution;
                    if (row != null && row.Status != "Complete")
                    {
                        row.Note = _reasonHeaderChecked ? 1 : (int?)null;
                        row.Status = _reasonHeaderChecked ? "Stop" : "Pending";
                        DbHelper.UpdateDistributionNoteAndStatus(row.DistributionID, row.Note ?? -1, row.Status);
                    }
                }
                gridViewProgressManagement.RefreshData();
            }
        }


        private void GridViewProgressManagement_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GridView view = sender as GridView;

            // Check if the current column is the noteColumn
            if (view.FocusedColumn.FieldName == LocalizationManager.GetString("Reason"))
            {
                e.Cancel = false; // Allow editing if the focused column is "Note"
            }
            else
            {
                e.Cancel = true; // Prevent editing for all other columns
            }
        }
        private void LoadTextLabel()
        {
            this.Text = LocalizationManager.GetString("ListOfDistributions");
        }
        public class Distribution
        {
            public int DistributionID { get; set; }
            public string SO { get; set; }
            public int DeviceID { get; set; }
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
            public DateTime UpdatedAt { get; set; }
            public bool IsLeather { get; set; }
            // New read-only property
            public string MaterialType => IsLeather ? LocalizationManager.GetString("leatherMaterial") : LocalizationManager.GetString("rawMaterial");

            public int? Note { get; set; }

            //// New read-only property for NoteDescription
            //public string NoteDescription
            //{
            //    get
            //    {
            //        switch (Note)
            //        {
            //            case 1: return LocalizationManager.GetString("NotEnoughMaterials");
            //            case 2: return LocalizationManager.GetString("ChangeOfPlan");
            //            case 3: return LocalizationManager.GetString("ForgotToChooseSize");
            //            case 0: return "Com";
            //            default: return string.Empty;
            //        }
            //    }
            //}
        }
    }
}