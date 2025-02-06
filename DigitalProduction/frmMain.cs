using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using System.Threading.Tasks;
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
            var methodInfo = userControl.GetType().GetMethod("SetWebSocketClient", new Type[] { typeof(WebSocketClient) });
            if (methodInfo != null)
            {
                _webSocketClient.ClearEventHandlers();
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
            ucRegisterUser register= new ucRegisterUser();
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
                barSubItem1.Caption = LocalizationManager.GetString("Guest");
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
            Language.Caption = LocalizationManager.GetString("Language");
            this.Text = LocalizationManager.GetString("Home");
            accordionControlElement1.Text = LocalizationManager.GetString("ProductionSchedule");
            accordionControlElement4.Text = LocalizationManager.GetString("CuttingManager");
            accordionControlElement3.Text = LocalizationManager.GetString("SystemManagerment");
            barSubItem1.Caption = LocalizationManager.GetString("Guest");

            btnDeviceManager.Text = LocalizationManager.GetString("DeviceManager");
            btnMonthlyPlan.Text = LocalizationManager.GetString("MonthlyPlan");
            btnDistribution.Text = LocalizationManager.GetString("Distribution");
            btnUserManager.Text = LocalizationManager.GetString("UserManager");
            btnDeviceOutput.Text = LocalizationManager.GetString("DeviceOutput");
        }


        private void toggleLanguage_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            bool isChecked = toggleLanguage.Checked;
            string selectedLanguage = isChecked ? "vi" : "en";

            LanguageSettings.ChangeLanguage(selectedLanguage);
            LocalizationManager.SetLanguage(selectedLanguage);
            resourceManager = new ResourceManager($"DigitalProduction.{LanguageSettings.CurrentLanguage}", typeof(frmMain).Assembly);

            UpdateFormTexts();

            UpdateConnectionStatus(ConnectionManager.Instance.IsConnected);
            UpdateConnectionStatus(ConnectionManager.Instance.IsConnected);
            UpdateReconnectStatus(ConnectionManager.Instance.IsReconnecting);

            RefreshControlsLanguage(selectedLanguage);

            // Khôi phục lại con trỏ chuột về trạng thái ban đầu (thường là con trỏ mặc định)
            Cursor.Current = Cursors.Default;
        }

        private void RefreshControlsLanguage(string selectedLanguage)
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
