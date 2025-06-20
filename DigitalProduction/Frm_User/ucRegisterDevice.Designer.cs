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
            this.groupControl_RegisterDevice = new DevExpress.XtraEditors.GroupControl();
            this.cb_plant = new DevExpress.XtraEditors.LookUpEdit();
            this.cb_department = new DevExpress.XtraEditors.LookUpEdit();
            this.lblPlant = new DevExpress.XtraEditors.LabelControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.lblDeparment = new DevExpress.XtraEditors.LabelControl();
            this.txt_machineName = new DevExpress.XtraEditors.TextEdit();
            this.lblAddressID = new DevExpress.XtraEditors.LabelControl();
            this.lblMachineName = new DevExpress.XtraEditors.LabelControl();
            this.txt_addressIP = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl_RegisterDevice)).BeginInit();
            this.groupControl_RegisterDevice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_plant.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_machineName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_addressIP.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl_RegisterDevice
            // 
            this.groupControl_RegisterDevice.Controls.Add(this.cb_plant);
            this.groupControl_RegisterDevice.Controls.Add(this.cb_department);
            this.groupControl_RegisterDevice.Controls.Add(this.lblPlant);
            this.groupControl_RegisterDevice.Controls.Add(this.btn_close);
            this.groupControl_RegisterDevice.Controls.Add(this.btn_submit);
            this.groupControl_RegisterDevice.Controls.Add(this.lblDeparment);
            this.groupControl_RegisterDevice.Controls.Add(this.txt_machineName);
            this.groupControl_RegisterDevice.Controls.Add(this.lblAddressID);
            this.groupControl_RegisterDevice.Controls.Add(this.lblMachineName);
            this.groupControl_RegisterDevice.Controls.Add(this.txt_addressIP);
            this.groupControl_RegisterDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl_RegisterDevice.Location = new System.Drawing.Point(0, 0);
            this.groupControl_RegisterDevice.Name = "groupControl_RegisterDevice";
            this.groupControl_RegisterDevice.Size = new System.Drawing.Size(563, 498);
            this.groupControl_RegisterDevice.TabIndex = 1;
            this.groupControl_RegisterDevice.Text = "Add new device";
            // 
            // cb_plant
            // 
            this.cb_plant.Location = new System.Drawing.Point(160, 172);
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
            this.cb_department.Location = new System.Drawing.Point(160, 127);
            this.cb_department.Name = "cb_department";
            this.cb_department.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 11F);
            this.cb_department.Properties.Appearance.Options.UseFont = true;
            this.cb_department.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cb_department.Properties.NullText = "Select Department";
            this.cb_department.Size = new System.Drawing.Size(171, 24);
            this.cb_department.TabIndex = 5;
            // 
            // lblPlant
            // 
            this.lblPlant.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblPlant.Appearance.Options.UseFont = true;
            this.lblPlant.Location = new System.Drawing.Point(19, 174);
            this.lblPlant.Name = "lblPlant";
            this.lblPlant.Size = new System.Drawing.Size(41, 19);
            this.lblPlant.TabIndex = 12;
            this.lblPlant.Text = "Plant:";
            // 
            // btn_close
            // 
            this.btn_close.Location = new System.Drawing.Point(256, 222);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(75, 23);
            this.btn_close.TabIndex = 8;
            this.btn_close.Text = "Cancel";
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // btn_submit
            // 
            this.btn_submit.Location = new System.Drawing.Point(160, 222);
            this.btn_submit.Name = "btn_submit";
            this.btn_submit.Size = new System.Drawing.Size(75, 23);
            this.btn_submit.TabIndex = 7;
            this.btn_submit.Text = "Submit";
            this.btn_submit.Click += new System.EventHandler(this.btn_submit_Click);
            // 
            // lblDeparment
            // 
            this.lblDeparment.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblDeparment.Appearance.Options.UseFont = true;
            this.lblDeparment.Location = new System.Drawing.Point(19, 129);
            this.lblDeparment.Name = "lblDeparment";
            this.lblDeparment.Size = new System.Drawing.Size(89, 19);
            this.lblDeparment.TabIndex = 8;
            this.lblDeparment.Text = "Department:";
            // 
            // txt_machineName
            // 
            this.txt_machineName.Location = new System.Drawing.Point(160, 84);
            this.txt_machineName.Name = "txt_machineName";
            this.txt_machineName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_machineName.Properties.Appearance.Options.UseFont = true;
            this.txt_machineName.Size = new System.Drawing.Size(171, 26);
            this.txt_machineName.TabIndex = 3;
            // 
            // lblAddressID
            // 
            this.lblAddressID.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblAddressID.Appearance.Options.UseFont = true;
            this.lblAddressID.Location = new System.Drawing.Point(19, 45);
            this.lblAddressID.Name = "lblAddressID";
            this.lblAddressID.Size = new System.Drawing.Size(83, 19);
            this.lblAddressID.TabIndex = 6;
            this.lblAddressID.Text = "Address IP:";
            // 
            // lblMachineName
            // 
            this.lblMachineName.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblMachineName.Appearance.Options.UseFont = true;
            this.lblMachineName.Location = new System.Drawing.Point(19, 87);
            this.lblMachineName.Name = "lblMachineName";
            this.lblMachineName.Size = new System.Drawing.Size(107, 19);
            this.lblMachineName.TabIndex = 2;
            this.lblMachineName.Text = "Machine name:";
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
            // ucRegisterDevice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControl_RegisterDevice);
            this.Name = "ucRegisterDevice";
            this.Size = new System.Drawing.Size(563, 498);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl_RegisterDevice)).EndInit();
            this.groupControl_RegisterDevice.ResumeLayout(false);
            this.groupControl_RegisterDevice.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cb_plant.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cb_department.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_machineName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_addressIP.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl_RegisterDevice;
        private DevExpress.XtraEditors.LookUpEdit cb_plant;
        private DevExpress.XtraEditors.LookUpEdit cb_department;
        private DevExpress.XtraEditors.LabelControl lblPlant;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.LabelControl lblDeparment;
        private DevExpress.XtraEditors.TextEdit txt_machineName;
        private DevExpress.XtraEditors.LabelControl lblAddressID;
        private DevExpress.XtraEditors.LabelControl lblMachineName;
        private DevExpress.XtraEditors.TextEdit txt_addressIP;
    }
}
