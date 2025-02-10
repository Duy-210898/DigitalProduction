namespace DigitalProduction
{
    partial class ucRegisterDevice
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
            this.cb_plant = new DevExpress.XtraEditors.LookUpEdit();
            this.cb_department = new DevExpress.XtraEditors.LookUpEdit();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.txt_machineName = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.txt_deviceName = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.txt_addressIP = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_plant.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_machineName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_deviceName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_addressIP.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.cb_plant);
            this.groupControl1.Controls.Add(this.cb_department);
            this.groupControl1.Controls.Add(this.labelControl6);
            this.groupControl1.Controls.Add(this.btn_close);
            this.groupControl1.Controls.Add(this.btn_submit);
            this.groupControl1.Controls.Add(this.labelControl5);
            this.groupControl1.Controls.Add(this.txt_machineName);
            this.groupControl1.Controls.Add(this.labelControl4);
            this.groupControl1.Controls.Add(this.txt_deviceName);
            this.groupControl1.Controls.Add(this.labelControl2);
            this.groupControl1.Controls.Add(this.txt_addressIP);
            this.groupControl1.Controls.Add(this.labelControl1);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(563, 498);
            this.groupControl1.TabIndex = 1;
            this.groupControl1.Text = "Add new device";
            // 
            // cb_plant
            // 
            this.cb_plant.Location = new System.Drawing.Point(160, 214);
            this.cb_plant.Name = "cb_plant";
            this.cb_plant.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 11F);
            this.cb_plant.Properties.Appearance.Options.UseFont = true;
            this.cb_plant.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cb_plant.Properties.NullText = "Select Plant";
            this.cb_plant.Size = new System.Drawing.Size(171, 24);
            this.cb_plant.TabIndex = 6;
            // 
            // cb_department
            // 
            this.cb_department.Location = new System.Drawing.Point(160, 169);
            this.cb_department.Name = "cb_department";
            this.cb_department.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 11F);
            this.cb_department.Properties.Appearance.Options.UseFont = true;
            this.cb_department.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cb_department.Properties.NullText = "Select Department";
            this.cb_department.Size = new System.Drawing.Size(171, 24);
            this.cb_department.TabIndex = 5;
            // 
            // labelControl6
            // 
            this.labelControl6.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl6.Appearance.Options.UseFont = true;
            this.labelControl6.Location = new System.Drawing.Point(19, 216);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(41, 19);
            this.labelControl6.TabIndex = 12;
            this.labelControl6.Text = "Plant:";
            // 
            // btn_close
            // 
            this.btn_close.Location = new System.Drawing.Point(256, 264);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(75, 23);
            this.btn_close.TabIndex = 8;
            this.btn_close.Text = "Cancel";
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // btn_submit
            // 
            this.btn_submit.Location = new System.Drawing.Point(160, 264);
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
            this.labelControl5.Location = new System.Drawing.Point(19, 171);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(89, 19);
            this.labelControl5.TabIndex = 8;
            this.labelControl5.Text = "Department:";
            // 
            // txt_machineName
            // 
            this.txt_machineName.Location = new System.Drawing.Point(160, 126);
            this.txt_machineName.Name = "txt_machineName";
            this.txt_machineName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_machineName.Properties.Appearance.Options.UseFont = true;
            this.txt_machineName.Size = new System.Drawing.Size(171, 26);
            this.txt_machineName.TabIndex = 3;
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(19, 45);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(83, 19);
            this.labelControl4.TabIndex = 6;
            this.labelControl4.Text = "Address IP:";
            // 
            // txt_deviceName
            // 
            this.txt_deviceName.Location = new System.Drawing.Point(160, 85);
            this.txt_deviceName.Name = "txt_deviceName";
            this.txt_deviceName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_deviceName.Properties.Appearance.Options.UseFont = true;
            this.txt_deviceName.Size = new System.Drawing.Size(171, 26);
            this.txt_deviceName.TabIndex = 2;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(19, 129);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(107, 19);
            this.labelControl2.TabIndex = 2;
            this.labelControl2.Text = "Machine name:";
            // 
            // txt_addressIP
            // 
            this.txt_addressIP.Location = new System.Drawing.Point(160, 42);
            this.txt_addressIP.Name = "txt_addressIP";
            this.txt_addressIP.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_addressIP.Properties.Appearance.Options.UseFont = true;
            this.txt_addressIP.Size = new System.Drawing.Size(171, 26);
            this.txt_addressIP.TabIndex = 1;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(19, 88);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(101, 19);
            this.labelControl1.TabIndex = 0;
            this.labelControl1.Text = "Device name: ";
            // 
            // ucRegisterDevice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControl1);
            this.Name = "ucRegisterDevice";
            this.Size = new System.Drawing.Size(563, 498);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            this.groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_plant.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_machineName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_deviceName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_addressIP.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.LookUpEdit cb_plant;
        private DevExpress.XtraEditors.LookUpEdit cb_department;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.TextEdit txt_machineName;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.TextEdit txt_deviceName;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.TextEdit txt_addressIP;
        private DevExpress.XtraEditors.LabelControl labelControl1;
    }
}
