using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraBars.Navigation;

namespace DigitalProduction
{
    public partial class frmMain : FluentDesignForm
    {
        private AccordionControlElement previousSelectedElement;
        private BarButtonItem btnLogOut;

        private WebSocketClient _webSocketClient;
        private ResourceManager resourceManager;
        public Dictionary<Type, UserControl> _userControls = new Dictionary<Type, UserControl>();

        private Dictionary<string, string> statusMapping;

        public frmMain()
        {
            resourceManager = new ResourceManager("DigitalProduction.en", typeof(frmMain).Assembly);

            InitializeComponent();
            _webSocketClient = WebSocketClient.Instance;
            InitializeLogOutButton();
            InitializeStatusMapping();

            accordionControl1.ElementClick += AccordionControl1_ElementClick;
            UpdateFormTexts();
            ConnectionManager.Instance.ConnectionStatusChanged += OnConnectionStatusChanged;
            ConnectionManager.Instance.ReconnectionStatusChanged += OnReconnectionStatusChanged;
            this.FormClosing += frmMain_FormClosing;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }


        private void OnReconnectionStatusChanged(bool isReconnecting)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateReconnectStatus(isReconnecting)));
            }
            else
            {
                UpdateReconnectStatus(isReconnecting);
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateConnectionStatus(isConnected)));
            }
            else
            {
                UpdateConnectionStatus(isConnected);
            }
        }

        private void UpdateReconnectStatus(bool isReconnecting)
        {
            if (isReconnecting)
            {
                statusItem.Caption = LocalizationManager.GetString("Reconnecting");
                statusItem.Appearance.ForeColor = Color.Orange;
            }
            else
            {
                if (ConnectionManager.Instance.IsConnected)
                {
                    statusItem.Caption = LocalizationManager.GetString("Connected");
                    statusItem.Appearance.ForeColor = Color.Green;
                }
                else
                {
                    statusItem.Caption = LocalizationManager.GetString("Disconnected");
                    statusItem.Appearance.ForeColor = Color.Red;
                }
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                statusItem.Caption = LocalizationManager.GetString("Connected");
                statusItem.Appearance.ForeColor = Color.Green;
            }
            else
            {
                statusItem.Caption = LocalizationManager.GetString("Disconnected");
                statusItem.Appearance.ForeColor = Color.Red;
            }
        }

        private void InitializeLogOutButton()
        {
            btnLogOut = new BarButtonItem();
            btnLogOut.Caption = LocalizationManager.GetString("LogOut");
            btnLogOut.ItemClick += BtnLogOut_ItemClick;

            barSubItem1.AddItem(btnLogOut);
        }

        private void BtnLogOut_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (barSubItem1.Caption == "Guest")
            {
                Application.Exit();
            }

            DialogResult result = MessageBox.Show(LocalizationManager.GetString("ConfirmLogOut"), LocalizationManager.GetString("LogOut"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Global.ResetUser();
                this.Hide();
                frmLogin loginForm = new frmLogin();
                loginForm.Show();
                loginForm.FormClosed += (s, args) => this.Close();
            }
        }

        public void HighlightSelectedItem(AccordionControlElement selectedElement)
        {
            if (selectedElement.Style != ElementStyle.Item)
                return;

            if (previousSelectedElement != null)
            {
                ResetElementAppearance(previousSelectedElement);
            }

            SetElementAppearance(selectedElement);
            previousSelectedElement = selectedElement;
        }

        private void SetElementAppearance(AccordionControlElement element)
        {
            element.Appearance.Normal.BackColor = Color.LightSteelBlue;
            element.Appearance.Normal.ForeColor = Color.Black;
            element.Appearance.Normal.Font = new Font(element.Appearance.Normal.Font, FontStyle.Bold);
        }

        private void ResetElementAppearance(AccordionControlElement element)
        {
            element.Appearance.Normal.BackColor = Color.Transparent;
            element.Appearance.Normal.ForeColor = Color.Empty;
            element.Appearance.Normal.Font = new Font(element.Appearance.Normal.Font, FontStyle.Regular);
        }

        private void AccordionControl1_ElementClick(object sender, ElementClickEventArgs e)
        {
            HighlightSelectedItem(e.Element);
        }

        public async Task ShowUserControlAsync<T>() where T : UserControl, new()
        {
            if (pnlControl.InvokeRequired)
            {
                var tcs = new TaskCompletionSource<bool>();
                pnlControl.Invoke(new Action(() => {
                    ShowUserControlAsync<T>().ContinueWith(t => {
                        if (t.IsFaulted) tcs.SetException(t.Exception);
                        else tcs.SetResult(true);
                    });
                }));
                await tcs.Task;
                return;
            }

            // Bật double buffering nếu chưa bật (gọi 1 lần duy nhất)
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, pnlControl, new object[] { true });

            if (!_userControls.TryGetValue(typeof(T), out UserControl userControl))
            {
                userControl = new T { Dock = DockStyle.Fill };

                _userControls[typeof(T)] = userControl;

                pnlControl.Controls.Add(userControl);

                InvokeSetWebSocketClient(userControl);
            }
            else
            {
                _webSocketClient?.ClearEventHandlers();
                InvokeSetWebSocketClient(userControl);
            }

            pnlControl.SuspendLayout();

            foreach (Control ctrl in pnlControl.Controls)
            {
                ctrl.Visible = ctrl == userControl;
            }

            userControl.BringToFront();

            pnlControl.ResumeLayout();
        }


        private void InvokeSetWebSocketClient(UserControl userControl)
        {
            var methodInfo = userControl.GetType().GetMethod("SetWebSocketClient", BindingFlags.Public | BindingFlags.Instance);

            if (methodInfo != null)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    methodInfo.Invoke(userControl, null); // No parameters
                }
                else
                {
                    if (_webSocketClient == null)
                    {
                        Console.WriteLine("WebSocketClient is null! Cannot pass to method.");
                        return;
                    }

                    _webSocketClient.ClearEventHandlers(); // Ensure clean event handling
                    methodInfo.Invoke(userControl, new object[] { _webSocketClient }); // Pass WebSocketClient
                }
            }
            else
            {
                Console.WriteLine($"SetWebSocketClient method not found on {userControl.GetType().Name}");
            }
        }




        private async void btnDeviceManager_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucDeviceManager>();
        }

        private async void btnUserManager_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucUserManagement>();
        }

        private async void btnSchedule_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucSchedule>();
        }

        private async void btnProgress_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucProgress>();
        }

        private async void btnDeviceManage_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucProgressManagement>();
        }

        private async void btnDistribution_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucDistribution>();
        }

        private async void btnDeviceOutput_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucDeviceOutput>();
        }

        private async void btnOperator_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucOperatorManagement>();
        }

        private async void btnProgressDistribution_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucProgress>();
        }

        private async void btnReportOder_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucReportOrder>();
        }

        private async void btnCuttingReport_Click(object sender, EventArgs e)
        {
            await ShowUserControlAsync<ucCuttingReport>();
        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            if (Global.CurrentUser == null)
            {
                barSubItem1.Caption = LocalizationManager.GetString("Guest");
            }
            else
            {
                barSubItem1.Caption = Global.CurrentUser.EmployeeName;
            }

            UpdateConnectionStatus(ConnectionManager.Instance.IsConnected);
        }


        private void InitializeStatusMapping()
        {
            statusMapping = new Dictionary<string, string>
            {
                { "DeviceManagerStatus", LocalizationManager.GetString("DeviceManagerStatus") },
                { "UserManagerStatus", LocalizationManager.GetString("UserManagerStatus") },
                { "ProductionScheduleStatus", LocalizationManager.GetString("ProductionSchedule") },
                { "CuttingMachineManagerStatus", LocalizationManager.GetString("CuttingMachineManager") },
                { "PODistributionStatus", LocalizationManager.GetString("PODistribution") }
            };
        }

        private void UpdateFormTexts()
        {
            btnLogOut.Caption = LocalizationManager.GetString("LogOut");
            this.Text = LocalizationManager.GetString("Home");
            accordionControlElement1.Text = LocalizationManager.GetString("ProductionSchedule");
            accordionControlElement4.Text = LocalizationManager.GetString("CuttingManager");
            accordionControlElement3.Text = LocalizationManager.GetString("SystemManagerment");
            barSubItem1.Caption = LocalizationManager.GetString("Guest");
            accordionControlElement6.Text = LocalizationManager.GetString("Report");

            btnDeviceManager.Text = LocalizationManager.GetString("DeviceManager");
            btnMonthlyPlan.Text = LocalizationManager.GetString("MonthlyPlan");
            btnDistribution.Text = LocalizationManager.GetString("Distribution");
            btnUserManager.Text = LocalizationManager.GetString("UserManager");
            btnDeviceOutput.Text = LocalizationManager.GetString("DeviceOutput");
            btnOperator.Text = LocalizationManager.GetString("OperatorManager");
            btnProgressDistribution.Text = LocalizationManager.GetString("Progress");
            btnReportOder.Text = LocalizationManager.GetString("OperatorPerformance");
            btnCuttingReportQty.Text = LocalizationManager.GetString("CuttingReport");
        }


        public static class LanguageSettings
        {
            private static string _currentLanguage = "vi";

            public static string CurrentLanguage
            {
                get { return _currentLanguage; }
                set
                {
                    if (_currentLanguage != value)
                    {
                        _currentLanguage = value;
                        LanguageChanged?.Invoke();
                    }
                }
            }

            public static event Action LanguageChanged;

            public static void ChangeLanguage(string language)
            {
                CurrentLanguage = language;
            }
        }
    }
}