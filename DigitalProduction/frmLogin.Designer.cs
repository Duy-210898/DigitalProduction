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
            this.icon_eye = new System.Windows.Forms.PictureBox();
            this.txt_pwd = new DevExpress.XtraEditors.TextEdit();
            this.txt_username = new DevExpress.XtraEditors.TextEdit();
            this.btn_Login = new DevExpress.XtraEditors.SimpleButton();
            this.lblExit = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.icon_eye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // icon_eye
            // 
            this.icon_eye.Cursor = System.Windows.Forms.Cursors.Hand;
            this.icon_eye.Image = global::DigitalProduction.Properties.Resources.icon_eye_close;
            this.icon_eye.Location = new System.Drawing.Point(705, 336);
            this.icon_eye.Name = "icon_eye";
            this.icon_eye.Size = new System.Drawing.Size(33, 23);
            this.icon_eye.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.icon_eye.TabIndex = 28;
            this.icon_eye.TabStop = false;
            // 
            // txt_pwd
            // 
            this.txt_pwd.EditValue = "";
            this.txt_pwd.Location = new System.Drawing.Point(510, 332);
            this.txt_pwd.Name = "txt_pwd";
            this.txt_pwd.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.txt_pwd.Properties.Appearance.Font = new System.Drawing.Font("Century Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_pwd.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txt_pwd.Properties.Appearance.Options.UseBackColor = true;
            this.txt_pwd.Properties.Appearance.Options.UseFont = true;
            this.txt_pwd.Properties.Appearance.Options.UseForeColor = true;
            this.txt_pwd.Properties.AutoHeight = false;
            this.txt_pwd.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txt_pwd.Size = new System.Drawing.Size(189, 28);
            this.txt_pwd.TabIndex = 25;
            // 
            // txt_username
            // 
            this.txt_username.EditValue = "";
            this.txt_username.Location = new System.Drawing.Point(510, 244);
            this.txt_username.Name = "txt_username";
            this.txt_username.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.txt_username.Properties.Appearance.Font = new System.Drawing.Font("Century Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_username.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txt_username.Properties.Appearance.Options.UseBackColor = true;
            this.txt_username.Properties.Appearance.Options.UseFont = true;
            this.txt_username.Properties.Appearance.Options.UseForeColor = true;
            this.txt_username.Properties.AutoHeight = false;
            this.txt_username.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txt_username.Size = new System.Drawing.Size(189, 28);
            this.txt_username.TabIndex = 24;
            // 
            // btn_Login
            // 
            this.btn_Login.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btn_Login.Appearance.Options.UseFont = true;
            this.btn_Login.Location = new System.Drawing.Point(482, 388);
            this.btn_Login.LookAndFeel.SkinMaskColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_Login.LookAndFeel.SkinName = "McSkin";
            this.btn_Login.LookAndFeel.UseDefaultLookAndFeel = false;
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(165, 39);
            this.btn_Login.TabIndex = 29;
            this.btn_Login.Text = "Login";
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // lblExit
            // 
            this.lblExit.AutoSize = true;
            this.lblExit.BackColor = System.Drawing.Color.Transparent;
            this.lblExit.Font = new System.Drawing.Font("Tahoma", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExit.ForeColor = System.Drawing.Color.Black;
            this.lblExit.Location = new System.Drawing.Point(771, 9);
            this.lblExit.Name = "lblExit";
            this.lblExit.Size = new System.Drawing.Size(27, 16);
            this.lblExit.TabIndex = 30;
            this.lblExit.Text = "Exit";
            this.lblExit.Click += new System.EventHandler(this.lblExit_Click);
            this.lblExit.MouseEnter += new System.EventHandler(this.lblExit_MouseEnter);
            this.lblExit.MouseLeave += new System.EventHandler(this.lblExit_MouseLeave);
            // 
            // frmLogin
            // 
            this.AcceptButton = this.btn_Login;
            this.Appearance.BackColor = System.Drawing.Color.White;
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayoutStore = System.Windows.Forms.ImageLayout.Stretch;
            this.BackgroundImageStore = global::DigitalProduction.Properties.Resources.bg_login;
            this.ClientSize = new System.Drawing.Size(810, 628);
            this.Controls.Add(this.lblExit);
            this.Controls.Add(this.btn_Login);
            this.Controls.Add(this.icon_eye);
            this.Controls.Add(this.txt_pwd);
            this.Controls.Add(this.txt_username);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmLogin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmLogin";
            this.Load += new System.EventHandler(this.frmLogin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.icon_eye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox icon_eye;
        private DevExpress.XtraEditors.TextEdit txt_pwd;
        private DevExpress.XtraEditors.TextEdit txt_username;
        private DevExpress.XtraEditors.SimpleButton btn_Login;
        private System.Windows.Forms.Label lblExit;
    }
}