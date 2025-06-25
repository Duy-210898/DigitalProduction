using System;
using System.Resources;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Validation;
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class frmLogin : DevExpress.XtraEditors.XtraForm
    {
        private ResourceManager resourceManager;

        public frmLogin()
        {
            InitializeComponent();
            resourceManager = new ResourceManager("DigitalProduction.Resource", typeof(frmMain).Assembly);
            txt_pwd.Properties.UseSystemPasswordChar = true;
            toggleLanguage.Properties.OnText = LocalizationManager.GetString("  English");
            toggleLanguage.Properties.OffText = LocalizationManager.GetString("  Tiếng Việt");
            picEye.Click += PicEye_Click;
            UpdateUI();
            // Load saved credentials if "Remember Me" is checked
            if (Properties.Settings.Default.RememberMe)
            {
                txt_username.Text = Properties.Settings.Default.SavedUsername;
                txt_pwd.Text = Properties.Settings.Default.SavedPassword; // Consider hashing/encrypting this in production
                chkRememberMe.Checked = true;
            }
        }

        // Flag to track password visibility
        private bool _isPasswordVisible = false;

        private void frmLogin_Load(object sender, EventArgs e)
        {
            txt_username.Enter += txt_User_Pwd_Enter;
            txt_pwd.Enter += txt_User_Pwd_Enter;
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txt_User_Pwd_Enter(object sender, EventArgs e)
        {
            TextEdit txtBox = sender as TextEdit;

            if (txtBox == null) return; // Return if null

            // Clear the TextBox if it contains placeholder text
            if (txtBox.Text == LocalizationManager.GetString("InputUser") || txtBox.Text == LocalizationManager.GetString("InputPassword"))
            {
                txtBox.Text = "";
                txtBox.ForeColor = System.Drawing.Color.Black;  // Change text color to black for actual input
            }
        }

        private void PicEye_Click(object sender, EventArgs e)
        {
            // Toggle the password visibility flag
            _isPasswordVisible = !_isPasswordVisible;

            // Set the TextBox property based on the flag
            txt_pwd.Properties.UseSystemPasswordChar = !_isPasswordVisible;

            // Change the PictureBox image accordingly
            picEye.Image = _isPasswordVisible ? Properties.Resources.icon_eye : Properties.Resources.icon_eye_close;
        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            if (!txt_username.ValidateInput(ValidationType.NotEmptyString) ||
                !txt_pwd.ValidateInput(ValidationType.NotEmptyString))
            {
                MessageBox.Show("Invalid input! Please correct the highlighted fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool checkLogin = DbHelper.loginUser(txt_username.Text, SecurityHelper.HashPassword(txt_pwd.Text));
            if (checkLogin)
            {
                // Save credentials if "Remember Me" is checked
                if (chkRememberMe.Checked)
                {
                    Properties.Settings.Default.RememberMe = true;
                    Properties.Settings.Default.SavedUsername = txt_username.Text;
                    Properties.Settings.Default.SavedPassword = txt_pwd.Text; // Consider hashing/encrypting
                }
                else
                {
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.SavedUsername = "";
                    Properties.Settings.Default.SavedPassword = "";
                }

                Properties.Settings.Default.Save(); // Save changes

                frmMain formMain = new frmMain();
                formMain.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("LoginFailedMessage"), LocalizationManager.GetString("LoginFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblExit_MouseEnter(object sender, EventArgs e)
        {
            lblExit.ForeColor = System.Drawing.Color.Red;
        }

        private void lblExit_MouseLeave(object sender, EventArgs e)
        {
            lblExit.ForeColor = System.Drawing.Color.Black;
        }

        //}
        private void UpdateUI()
        {
            this.SuspendLayout();  // Suspend layout to batch UI updates

            try
            {
                bool isChecked = toggleLanguage.IsOn;
                string selectedLanguage = isChecked ? "en" : "vi";
                LocalizationManager.SetLanguage(selectedLanguage);

                this.Text = LocalizationManager.GetString("frmLogin_Title");
                btn_Login.Text = LocalizationManager.GetString("Login");
                lblExit.Text = LocalizationManager.GetString("Exit");
                lblShowPassword.Text = LocalizationManager.GetString("ChangePassword");
                chkRememberMe.Text = LocalizationManager.GetString("RememberMe");
            }
            finally
            {
                this.ResumeLayout(true); // Resume layout and perform layout immediately
            }
        }

        private void toggleLanguage_Toggled(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                this.SuspendLayout(); // Suspend layout before UI update

                bool isChecked = toggleLanguage.IsOn;
                string selectedLanguage = isChecked ? "en" : "vi";

                LanguageSettings.ChangeLanguage(selectedLanguage);
                LocalizationManager.SetLanguage(selectedLanguage);

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing language: {ex.Message}", LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.ResumeLayout(true); // Resume layout
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
