using System;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Validation;
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class frmLogin : XtraForm
    {
        private ResourceManager resourceManager;
        private bool _isPasswordVisible = false;

        public frmLogin()
        {
            InitializeComponent();
            resourceManager = new ResourceManager("DigitalProduction.Resource", typeof(frmMain).Assembly);

            // Setup password textbox
            txt_pwd.Properties.UseSystemPasswordChar = true;

            // Setup language toggle
            toggleLanguage.Properties.OnText = LocalizationManager.GetString("  English");
            toggleLanguage.Properties.OffText = LocalizationManager.GetString("  Tiếng Việt");
            toggleLanguage.Toggled += toggleLanguage_Toggled;

            // Setup events
            picEye.Click += PicEye_Click;

            // UI update
            UpdateUI();

            // Load saved credentials
            if (Properties.Settings.Default.RememberMe)
            {
                txt_username.Text = Properties.Settings.Default.SavedUsername;
                txt_pwd.Text = Properties.Settings.Default.SavedPassword; // ❗ You should encrypt this
                chkRememberMe.Checked = true;
            }
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            txt_username.Enter += txt_User_Pwd_Enter;
            txt_pwd.Enter += txt_User_Pwd_Enter;
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Clear placeholder on enter.
        /// </summary>
        private void txt_User_Pwd_Enter(object sender, EventArgs e)
        {
            var txtBox = sender as TextEdit;
            if (txtBox == null) return;

            if (txtBox.Text == LocalizationManager.GetString("InputUser") ||
                txtBox.Text == LocalizationManager.GetString("InputPassword"))
            {
                txtBox.Text = "";
                txtBox.ForeColor = Color.Black;
            }
        }

        /// <summary>
        /// Toggle password visibility.
        /// </summary>
        private void PicEye_Click(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            txt_pwd.Properties.UseSystemPasswordChar = !_isPasswordVisible;
            picEye.Image = _isPasswordVisible ? Properties.Resources.icon_eye : Properties.Resources.icon_eye_close;
        }

        /// <summary>
        /// Login logic.
        /// </summary>
        private void btn_Login_Click(object sender, EventArgs e)
        {
            if (!txt_username.ValidateInput(ValidationType.NotEmptyString) ||
                !txt_pwd.ValidateInput(ValidationType.NotEmptyString))
            {
                MessageBox.Show("Invalid input! Please correct the highlighted fields.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string username = txt_username.Text;
            string password = SecurityHelper.HashPassword(txt_pwd.Text);

            bool checkLogin = DbHelper.LoginUser(username, password);
            if (checkLogin)
            {
                if (chkRememberMe.Checked)
                {
                    Properties.Settings.Default.RememberMe = true;
                    Properties.Settings.Default.SavedUsername = username;
                    Properties.Settings.Default.SavedPassword = txt_pwd.Text; // ❗ Consider encrypting this
                }
                else
                {
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.SavedUsername = "";
                    Properties.Settings.Default.SavedPassword = "";
                }

                Properties.Settings.Default.Save();

                frmMain formMain = new frmMain();
                formMain.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("LoginFailedMessage"),
                    LocalizationManager.GetString("LoginFailedTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblExit_MouseEnter(object sender, EventArgs e)
        {
            lblExit.ForeColor = Color.Red;
        }

        private void lblExit_MouseLeave(object sender, EventArgs e)
        {
            lblExit.ForeColor = Color.Black;
        }

        /// <summary>
        /// Update text labels based on current language.
        /// </summary>
        private void UpdateUI()
        {
            this.SuspendLayout();
            try
            {
                string selectedLanguage = toggleLanguage.IsOn ? "en" : "vi";
                LocalizationManager.SetLanguage(selectedLanguage);

                this.Text = LocalizationManager.GetString("frmLogin_Title");
                btn_Login.Text = LocalizationManager.GetString("Login");
                lblExit.Text = LocalizationManager.GetString("Exit");
                lblShowPassword.Text = LocalizationManager.GetString("ChangePassword");
                chkRememberMe.Text = LocalizationManager.GetString("RememberMe");

                txt_username.Properties.NullValuePrompt = LocalizationManager.GetString("InputUser");
                txt_pwd.Properties.NullValuePrompt = LocalizationManager.GetString("InputPassword");
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Handle language toggle.
        /// </summary>
        private void toggleLanguage_Toggled(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                this.SuspendLayout();

                string selectedLanguage = toggleLanguage.IsOn ? "en" : "vi";
                LanguageSettings.ChangeLanguage(selectedLanguage);
                LocalizationManager.SetLanguage(selectedLanguage);

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing language: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.ResumeLayout(true);
                Cursor.Current = Cursors.Default;
            }
        }

        private void lblChangePassword_Click(object sender, EventArgs e)
        {
            ChangePassword changePasswordForm = new ChangePassword();
            changePasswordForm.ShowDialog();
        }
    }
}
