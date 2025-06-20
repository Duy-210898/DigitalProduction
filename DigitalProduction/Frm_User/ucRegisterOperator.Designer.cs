namespace DigitalProduction
{
    partial class ucRegisterOperator
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
            this.groupControlRegisterOperator = new DevExpress.XtraEditors.GroupControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.txt_employeeName = new DevExpress.XtraEditors.TextEdit();
            this.txt_employeeID = new DevExpress.XtraEditors.TextEdit();
            this.lblEmployeeID = new DevExpress.XtraEditors.LabelControl();
            this.lblEmployeeName = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControlRegisterOperator)).BeginInit();
            this.groupControlRegisterOperator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControlRegisterOperator
            // 
            this.groupControlRegisterOperator.CaptionLocation = DevExpress.Utils.Locations.Top;
            this.groupControlRegisterOperator.Controls.Add(this.btn_close);
            this.groupControlRegisterOperator.Controls.Add(this.btn_submit);
            this.groupControlRegisterOperator.Controls.Add(this.txt_employeeName);
            this.groupControlRegisterOperator.Controls.Add(this.txt_employeeID);
            this.groupControlRegisterOperator.Controls.Add(this.lblEmployeeID);
            this.groupControlRegisterOperator.Controls.Add(this.lblEmployeeName);
            this.groupControlRegisterOperator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControlRegisterOperator.Location = new System.Drawing.Point(0, 0);
            this.groupControlRegisterOperator.Name = "groupControlRegisterOperator";
            this.groupControlRegisterOperator.Size = new System.Drawing.Size(426, 272);
            this.groupControlRegisterOperator.TabIndex = 1;
            this.groupControlRegisterOperator.Text = "Register Operator";
            // 
            // btn_close
            // 
            this.btn_close.Location = new System.Drawing.Point(242, 140);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(74, 23);
            this.btn_close.TabIndex = 13;
            this.btn_close.Text = "Cancel";
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // btn_submit
            // 
            this.btn_submit.Location = new System.Drawing.Point(145, 140);
            this.btn_submit.Name = "btn_submit";
            this.btn_submit.Size = new System.Drawing.Size(74, 23);
            this.btn_submit.TabIndex = 12;
            this.btn_submit.Text = "Submit";
            this.btn_submit.Click += new System.EventHandler(this.btn_submit_Click);
            // 
            // txt_employeeName
            // 
            this.txt_employeeName.Location = new System.Drawing.Point(145, 90);
            this.txt_employeeName.Name = "txt_employeeName";
            this.txt_employeeName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_employeeName.Properties.Appearance.Options.UseFont = true;
            this.txt_employeeName.Size = new System.Drawing.Size(171, 26);
            this.txt_employeeName.TabIndex = 11;
            // 
            // txt_employeeID
            // 
            this.txt_employeeID.Location = new System.Drawing.Point(145, 42);
            this.txt_employeeID.Name = "txt_employeeID";
            this.txt_employeeID.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.txt_employeeID.Properties.Appearance.Options.UseFont = true;
            this.txt_employeeID.Size = new System.Drawing.Size(171, 26);
            this.txt_employeeID.TabIndex = 10;
            // 
            // lblEmployeeID
            // 
            this.lblEmployeeID.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblEmployeeID.Appearance.Options.UseFont = true;
            this.lblEmployeeID.Location = new System.Drawing.Point(18, 45);
            this.lblEmployeeID.Name = "lblEmployeeID";
            this.lblEmployeeID.Size = new System.Drawing.Size(97, 19);
            this.lblEmployeeID.TabIndex = 6;
            this.lblEmployeeID.Text = "Employee ID:";
            // 
            // lblEmployeeName
            // 
            this.lblEmployeeName.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblEmployeeName.Appearance.Options.UseFont = true;
            this.lblEmployeeName.Location = new System.Drawing.Point(18, 97);
            this.lblEmployeeName.Name = "lblEmployeeName";
            this.lblEmployeeName.Size = new System.Drawing.Size(121, 19);
            this.lblEmployeeName.TabIndex = 4;
            this.lblEmployeeName.Text = "Employee Name:";
            // 
            // ucRegisterOperator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControlRegisterOperator);
            this.Name = "ucRegisterOperator";
            this.Size = new System.Drawing.Size(426, 272);
            ((System.ComponentModel.ISupportInitialize)(this.groupControlRegisterOperator)).EndInit();
            this.groupControlRegisterOperator.ResumeLayout(false);
            this.groupControlRegisterOperator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControlRegisterOperator;
        private DevExpress.XtraEditors.LabelControl lblEmployeeID;
        private DevExpress.XtraEditors.LabelControl lblEmployeeName;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.TextEdit txt_employeeName;
        private DevExpress.XtraEditors.TextEdit txt_employeeID;
    }
}
