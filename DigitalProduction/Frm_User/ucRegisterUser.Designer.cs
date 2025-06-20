namespace DigitalProduction
{
    partial class ucRegisterUser
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupControlRegisterUser = new DevExpress.XtraEditors.GroupControl();
            this.cb_position = new DevExpress.XtraEditors.LookUpEdit();
            this.cb_department = new DevExpress.XtraEditors.LookUpEdit();
            this.lblPosition = new DevExpress.XtraEditors.LabelControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.lblDepartment = new DevExpress.XtraEditors.LabelControl();
            this.txt_employeeID = new DevExpress.XtraEditors.TextEdit();
            this.lblEmployeeID = new DevExpress.XtraEditors.LabelControl();
            this.txt_employeeName = new DevExpress.XtraEditors.TextEdit();
            this.lblEmployeeName = new DevExpress.XtraEditors.LabelControl();
            this.txt_pwd = new DevExpress.XtraEditors.TextEdit();
            this.lblPassword = new DevExpress.XtraEditors.LabelControl();
            this.txt_username = new DevExpress.XtraEditors.TextEdit();
            this.lblUserName = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControlRegisterUser)).BeginInit();
            this.groupControlRegisterUser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_position.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControlRegisterUser
            // 
            this.groupControlRegisterUser.CaptionLocation = DevExpress.Utils.Locations.Top;
            this.groupControlRegisterUser.Controls.Add(this.cb_position);
            this.groupControlRegisterUser.Controls.Add(this.cb_department);
            this.groupControlRegisterUser.Controls.Add(this.lblPosition);
            this.groupControlRegisterUser.Controls.Add(this.btn_close);
            this.groupControlRegisterUser.Controls.Add(this.btn_submit);
            this.groupControlRegisterUser.Controls.Add(this.lblDepartment);
            this.groupControlRegisterUser.Controls.Add(this.txt_employeeID);
            this.groupControlRegisterUser.Controls.Add(this.lblEmployeeID);
            this.groupControlRegisterUser.Controls.Add(this.txt_employeeName);
            this.groupControlRegisterUser.Controls.Add(this.lblEmployeeName);
            this.groupControlRegisterUser.Controls.Add(this.txt_pwd);
            this.groupControlRegisterUser.Controls.Add(this.lblPassword);
            this.groupControlRegisterUser.Controls.Add(this.txt_username);
            this.groupControlRegisterUser.Controls.Add(this.lblUserName);
            this.groupControlRegisterUser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControlRegisterUser.Location = new System.Drawing.Point(0, 0);
            this.groupControlRegisterUser.Name = "groupControlRegisterUser";
            this.groupControlRegisterUser.Size = new System.Drawing.Size(536, 354);
            this.groupControlRegisterUser.TabIndex = 0;
            this.groupControlRegisterUser.Text = "Register User";
            // 
            // cb_position
            // 
            this.cb_position.Location = new System.Drawing.Point(160, 259);
            this.cb_position.Name = "cb_position";
            this.cb_position.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 11F);
            this.cb_position.Properties.Appearance.Options.UseFont = true;
            this.cb_position.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cb_position.Properties.NullText = "Select";
            this.cb_position.Size = new System.Drawing.Size(171, 24);
            this.cb_position.TabIndex = 6;
            // 
            // cb_department
            // 
            this.cb_department.Location = new System.Drawing.Point(160, 216);
            this.cb_department.Name = "cb_department";
            this.cb_department.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 11F);
            this.cb_department.Properties.Appearance.Options.UseFont = true;
            this.cb_department.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cb_department.Properties.NullText = "Select";
            this.cb_department.Size = new System.Drawing.Size(171, 24);
            this.cb_department.TabIndex = 5;
            // 
            // lblPosition
            // 
            this.lblPosition.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblPosition.Appearance.Options.UseFont = true;
            this.lblPosition.Location = new System.Drawing.Point(19, 262);
            this.lblPosition.Name = "lblPosition";
            this.lblPosition.Size = new System.Drawing.Size(62, 19);
            this.lblPosition.TabIndex = 12;
            this.lblPosition.Text = "Position:";
            // 
            // btn_close
            // 
            this.btn_close.Location = new System.Drawing.Point(256, 305);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(75, 23);
            this.btn_close.TabIndex = 8;
            this.btn_close.Text = "Cancel";
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // btn_submit
            // 
            this.btn_submit.Location = new System.Drawing.Point(160, 305);
            this.btn_submit.Name = "btn_submit";
            this.btn_submit.Size = new System.Drawing.Size(74, 23);
            this.btn_submit.TabIndex = 7;
            this.btn_submit.Text = "Submit";
            this.btn_submit.Click += new System.EventHandler(this.btn_submit_Click);
            // 
            // lblDepartment
            // 
            this.lblDepartment.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblDepartment.Appearance.Options.UseFont = true;
            this.lblDepartment.Location = new System.Drawing.Point(19, 219);
            this.lblDepartment.Name = "lblDepartment";
            this.lblDepartment.Size = new System.Drawing.Size(89, 19);
            this.lblDepartment.TabIndex = 8;
            this.lblDepartment.Text = "Department:";
            // 
            // txt_employeeID
            // 
            this.txt_employeeID.Location = new System.Drawing.Point(160, 126);
            this.txt_employeeID.Name = "txt_employeeID";
            this.txt_employeeID.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_employeeID.Properties.Appearance.Options.UseFont = true;
            this.txt_employeeID.Size = new System.Drawing.Size(171, 26);
            this.txt_employeeID.TabIndex = 3;
            // 
            // lblEmployeeID
            // 
            this.lblEmployeeID.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblEmployeeID.Appearance.Options.UseFont = true;
            this.lblEmployeeID.Location = new System.Drawing.Point(19, 129);
            this.lblEmployeeID.Name = "lblEmployeeID";
            this.lblEmployeeID.Size = new System.Drawing.Size(97, 19);
            this.lblEmployeeID.TabIndex = 6;
            this.lblEmployeeID.Text = "Employee ID:";
            // 
            // txt_employeeName
            // 
            this.txt_employeeName.Location = new System.Drawing.Point(160, 174);
            this.txt_employeeName.Name = "txt_employeeName";
            this.txt_employeeName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_employeeName.Properties.Appearance.Options.UseFont = true;
            this.txt_employeeName.Size = new System.Drawing.Size(171, 26);
            this.txt_employeeName.TabIndex = 4;
            // 
            // lblEmployeeName
            // 
            this.lblEmployeeName.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblEmployeeName.Appearance.Options.UseFont = true;
            this.lblEmployeeName.Location = new System.Drawing.Point(19, 181);
            this.lblEmployeeName.Name = "lblEmployeeName";
            this.lblEmployeeName.Size = new System.Drawing.Size(121, 19);
            this.lblEmployeeName.TabIndex = 4;
            this.lblEmployeeName.Text = "Employee Name:";
            // 
            // txt_pwd
            // 
            this.txt_pwd.Location = new System.Drawing.Point(160, 85);
            this.txt_pwd.Name = "txt_pwd";
            this.txt_pwd.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_pwd.Properties.Appearance.Options.UseFont = true;
            this.txt_pwd.Properties.UseSystemPasswordChar = true;
            this.txt_pwd.Size = new System.Drawing.Size(171, 26);
            this.txt_pwd.TabIndex = 2;
            // 
            // lblPassword
            // 
            this.lblPassword.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblPassword.Appearance.Options.UseFont = true;
            this.lblPassword.Location = new System.Drawing.Point(19, 88);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(73, 19);
            this.lblPassword.TabIndex = 2;
            this.lblPassword.Text = "Password:";
            // 
            // txt_username
            // 
            this.txt_username.Location = new System.Drawing.Point(160, 42);
            this.txt_username.Name = "txt_username";
            this.txt_username.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_username.Properties.Appearance.Options.UseFont = true;
            this.txt_username.Size = new System.Drawing.Size(171, 26);
            this.txt_username.TabIndex = 1;
            // 
            // lblUserName
            // 
            this.lblUserName.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblUserName.Appearance.Options.UseFont = true;
            this.lblUserName.Location = new System.Drawing.Point(19, 45);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(87, 19);
            this.lblUserName.TabIndex = 0;
            this.lblUserName.Text = "User name: ";
            // 
            // ucRegisterUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControlRegisterUser);
            this.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Name = "ucRegisterUser";
            this.Size = new System.Drawing.Size(536, 354);
            ((System.ComponentModel.ISupportInitialize)(this.groupControlRegisterUser)).EndInit();
            this.groupControlRegisterUser.ResumeLayout(false);
            this.groupControlRegisterUser.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_position.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControlRegisterUser;
        private DevExpress.XtraEditors.TextEdit txt_username;
        private DevExpress.XtraEditors.LabelControl lblUserName;
        private DevExpress.XtraEditors.LabelControl lblDepartment;
        private DevExpress.XtraEditors.TextEdit txt_employeeID;
        private DevExpress.XtraEditors.LabelControl lblEmployeeID;
        private DevExpress.XtraEditors.TextEdit txt_employeeName;
        private DevExpress.XtraEditors.LabelControl lblEmployeeName;
        private DevExpress.XtraEditors.TextEdit txt_pwd;
        private DevExpress.XtraEditors.LabelControl lblPassword;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.LabelControl lblPosition;
        private DevExpress.XtraEditors.LookUpEdit cb_position;
        private DevExpress.XtraEditors.LookUpEdit cb_department;
    }
}
