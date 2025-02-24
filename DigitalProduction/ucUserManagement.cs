using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction
{
    public partial class ucUserManagement : XtraUserControl
    {
        public BindingList<Employee> employees = new BindingList<Employee>();
        private WebSocketClient _webSocketClient;
        private PanelControl groupPanelButtonContainer;
        private SimpleButton button;
        private ucRegisterUser frmRegister;
        private PanelControl paginationPanel;
        private LabelControl lblPageInfo;

        private DataGridView dataGridView_UserManagement;

        public ucUserManagement()
        {
            InitializeComponent();
            LoadTextLable();
            InitializeDataGridView();
            CreateButtonContainer();

            // show add new user
            frmRegister = new ucRegisterUser();
            frmRegister.Visible = false;
            this.Controls.Add(frmRegister);
            frmRegister.ExitClicked += RegisterControl_ExitClicked;
            frmRegister.UserCreated += RegisterForm_UserCreated;
        }
        private void InitializeDataGridView()
        {
            dataGridView_UserManagement = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 50 }
            };

            this.Controls.Add(dataGridView_UserManagement);
        }
        private void ApplyLocalization()
        {
            // Hide sensitive data like Password and deparmentID
            dataGridView_UserManagement.Columns["Password"].Visible = false;
            dataGridView_UserManagement.Columns["DepartmentID"].Visible = false;
            dataGridView_UserManagement.Columns["OperatorID"].Visible = false;
            dataGridView_UserManagement.Columns["OperatorName"].Visible = false;
            dataGridView_UserManagement.Columns["PositionID"].Visible = false;
            dataGridView_UserManagement.Columns["CreatedAt"].Visible = false;
            dataGridView_UserManagement.Columns["UpdatedAt"].Visible = false;

            if (dataGridView_UserManagement.Columns.Contains("Username"))
                dataGridView_UserManagement.Columns["Username"].HeaderText = LocalizationManager.GetString("Username");
            dataGridView_UserManagement.Columns["EmployeeID"].HeaderText = LocalizationManager.GetString("EmployeeID");
            dataGridView_UserManagement.Columns["EmployeeName"].HeaderText = LocalizationManager.GetString("EmployeeName");
            dataGridView_UserManagement.Columns["IsActive"].HeaderText = LocalizationManager.GetString("IsActive");
            dataGridView_UserManagement.Columns["DepartmentName"].HeaderText = LocalizationManager.GetString("DepartmentName");
            dataGridView_UserManagement.Columns["PositionName"].HeaderText = LocalizationManager.GetString("PositionName");

            // ✅ Add Action Column if it does not exist
            if (!dataGridView_UserManagement.Columns.Contains("Action"))
            {
                DataGridViewButtonColumn actionColumn = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = "Action",
                    UseColumnTextForButtonValue = false  // ✅ Set to false to allow dynamic text change
                };
                dataGridView_UserManagement.Columns.Add(actionColumn);
            }

            // ✅ Set every row to be read-only at the start
            foreach (DataGridViewRow row in dataGridView_UserManagement.Rows)
            {
                //row.Cells["Address"].ReadOnly = true;
                //row.Cells["Machine Name"].ReadOnly = true;
                //row.Cells["Plant Name"].ReadOnly = true;
                //row.Cells["Department Name"].ReadOnly = true;
                //row.Cells["IsActive"].ReadOnly = true;
                //row.Cells["ConnectionStatus"].ReadOnly = true;

                row.Cells["Action"].Value = "Edit";
            }
            dataGridView_UserManagement.Columns["Action"].HeaderText = LocalizationManager.GetString("Action");

            // dataGridView_UserManagement.CellClick += dgvDevices_CellClick; // Attach event
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
                Height = 50,
                Padding = new Padding(10)
            };

            lblPageInfo = new LabelControl()
            {
                Text = $"Total Records: {employees.Count}",
                Size = new Size(200, 30),
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(20, 10)
            };

            SimpleButton refreshButton = new SimpleButton()
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(250, 10)
            };

            // Add click event to refresh the data
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

            this.Controls.Add(paginationPanel);
            this.Controls.SetChildIndex(paginationPanel, 0);
        }

        private void RegisterForm_UserCreated(object sender, Employee newUser)
        {
            employees.Insert(0, newUser); // Refresh DataGridView
            LoadDataGridView();
        }

        private void LoadDataGridView()
        {
            dataGridView_UserManagement.DataSource = null; // Reset data source
            dataGridView_UserManagement.DataSource = employees.ToList(); // Bind the data source
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
            _ = GetDataAndLoadToGridAsync();
            _webSocketClient.OnResponseReceived += WebSocket_OnMessage;
        }

        public async Task GetDataAndLoadToGridAsync()
        {
            var request = new { app = Global.App, action = "getUsers" };
            string jsonRequest = JsonConvert.SerializeObject(request);

            await _webSocketClient.SendAsync(jsonRequest);
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            try
            {
                ResponseMessage<List<Employee>> response = ResponseMessage<List<Employee>>.FromJson(jsonData);

                if (response?.Users != null && response.Users.Count > 0)
                {
                    employees.Clear();

                    foreach (var employee in response.Users)
                    {
                        employees.Add(employee);
                    }

                    CreatelabelTotalControls(); // Create/update pagination info
                    LoadDataGridView(); // Load data into the DataGridView
                    ApplyLocalization();
                }
                else
                {
                    MessageBox.Show("No Data Found");
                }
            }
            catch (JsonSerializationException jsonEx)
            {
                MessageBox.Show($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Create button container and other existing methods remain unchanged...

        private void CreateButtonContainer()
        {
            // Create a PanelControl to hold the button
            groupPanelButtonContainer = new PanelControl()
            {
                Dock = DockStyle.Top,
                Height = 50 // Adjust the height to suit your layout
            };

            Controls.Add(groupPanelButtonContainer);
            button = new SimpleButton()
            {
                Text = "Create user",
                Size = new Size(100, 40)
            };
            groupPanelButtonContainer.Controls.Add(button);
            button.Click += Button_Click;
            button.Location = new Point(10, 5);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            showRegisterUser();
        }

        private void showRegisterUser()
        {
            // Hide the DataGridView
            dataGridView_UserManagement.Visible = false;
            frmRegister.Location = dataGridView_UserManagement.Location;
            frmRegister.Size = dataGridView_UserManagement.Size;
            frmRegister.Visible = true;
            frmRegister.BringToFront();
        }

        private void RegisterControl_ExitClicked(object sender, EventArgs e)
        {
            // Show the DataGridView
            dataGridView_UserManagement.Visible = true;
            frmRegister.Visible = false;
        }

        private void LoadTextLable()
        {
            // Assuming this is where you load the grid's header labels, adjust accordingly 
        }
    }
}