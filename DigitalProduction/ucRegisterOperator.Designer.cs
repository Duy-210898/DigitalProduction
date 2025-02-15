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
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.btn_close = new DevExpress.XtraEditors.SimpleButton();
            this.btn_submit = new DevExpress.XtraEditors.SimpleButton();
            this.txt_employeeName = new DevExpress.XtraEditors.TextEdit();
            this.txt_employeeID = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl1
            // 
            this.groupControl1.CaptionLocation = DevExpress.Utils.Locations.Top;
            this.groupControl1.Controls.Add(this.btn_close);
            this.groupControl1.Controls.Add(this.btn_submit);
            this.groupControl1.Controls.Add(this.txt_employeeName);
            this.groupControl1.Controls.Add(this.txt_employeeID);
            this.groupControl1.Controls.Add(this.labelControl4);
            this.groupControl1.Controls.Add(this.labelControl3);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(426, 272);
            this.groupControl1.TabIndex = 1;
            this.groupControl1.Text = "Register Operator";
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
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(18, 45);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(97, 19);
            this.labelControl4.TabIndex = 6;
            this.labelControl4.Text = "Employee ID:";
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(18, 97);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(121, 19);
            this.labelControl3.TabIndex = 4;
            this.labelControl3.Text = "Employee Name:";
            // 
            // ucRegisterOperator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControl1);
            this.Name = "ucRegisterOperator";
            this.Size = new System.Drawing.Size(426, 272);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            this.groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_employeeID.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.SimpleButton btn_close;
        private DevExpress.XtraEditors.SimpleButton btn_submit;
        private DevExpress.XtraEditors.TextEdit txt_employeeName;
        private DevExpress.XtraEditors.TextEdit txt_employeeID;
    }
}
