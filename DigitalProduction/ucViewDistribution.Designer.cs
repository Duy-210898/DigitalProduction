namespace DigitalProduction
{
    partial class ucViewDistribution
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblFilterDate = new DevExpress.XtraEditors.LabelControl();
            this.dateTimePickerViewSO = new System.Windows.Forms.DateTimePicker();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectSO = new DevExpress.XtraEditors.LabelControl();
            this.cboSO = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.gridViewSO = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridControlViewSO = new DevExpress.XtraGrid.GridControl();
            this.btnSync = new DevExpress.XtraEditors.SimpleButton();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboSO.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSO)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlViewSO)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(781, 113);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.lblFilterDate, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.dateTimePickerViewSO, 0, 1);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(189, 50);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // lblFilterDate
            // 
            this.lblFilterDate.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilterDate.Appearance.Options.UseFont = true;
            this.lblFilterDate.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblFilterDate.Location = new System.Drawing.Point(3, 3);
            this.lblFilterDate.Name = "lblFilterDate";
            this.lblFilterDate.Size = new System.Drawing.Size(59, 14);
            this.lblFilterDate.TabIndex = 1;
            this.lblFilterDate.Text = "Filter date:";
            // 
            // dateTimePickerViewSO
            // 
            this.dateTimePickerViewSO.CustomFormat = "MM/yyyy";
            this.dateTimePickerViewSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dateTimePickerViewSO.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerViewSO.Location = new System.Drawing.Point(3, 28);
            this.dateTimePickerViewSO.Name = "dateTimePickerViewSO";
            this.dateTimePickerViewSO.Size = new System.Drawing.Size(183, 20);
            this.dateTimePickerViewSO.TabIndex = 2;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.lblSelectSO, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.cboSO, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 59);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(189, 51);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // lblSelectSO
            // 
            this.lblSelectSO.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectSO.Appearance.Options.UseFont = true;
            this.lblSelectSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectSO.Location = new System.Drawing.Point(3, 3);
            this.lblSelectSO.Name = "lblSelectSO";
            this.lblSelectSO.Size = new System.Drawing.Size(183, 19);
            this.lblSelectSO.TabIndex = 0;
            this.lblSelectSO.Text = "Please select SO:";
            // 
            // cboSO
            // 
            this.cboSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboSO.Location = new System.Drawing.Point(3, 28);
            this.cboSO.Name = "cboSO";
            this.cboSO.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboSO.Size = new System.Drawing.Size(183, 20);
            this.cboSO.TabIndex = 5;
            // 
            // gridViewSO
            // 
            this.gridViewSO.GridControl = this.gridControlViewSO;
            this.gridViewSO.Name = "gridViewSO";
            // 
            // gridControlViewSO
            // 
            this.gridControlViewSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlViewSO.Location = new System.Drawing.Point(0, 113);
            this.gridControlViewSO.MainView = this.gridViewSO;
            this.gridControlViewSO.Name = "gridControlViewSO";
            this.gridControlViewSO.Size = new System.Drawing.Size(781, 454);
            this.gridControlViewSO.TabIndex = 1;
            this.gridControlViewSO.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSO});
            // 
            // btnSync
            // 
            this.btnSync.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnSync.ImageOptions.Image = global::DigitalProduction.Properties.Resources.sync_icon;
            this.btnSync.Location = new System.Drawing.Point(3, 17);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(109, 31);
            this.btnSync.TabIndex = 5;
            this.btnSync.Text = "simpleButton1";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.btnSync, 0, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(198, 59);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27.45098F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 72.54902F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(580, 51);
            this.tableLayoutPanel4.TabIndex = 6;
            // 
            // ucViewDistribution
            // 
            this.Controls.Add(this.gridControlViewSO);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucViewDistribution";
            this.Size = new System.Drawing.Size(781, 567);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboSO.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSO)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlViewSO)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private DevExpress.XtraEditors.LabelControl lblSelectSO;
        private System.Windows.Forms.DateTimePicker dateTimePickerViewSO;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevExpress.XtraEditors.LabelControl lblFilterDate;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DevExpress.XtraEditors.CheckedComboBoxEdit cboSO;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSO;
        private DevExpress.XtraGrid.GridControl gridControlViewSO;
        private DevExpress.XtraEditors.SimpleButton btnSync;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
    }
}
