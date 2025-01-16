using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using DigitalProduction.Models;
using System.Threading.Tasks;
using DevExpress.XtraGrid.Views.Base;
using System.Drawing;
using DevExpress.XtraGrid.Columns;
using DigitalProduction.Extensions;

namespace DigitalProduction
{
    public partial class ucUserManagement : DevExpress.XtraEditors.XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        public ucUserManagement()
        {
            InitializeComponent();
            LoadTextLable();
            gridView_UserManagement.CustomDrawGroupPanel += gridView_CustomDrawGroupPanel;
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
                if (response?.Users != null)
                {
                    gridControl_UserManagement.DataSource = response?.Users;
                    ConfigureGridView();
                    gridView_UserManagement.EditFormPrepared += Extentions.GridView_EditFormPrepared;
                    Extentions.showEditModeCellGridView(gridControl_UserManagement, gridView_UserManagement, "Username");
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
            gridView_UserManagement.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss"; // Format for day/month/year
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView_UserManagement.Columns["UpdatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy hh:ss"; // Format for day/month/year

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
        }

    }
}
