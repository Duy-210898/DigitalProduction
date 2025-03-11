namespace DigitalProduction
{
    partial class frmLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.picEye = new System.Windows.Forms.PictureBox();
            this.txt_pwd = new DevExpress.XtraEditors.TextEdit();
            this.txt_username = new DevExpress.XtraEditors.TextEdit();
            this.lblExit = new System.Windows.Forms.Label();
            this.btn_Login = new DevExpress.XtraEditors.SimpleButton();
            this.lblShowPassword = new System.Windows.Forms.Label();
            this.toggleLanguage = new DevExpress.XtraEditors.ToggleSwitch();
            this.chkRememberMe = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picEye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toggleLanguage.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // picEye
            // 
            this.picEye.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picEye.Image = global::DigitalProduction.Properties.Resources.icon_eye_close;
            this.picEye.Location = new System.Drawing.Point(769, 352);
            this.picEye.Name = "picEye";
            this.picEye.Size = new System.Drawing.Size(20, 20);
            this.picEye.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picEye.TabIndex = 28;
            this.picEye.TabStop = false;
            // 
            // txt_pwd
            // 
            this.txt_pwd.EditValue = "";
            this.txt_pwd.Location = new System.Drawing.Point(539, 346);
            this.txt_pwd.Name = "txt_pwd";
            this.txt_pwd.Properties.Appearance.BackColor = System.Drawing.Color.Gainsboro;
            this.txt_pwd.Properties.Appearance.Font = new System.Drawing.Font("Century Gothic", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_pwd.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txt_pwd.Properties.Appearance.Options.UseBackColor = true;
            this.txt_pwd.Properties.Appearance.Options.UseFont = true;
            this.txt_pwd.Properties.Appearance.Options.UseForeColor = true;
            this.txt_pwd.Properties.AutoHeight = false;
            this.txt_pwd.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txt_pwd.Size = new System.Drawing.Size(220, 30);
            this.txt_pwd.TabIndex = 25;
            // 
            // txt_username
            // 
            this.txt_username.EditValue = "";
            this.txt_username.Location = new System.Drawing.Point(539, 276);
            this.txt_username.Name = "txt_username";
            this.txt_username.Properties.Appearance.BackColor = System.Drawing.Color.Gainsboro;
            this.txt_username.Properties.Appearance.Font = new System.Drawing.Font("Century Gothic", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_username.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txt_username.Properties.Appearance.Options.UseBackColor = true;
            this.txt_username.Properties.Appearance.Options.UseFont = true;
            this.txt_username.Properties.Appearance.Options.UseForeColor = true;
            this.txt_username.Properties.AutoHeight = false;
            this.txt_username.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txt_username.Size = new System.Drawing.Size(220, 30);
            this.txt_username.TabIndex = 24;
            // 
            // lblExit
            // 
            this.lblExit.AutoSize = true;
            this.lblExit.BackColor = System.Drawing.Color.Transparent;
            this.lblExit.Font = new System.Drawing.Font("Tahoma", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExit.ForeColor = System.Drawing.Color.Black;
            this.lblExit.Location = new System.Drawing.Point(849, 9);
            this.lblExit.Name = "lblExit";
            this.lblExit.Size = new System.Drawing.Size(27, 16);
            this.lblExit.TabIndex = 30;
            this.lblExit.Text = "Exit";
            this.lblExit.Click += new System.EventHandler(this.lblExit_Click);
            this.lblExit.MouseEnter += new System.EventHandler(this.lblExit_MouseEnter);
            this.lblExit.MouseLeave += new System.EventHandler(this.lblExit_MouseLeave);
            // 
            // btn_Login
            // 
            this.btn_Login.Appearance.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Login.Appearance.ForeColor = System.Drawing.Color.White;
            this.btn_Login.Appearance.Options.UseFont = true;
            this.btn_Login.Appearance.Options.UseForeColor = true;
            this.btn_Login.Location = new System.Drawing.Point(551, 413);
            this.btn_Login.LookAndFeel.SkinMaskColor = System.Drawing.Color.Black;
            this.btn_Login.LookAndFeel.SkinMaskColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btn_Login.LookAndFeel.SkinName = "WXI";
            this.btn_Login.LookAndFeel.UseDefaultLookAndFeel = false;
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(208, 35);
            this.btn_Login.TabIndex = 31;
            this.btn_Login.Text = "Log in";
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // lblShowPassword
            // 
            this.lblShowPassword.AutoSize = true;
            this.lblShowPassword.BackColor = System.Drawing.Color.Transparent;
            this.lblShowPassword.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblShowPassword.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShowPassword.ForeColor = System.Drawing.Color.Gray;
            this.lblShowPassword.Location = new System.Drawing.Point(607, 458);
            this.lblShowPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblShowPassword.Name = "lblShowPassword";
            this.lblShowPassword.Size = new System.Drawing.Size(93, 17);
            this.lblShowPassword.TabIndex = 32;
            this.lblShowPassword.Text = "Đổi mật khẩu";
            this.lblShowPassword.Click += new System.EventHandler(this.lblChangePassword_Click);
            // 
            // toggleLanguage
            // 
            this.toggleLanguage.Location = new System.Drawing.Point(685, 8);
            this.toggleLanguage.Name = "toggleLanguage";
            this.toggleLanguage.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toggleLanguage.Properties.Appearance.Options.UseFont = true;
            this.toggleLanguage.Properties.OffText = "Off";
            this.toggleLanguage.Properties.OnText = "On";
            this.toggleLanguage.Size = new System.Drawing.Size(158, 19);
            this.toggleLanguage.TabIndex = 33;
            this.toggleLanguage.Toggled += new System.EventHandler(this.toggleLanguage_Toggled);
            // 
            // chkRememberMe
            // 
            this.chkRememberMe.AutoSize = true;
            this.chkRememberMe.Location = new System.Drawing.Point(610, 390);
            this.chkRememberMe.Name = "chkRememberMe";
            this.chkRememberMe.Size = new System.Drawing.Size(94, 17);
            this.chkRememberMe.TabIndex = 34;
            this.chkRememberMe.Text = "Remember me";
            this.chkRememberMe.UseVisualStyleBackColor = true;
            // 
            // frmLogin
            // 
            this.AcceptButton = this.btn_Login;
            this.Appearance.BackColor = System.Drawing.Color.White;
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayoutStore = System.Windows.Forms.ImageLayout.Stretch;
            this.BackgroundImageStore = global::DigitalProduction.Properties.Resources.bg;
            this.ClientSize = new System.Drawing.Size(905, 628);
            this.Controls.Add(this.chkRememberMe);
            this.Controls.Add(this.toggleLanguage);
            this.Controls.Add(this.btn_Login);
            this.Controls.Add(this.lblShowPassword);
            this.Controls.Add(this.lblExit);
            this.Controls.Add(this.picEye);
            this.Controls.Add(this.txt_pwd);
            this.Controls.Add(this.txt_username);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmLogin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmLogin";
            this.Load += new System.EventHandler(this.frmLogin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picEye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toggleLanguage.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox picEye;
        private DevExpress.XtraEditors.TextEdit txt_pwd;
        private DevExpress.XtraEditors.TextEdit txt_username;
        private System.Windows.Forms.Label lblExit;
        private DevExpress.XtraEditors.SimpleButton btn_Login;
        private System.Windows.Forms.Label lblShowPassword;
        private DevExpress.XtraEditors.ToggleSwitch toggleLanguage;
        private System.Windows.Forms.CheckBox chkRememberMe;
    }
}