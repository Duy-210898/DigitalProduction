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
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.txt_employeeID = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.txt_employeeName = new DevExpress.XtraEditors.TextEdit();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.txt_pwd = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.txt_username = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.cb_department = new DevExpress.XtraEditors.LookUpEdit();
            this.cb_position = new DevExpress.XtraEditors.LookUpEdit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_position.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.cb_position);
            this.groupControl1.Controls.Add(this.cb_department);
            this.groupControl1.Controls.Add(this.labelControl6);
            this.groupControl1.Controls.Add(this.btn_close);
            this.groupControl1.Controls.Add(this.btn_submit);
            this.groupControl1.Controls.Add(this.labelControl5);
            this.groupControl1.Controls.Add(this.txt_employeeID);
            this.groupControl1.Controls.Add(this.labelControl4);
            this.groupControl1.Controls.Add(this.txt_employeeName);
            this.groupControl1.Controls.Add(this.labelControl3);
            this.groupControl1.Controls.Add(this.txt_pwd);
            this.groupControl1.Controls.Add(this.labelControl2);
            this.groupControl1.Controls.Add(this.txt_username);
            this.groupControl1.Controls.Add(this.labelControl1);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(536, 354);
            this.groupControl1.TabIndex = 0;
            this.groupControl1.Text = "Register User";
            // 
            // labelControl6
            // 
            this.labelControl6.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl6.Appearance.Options.UseFont = true;
            this.labelControl6.Location = new System.Drawing.Point(19, 262);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(62, 19);
            this.labelControl6.TabIndex = 12;
            this.labelControl6.Text = "Position:";
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
            this.btn_submit.Size = new System.Drawing.Size(75, 23);
            this.btn_submit.TabIndex = 7;
            this.btn_submit.Text = "Submit";
            this.btn_submit.Click += new System.EventHandler(this.btn_submit_Click);
            // 
            // labelControl5
            // 
            this.labelControl5.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl5.Appearance.Options.UseFont = true;
            this.labelControl5.Location = new System.Drawing.Point(19, 219);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(89, 19);
            this.labelControl5.TabIndex = 8;
            this.labelControl5.Text = "Department:";
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
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(19, 129);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(97, 19);
            this.labelControl4.TabIndex = 6;
            this.labelControl4.Text = "Employee ID:";
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
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(19, 181);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(121, 19);
            this.labelControl3.TabIndex = 4;
            this.labelControl3.Text = "Employee Name:";
            // 
            // txt_pwd
            // 
            this.txt_pwd.Location = new System.Drawing.Point(160, 85);
            this.txt_pwd.Name = "txt_pwd";
            this.txt_pwd.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_pwd.Properties.Appearance.Options.UseFont = true;
            this.txt_pwd.Size = new System.Drawing.Size(171, 26);
            this.txt_pwd.TabIndex = 2;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(19, 88);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(73, 19);
            this.labelControl2.TabIndex = 2;
            this.labelControl2.Text = "Password:";
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
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(19, 45);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(87, 19);
            this.labelControl1.TabIndex = 0;
            this.labelControl1.Text = "User name: ";
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
            // ucRegisterUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControl1);
            this.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Name = "ucRegisterUser";
            this.Size = new System.Drawing.Size(536, 354);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            this.groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_pwd.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_username.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_position.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.TextEdit txt_username;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.TextEdit txt_employeeID;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.TextEdit txt_employeeName;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.TextEdit txt_pwd;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.LookUpEdit cb_position;
        private DevExpress.XtraEditors.LookUpEdit cb_department;
    }
}
