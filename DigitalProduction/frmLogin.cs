using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace DigitalProduction
{
    public partial class frmLogin : DevExpress.XtraEditors.XtraForm
    {
        public frmLogin()
        {
            InitializeComponent();
            // event handler and hide pwd 
            txt_pwd.Properties.UseSystemPasswordChar = true;
            icon_eye.Click += PicEye_Click;
        }

        // Flag to track password visibility
        private bool _isPasswordVisible = false;
        private void frmLogin_Load(object sender, EventArgs e)
        {
            removeBackGroundImage(txtIcon_user, icon_username);
            removeBackGroundImage(txtIcon_pwd, icon_pwd);
            removeBackGroundImage(txtIcon_pwd, icon_eye);
            txt_username.Enter += txt_User_Pwd_Enter;
            txt_username.Leave += txt_User_Pwd_Leave;
            txt_pwd.Enter += txt_User_Pwd_Enter;
            txt_pwd.Leave += txt_User_Pwd_Leave;
        }

        private void removeBackGroundImage(PictureBox pb, PictureBox pb2)
        {
            // Get pb2's screen position.
            Point screenPos = pb2.PointToScreen(Point.Empty);

            // Option A: When reparenting to pb.
            Point newPos = pb.PointToClient(screenPos);

            // Reparent and adjust
            pb2.Parent = pb;
            pb2.Location = newPos;
            pb2.Visible = true;
            pb2.BringToFront();
            pb2.BackColor = Color.Transparent;
        }


        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txt_User_Pwd_Enter(object sender, EventArgs e)
        {
            TextEdit txtBox = sender as TextEdit;

            if (txtBox == null) return; //return if null

            // Clear the TextBox if it contains placeholder text
            if (txtBox.Text == "User Name" || txtBox.Text == "Password")  // Replace with your placeholder text
            {
                txtBox.Text = "";
                txtBox.ForeColor = System.Drawing.Color.Black;  // Change text color to black for actual input
            }
        }
        private void txt_User_Pwd_Leave(object sender, EventArgs e)
        {
            TextEdit txtBox = sender as TextEdit;

            if (txtBox == null) return; //return if null

            if (string.IsNullOrEmpty(txtBox.Text))
            {
                txtBox.Text = "User Name";  // Set placeholder text
                txtBox.ForeColor = System.Drawing.Color.Gray;
            }
            if (txtBox.Text == "Password")
            {
                txtBox.Text = "*********";  // Set placeholder text
                txtBox.ForeColor = System.Drawing.Color.Gray;
            }
        }
        private void PicEye_Click(object sender, EventArgs e)
        {
            // Toggle the password visibility flag
            _isPasswordVisible = !_isPasswordVisible;

            // Set the TextBox property based on the flag
            txt_pwd.Properties.UseSystemPasswordChar = !_isPasswordVisible;

            // Change the PictureBox image accordingly
            if (_isPasswordVisible)
            {
                txt_pwd.Properties.PasswordChar = '\0';
                icon_eye.Image = Properties.Resources.icon_eye;
            }
            else
            {
                icon_eye.Image = Properties.Resources.icon_eye_close;
            }
        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            bool checkLogin = DbHelper.loginUser(txt_username.Text, SecurityHelper.HashPassword(txt_pwd.Text));
            if (checkLogin)
            {
                MessageBox.Show("Login Successful! Welcome", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                MessageBox.Show("Invalid username or password, or account is inactive.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}