using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraExport.Helpers;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucUserManagement : DevExpress.XtraEditors.XtraUserControl
    {
        private static BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterUser popup;
        public ucUserManagement()
        {
            InitializeComponent();
            LoadTextLable();
            gridView_UserManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
            CreateButtonContainer();
            popup = new ucRegisterUser();
            popup.Visible = false;
            this.Controls.Add(popup);
            popup.ExitClicked += RegisterControl_ExitClicked;

        }
        private void MainForm_Click(object sender, EventArgs e)
        {
            if (!popup.Bounds.Contains(PointToClient(MousePosition)))
            {
                popup.Visible = false;
            }
        }
        private void showRegisterUser()
        {
            // Hide the GridView
            gridControl_UserManagement.Visible = false;

            // Show the user control
            popup.Location = gridControl_UserManagement.Location;
            popup.Size = gridControl_UserManagement.Size;
            popup.Visible = true;
            popup.BringToFront();
        }
        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the GridView
            gridControl_UserManagement.Visible = true;

            // Hide the user control
            popup.Visible = false;
        }
        private void registerButton_Click(object sender, EventArgs e)
        {
            // Logic for user registration goes here...

            // Show popup
            MessageBox.Show("User registered successfully!", "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void gridView_CustomDrawGroupPanel(object sender, CustomDrawEventArgs e)
        {
            // Set the alignment of the GroupPanelText
            e.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            e.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            // Optional: You can also change the font and color if necessary
            e.Appearance.Font = new Font("Tahoma", 13, FontStyle.Bold);
            e.Appearance.ForeColor = Color.Blue;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = WebSocketClient.Instance;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        // Send request to WebSocket or API and load data
        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { action = "getUsers" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }
        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                // Assuming the response
                ResponseMessage<List<Employee>> response = ResponseMessage<List<Employee>>.FromJson(jsonData);

                // Check if there are devices in the response
                if (response?.Users != null && response.Users.Count > 0)
                {
                    employees.Clear();
                    foreach (var employee in response.Users)
                    {
                        employees.Add(employee);
                    }
                    gridView_UserManagement.SortInfo.Clear();
                    gridControl_UserManagement.DataSource = employees;
                    ConfigureGridView();
                    gridView_UserManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                    Extentions.showEditModeCellGridView(gridControl_UserManagement, gridView_UserManagement, "Username");
                    ApplyLocalization();
                }
                else
                {
                    ShowMessage.ShowInfo("No Data Found");
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                ShowMessage.ShowError($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                ShowMessage.ShowError($"An error occurred: {ex.Message}");
            }
        }
        private void ConfigureGridView()
        {
            gridView_UserManagement.BestFitColumns();
            // Hide sensitive data like Password
            gridView_UserManagement.Columns["Password"].Visible = false;

            // Format the DateTime columns
            gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss";

            // Apply sorting by Username in ascending order when the grid loads
            gridView_UserManagement.SortInfo.Clear();
            gridView_UserManagement.SortInfo.Add(new GridColumnSortInfo(gridView_UserManagement.Columns["EmployeeName"], DevExpress.Data.ColumnSortOrder.Ascending));

            // Set custom column captions
            gridView_UserManagement.Columns["EmployeeName"].Caption = "Full Name";
            gridView_UserManagement.Columns["Username"].Caption = "Username";
            gridView_UserManagement.Columns["Department"].Caption = "Department";

            // Optionally, format the IsActive column to display checkboxes
            gridView_UserManagement.Columns["IsActive"].ColumnEdit = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            // Apply custom styles to column headers
            gridView_UserManagement.Appearance.HeaderPanel.BorderColor = Color.LightSteelBlue;
            gridView_UserManagement.Appearance.HeaderPanel.ForeColor = Color.Black;
            gridView_UserManagement.Appearance.HeaderPanel.Font = new Font("Arial", 10, FontStyle.Bold);
        }

        private void LoadTextLable()
        { 
            gridView_UserManagement.GroupPanelText = LocalizationManager.GetString("ListOfUser");
        }
        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }
        private void OnLanguageChanged()
        {
            LoadTextLable();
            ApplyLocalization();
        }

        private void CreateButtonContainer()
        {
            // Create a PanelControl to hold the button
            groupPanelButtonContainer = new PanelControl()
            {
                Dock = DockStyle.Top,
                Height = 50 // Adjust the height to suit your layout
            };

            // Add the PanelControl to the form
            Controls.Add(groupPanelButtonContainer);

            // Create the button
            button = new SimpleButton()
            {
                Text = "Create user",
                Size = new System.Drawing.Size(100, 40)
            };

            // Add the button to the PanelControl
            groupPanelButtonContainer.Controls.Add(button);

            // Handle the button click event
            button.Click += Button_Click;

            // Position the button inside the PanelControl (optional)
            button.Location = new System.Drawing.Point(10, 5); // Adjust location as needed
        }
        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterUser();
        }
        private void ApplyLocalization()
        {
            // grid view
            gridControl_UserManagement.BeginUpdate();
           
            if (!gridView_UserManagement.Columns.Count.Equals(0))
            {
                gridView_UserManagement.Columns["Username"].Caption = LocalizationManager.GetString("Username");
                gridView_UserManagement.Columns["EmployeeID"].Caption = LocalizationManager.GetString("EmployeeID");
                gridView_UserManagement.Columns["EmployeeName"].Caption = LocalizationManager.GetString("EmployeeName");
                gridView_UserManagement.Columns["CreatedAt"].Caption = LocalizationManager.GetString("CreatedAt");
                gridView_UserManagement.Columns["UpdatedAt"].Caption = LocalizationManager.GetString("UpdatedAt");
                gridView_UserManagement.Columns["IsActive"].Caption = LocalizationManager.GetString("IsActive");
                gridView_UserManagement.Columns["Department"].Caption = LocalizationManager.GetString("Department");
                gridView_UserManagement.Columns["Action"].Caption = LocalizationManager.GetString("Action");

                gridControl_UserManagement.DataSource = null;
                gridControl_UserManagement.DataSource = employees;
                gridView_UserManagement.SortInfo.Clear();
            }
            gridControl_UserManagement.EndUpdate();
        }
    }
}
