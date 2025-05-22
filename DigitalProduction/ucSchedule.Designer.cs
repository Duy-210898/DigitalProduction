namespace DigitalProduction
{
    partial class ucSchedule
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
            this.dateTimePickerSchedule = new System.Windows.Forms.DateTimePicker();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectSO = new DevExpress.XtraEditors.LabelControl();
            this.cboSO = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.btnSync = new DevExpress.XtraEditors.SimpleButton();
            this.bottomPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridControlSchedule = new DevExpress.XtraGrid.GridControl();
            this.gridView2 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridViewSchedule = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboSO.Properties)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
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
            this.tableLayoutPanel1.Size = new System.Drawing.Size(975, 123);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.lblFilterDate, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.dateTimePickerSchedule, 0, 1);
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
            // dateTimePickerSchedule
            // 
            this.dateTimePickerSchedule.CustomFormat = "MM/yyyy";
            this.dateTimePickerSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dateTimePickerSchedule.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerSchedule.Location = new System.Drawing.Point(3, 28);
            this.dateTimePickerSchedule.Name = "dateTimePickerSchedule";
            this.dateTimePickerSchedule.Size = new System.Drawing.Size(183, 20);
            this.dateTimePickerSchedule.TabIndex = 2;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.lblSelectSO, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.cboSO, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 64);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(237, 56);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // lblSelectSO
            // 
            this.lblSelectSO.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectSO.Appearance.Options.UseFont = true;
            this.lblSelectSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectSO.Location = new System.Drawing.Point(3, 3);
            this.lblSelectSO.Name = "lblSelectSO";
            this.lblSelectSO.Size = new System.Drawing.Size(231, 22);
            this.lblSelectSO.TabIndex = 0;
            this.lblSelectSO.Text = "Please select SO:";
            // 
            // cboSO
            // 
            this.cboSO.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSO.Location = new System.Drawing.Point(3, 31);
            this.cboSO.Name = "cboSO";
            this.cboSO.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboSO.Size = new System.Drawing.Size(231, 20);
            this.cboSO.TabIndex = 5;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.btnSync, 0, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(246, 64);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27.45098F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 72.54902F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(726, 56);
            this.tableLayoutPanel4.TabIndex = 6;
            // 
            // btnSync
            // 
            this.btnSync.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnSync.ImageOptions.Image = global::DigitalProduction.Properties.Resources.sync_icon;
            this.btnSync.Location = new System.Drawing.Point(3, 18);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(109, 35);
            this.btnSync.TabIndex = 5;
            this.btnSync.Text = "simpleButton1";
            // 
            // bottomPanel
            // 
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 536);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(975, 31);
            this.bottomPanel.TabIndex = 3;
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControlSchedule;
            this.gridView1.Name = "gridView1";
            // 
            // gridControlSchedule
            // 
            this.gridControlSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlSchedule.Location = new System.Drawing.Point(3, 3);
            this.gridControlSchedule.MainView = this.gridViewSchedule;
            this.gridControlSchedule.Name = "gridControlSchedule";
            this.gridControlSchedule.Size = new System.Drawing.Size(969, 407);
            this.gridControlSchedule.TabIndex = 2;
            this.gridControlSchedule.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSchedule});
            // 
            // gridView2
            // 
            this.gridView2.GridControl = this.gridControlSchedule;
            this.gridView2.Name = "gridView2";
            // 
            // gridViewSchedule
            // 
            this.gridViewSchedule.GridControl = this.gridControlSchedule;
            this.gridViewSchedule.Name = "gridViewSchedule";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.gridControlSchedule, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 123);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(975, 413);
            this.tableLayoutPanel5.TabIndex = 4;
            // 
            // ucSchedule
            // 
            this.Controls.Add(this.tableLayoutPanel5);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucSchedule";
            this.Size = new System.Drawing.Size(975, 567);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboSO.Properties)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevExpress.XtraEditors.LabelControl lblFilterDate;
        private System.Windows.Forms.DateTimePicker dateTimePickerSchedule;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DevExpress.XtraEditors.LabelControl lblSelectSO;
        private DevExpress.XtraEditors.CheckedComboBoxEdit cboSO;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private DevExpress.XtraEditors.SimpleButton btnSync;
        private System.Windows.Forms.FlowLayoutPanel bottomPanel;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.GridControl gridControlSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
    }
}
