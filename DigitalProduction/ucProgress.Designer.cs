namespace DigitalProduction
{
    partial class ucProgress
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
            this.gridProgressManagement = new DevExpress.XtraGrid.GridControl();
            this.gridViewProgressManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.dtpStartDate = new DevExpress.XtraEditors.DateEdit();
            this.dtpEndDate = new DevExpress.XtraEditors.DateEdit();
            this.lblFilterDate = new DevExpress.XtraEditors.LabelControl();
            this.stackPanel1 = new DevExpress.Utils.Layout.StackPanel();
            this.cbxDevice = new System.Windows.Forms.ComboBox();
            this.btnApplyDevice = new DevExpress.XtraEditors.SimpleButton();
            this.syncButton = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.gridProgressManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewProgressManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtpStartDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpStartDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpEndDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpEndDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stackPanel1)).BeginInit();
            this.stackPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gridProgressManagement
            // 
            this.gridProgressManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProgressManagement.Location = new System.Drawing.Point(3, 56);
            this.gridProgressManagement.MainView = this.gridViewProgressManagement;
            this.gridProgressManagement.Name = "gridProgressManagement";
            this.gridProgressManagement.Size = new System.Drawing.Size(695, 466);
            this.gridProgressManagement.TabIndex = 0;
            this.gridProgressManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewProgressManagement,
            this.gridView1});
            // 
            // gridViewProgressManagement
            // 
            this.gridViewProgressManagement.GridControl = this.gridProgressManagement;
            this.gridViewProgressManagement.Name = "gridViewProgressManagement";
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridProgressManagement;
            this.gridView1.Name = "gridView1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.gridProgressManagement, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.24287F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.75713F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(701, 525);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34.67626F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65.32374F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.stackPanel1, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(695, 47);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46.47887F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 53.52113F));
            this.tableLayoutPanel3.Controls.Add(this.dtpStartDate, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.dtpEndDate, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.lblFilterDate, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 47.05882F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52.94118F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(235, 41);
            this.tableLayoutPanel3.TabIndex = 6;
            // 
            // dtpStartDate
            // 
            this.dtpStartDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpStartDate.EditValue = new System.DateTime(2025, 6, 14, 12, 50, 33, 282);
            this.dtpStartDate.Location = new System.Drawing.Point(3, 22);
            this.dtpStartDate.Name = "dtpStartDate";
            this.dtpStartDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtpStartDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtpStartDate.Properties.CalendarView = DevExpress.XtraEditors.Repository.CalendarView.Vista;
            this.dtpStartDate.Properties.VistaDisplayMode = DevExpress.Utils.DefaultBoolean.True;
            this.dtpStartDate.Size = new System.Drawing.Size(103, 20);
            this.dtpStartDate.TabIndex = 3;
            // 
            // dtpEndDate
            // 
            this.dtpEndDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpEndDate.EditValue = new System.DateTime(2025, 6, 14, 12, 50, 33, 282);
            this.dtpEndDate.Location = new System.Drawing.Point(112, 22);
            this.dtpEndDate.Name = "dtpEndDate";
            this.dtpEndDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtpEndDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dtpEndDate.Size = new System.Drawing.Size(120, 20);
            this.dtpEndDate.TabIndex = 0;
            // 
            // lblFilterDate
            // 
            this.lblFilterDate.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilterDate.Appearance.Options.UseFont = true;
            this.lblFilterDate.Location = new System.Drawing.Point(3, 3);
            this.lblFilterDate.Name = "lblFilterDate";
            this.lblFilterDate.Size = new System.Drawing.Size(59, 14);
            this.lblFilterDate.TabIndex = 2;
            this.lblFilterDate.Text = "Filter date:";
            // 
            // stackPanel1
            // 
            this.stackPanel1.Appearance.BackColor = System.Drawing.Color.Silver;
            this.stackPanel1.Appearance.Options.UseBackColor = true;
            this.stackPanel1.Controls.Add(this.cbxDevice);
            this.stackPanel1.Controls.Add(this.btnApplyDevice);
            this.stackPanel1.Controls.Add(this.syncButton);
            this.stackPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stackPanel1.Location = new System.Drawing.Point(244, 3);
            this.stackPanel1.Name = "stackPanel1";
            this.stackPanel1.Size = new System.Drawing.Size(448, 41);
            this.stackPanel1.TabIndex = 8;
            this.stackPanel1.UseSkinIndents = true;
            // 
            // cbxDevice
            // 
            this.cbxDevice.Dock = System.Windows.Forms.DockStyle.Left;
            this.cbxDevice.FormattingEnabled = true;
            this.cbxDevice.Location = new System.Drawing.Point(13, 10);
            this.cbxDevice.Name = "cbxDevice";
            this.cbxDevice.Size = new System.Drawing.Size(118, 21);
            this.cbxDevice.TabIndex = 4;
            // 
            // btnApplyDevice
            // 
            this.btnApplyDevice.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnApplyDevice.Location = new System.Drawing.Point(135, 9);
            this.btnApplyDevice.Name = "btnApplyDevice";
            this.btnApplyDevice.Size = new System.Drawing.Size(115, 23);
            this.btnApplyDevice.TabIndex = 8;
            this.btnApplyDevice.Text = "btnApplyDevice";
            // 
            // syncButton
            // 
            this.syncButton.Location = new System.Drawing.Point(254, 3);
            this.syncButton.Name = "syncButton";
            this.syncButton.Size = new System.Drawing.Size(124, 35);
            this.syncButton.TabIndex = 7;
            this.syncButton.Text = "syncButton";
            // 
            // ucProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucProgress";
            this.Size = new System.Drawing.Size(701, 525);
            ((System.ComponentModel.ISupportInitialize)(this.gridProgressManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewProgressManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtpStartDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpStartDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpEndDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtpEndDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stackPanel1)).EndInit();
            this.stackPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridProgressManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewProgressManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DevExpress.XtraEditors.DateEdit dtpStartDate;
        private DevExpress.XtraEditors.DateEdit dtpEndDate;
        private DevExpress.XtraEditors.LabelControl lblFilterDate;
        private System.Windows.Forms.ComboBox cbxDevice;
        private DevExpress.XtraEditors.SimpleButton syncButton;
        private DevExpress.Utils.Layout.StackPanel stackPanel1;
        private DevExpress.XtraEditors.SimpleButton btnApplyDevice;
    }
}
