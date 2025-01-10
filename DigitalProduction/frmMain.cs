using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using static DevExpress.Utils.Frames.FrameHelper;

namespace DigitalProduction
{
    public partial class frmMain : FluentDesignForm
    {
        private AccordionControlElement previousSelectedElement;
        private BarButtonItem btnLogOut;

        private WebSocketClient _webSocketClient;
        private ResourceManager resourceManager;

        private Dictionary<string, string> statusMapping;

        public frmMain(WebSocketClient webSocketClient)
        {
            resourceManager = new ResourceManager("DigitalProduction.en", typeof(frmMain).Assembly);

            InitializeComponent();
            _webSocketClient = webSocketClient;
            InitializeLogOutButton();
            InitializeStatusMapping();

            accordionControl1.ElementClick += AccordionControl1_ElementClick;
            UpdateFormTexts();
            ConnectionManager.Instance.ConnectionStatusChanged += OnConnectionStatusChanged;
            ConnectionManager.Instance.ReconnectionStatusChanged += OnReconnectionStatusChanged;
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
                statusItem.Caption = resourceManager.GetString("Reconnecting");
                statusItem.Appearance.ForeColor = Color.Orange;
            }
            else
            {
                if (ConnectionManager.Instance.IsConnected)
                {
                    statusItem.Caption = resourceManager.GetString("Connected");
                    statusItem.Appearance.ForeColor = Color.Green;
                }
                else
                {
                    statusItem.Caption = resourceManager.GetString("Disconnected");
                    statusItem.Appearance.ForeColor = Color.Red;
                }
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                statusItem.Caption = resourceManager.GetString("Connected");
                statusItem.Appearance.ForeColor = Color.Green;
            }
            else
            {
                statusItem.Caption = resourceManager.GetString("Disconnected");
                statusItem.Appearance.ForeColor = Color.Red;
            }
        }

        private void InitializeLogOutButton()
        {
            btnLogOut = new BarButtonItem();
            btnLogOut.Caption = resourceManager.GetString("LogOut");
            btnLogOut.ItemClick += BtnLogOut_ItemClick;

            barSubItem1.AddItem(btnLogOut);
        }

        private void BtnLogOut_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (barSubItem1.Caption == "Guest")
            {
                Application.Exit();
            }

            DialogResult result = MessageBox.Show(resourceManager.GetString("ConfirmLogOut"), resourceManager.GetString("LogOut"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                this.Hide();
                frmLogin loginForm = new frmLogin();
                loginForm.Show();
                loginForm.FormClosed += (s, args) => this.Close();
            }
        }

        private void HighlightSelectedItem(AccordionControlElement selectedElement)
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

        private void ShowUserControl<T>() where T : UserControl, new()
        {
            if (!(pnlControl.Controls.Count > 0 && pnlControl.Controls[0] is T))
            {
                pnlControl.Controls.Clear();

                T userControl = new T();
                userControl.Dock = DockStyle.Fill;

                InvokeSetWebSocketClient(userControl);

                pnlControl.Controls.Add(userControl);
            }
        }

        private void InvokeSetWebSocketClient(UserControl userControl)
        {
            var methodInfo = userControl.GetType().GetMethod("SetWebSocketClient");
            if (methodInfo != null)
            {
                methodInfo.Invoke(userControl, new object[] { _webSocketClient });
            }
            else
            {
                Console.WriteLine($"SetWebSocketClient method not found on {userControl.GetType().Name}");
            }
        }

        private void btnDeviceManager_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucDeviceManager>();
        }

        private void btnUserManager_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucUserManagement>();
        }

        private void btnSchedule_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucSchedule>();
        }

        private void btnProgress_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucProgress>();
        }

        private void btnDeviceManage_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucProgressManagement>();
        }

        private void btnDistribution_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucDistribution>();
        }

        private void btnDeviceOutput_Click(object sender, EventArgs e)
        {
            ShowUserControl<ucDeviceOutput>();
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Global.Username))
            {
                barSubItem1.Caption = resourceManager.GetString("Guest");
            }
            else
            {
                barSubItem1.Caption = Global.Username;
            }

            UpdateConnectionStatus(ConnectionManager.Instance.IsConnected);
        }


        private void InitializeStatusMapping()
        {
            statusMapping = new Dictionary<string, string>
            {
                { "DeviceManagerStatus", resourceManager.GetString("DeviceManagerStatus") },
                { "UserManagerStatus", resourceManager.GetString("UserManagerStatus") },
                { "ProductionScheduleStatus", resourceManager.GetString("ProductionSchedule") },
                { "CuttingMachineManagerStatus", resourceManager.GetString("CuttingMachineManager") },
                { "PODistributionStatus", resourceManager.GetString("PODistribution") }
            };
        }

        private void UpdateFormTexts()
        {
            btnLogOut.Caption = resourceManager.GetString("LogOut");
            Language.Caption = resourceManager.GetString("Language");
            this.Text = resourceManager.GetString("Home");
            accordionControlElement1.Text = resourceManager.GetString("ProductionSchedule");
            accordionControlElement4.Text = resourceManager.GetString("CuttingManager");
            accordionControlElement3.Text = resourceManager.GetString("SystemManagerment");
            barSubItem1.Caption = resourceManager.GetString("Guest");

            btnDeviceManager.Text = resourceManager.GetString("DeviceManager");
            btnMonthlyPlan.Text = resourceManager.GetString("MonthlyPlan");
            btnDistribution.Text = resourceManager.GetString("Distribution");
            btnUserManager.Text = resourceManager.GetString("UserManager");
            btnDeviceOutput.Text = resourceManager.GetString("DeviceOutput");
        }


        private void toggleLanguage_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            bool isChecked = toggleLanguage.Checked;
            string selectedLanguage = isChecked ? "vi" : "en";
            LanguageSettings.ChangeLanguage(selectedLanguage);

            resourceManager = new ResourceManager($"DigitalProduction.{LanguageSettings.CurrentLanguage}", typeof(frmMain).Assembly);
            UpdateFormTexts();

            // Update the status item captions to the new language
            UpdateConnectionStatus(ConnectionManager.Instance.IsConnected);
            UpdateReconnectStatus(ConnectionManager.Instance.IsReconnecting);

            RefreshControlsLanguage();
        }

        private void RefreshControlsLanguage()
        {
            foreach (Control control in pnlControl.Controls)
            {
                switch (control)
                {
                    case ucDeviceManager deviceManager:
                        deviceManager.RefreshLanguage();
                        break;
                    case ucUserManagement userManager:
                        userManager.RefreshLanguage();
                        break;
                    case ucSchedule scheduleManager:
                        scheduleManager.RefreshLanguage();
                        break;
                    case ucProgress progress:
                        progress.RefreshLanguage();
                        break;
                    case ucDistribution distribution:
                        distribution.RefreshLanguage();
                        break;
                    case ucProgressManagement deviceManagerment:
                        deviceManagerment.RefreshLanguage();
                        break;
                }
            }
        }

        public static class LanguageSettings
        {
            private static string _currentLanguage = "en";

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
