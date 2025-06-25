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
            this.bottomPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.gridControlSchedule = new DevExpress.XtraGrid.GridControl();
            this.gridViewSchedule = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lb_Size = new DevExpress.XtraEditors.LabelControl();
            this.comboSize = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.btnSync = new DevExpress.XtraEditors.SimpleButton();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectSO = new DevExpress.XtraEditors.LabelControl();
            this.gridLookUpEditSO = new DevExpress.XtraEditors.GridLookUpEdit();
            this.gridLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblFilterDate = new DevExpress.XtraEditors.LabelControl();
            this.dateTimePickerSchedule = new System.Windows.Forms.DateTimePicker();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.lb_PartName = new DevExpress.XtraEditors.LabelControl();
            this.comboxPartName = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboSize.Properties)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditSO.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboxPartName.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // bottomPanel
            // 
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 536);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(975, 31);
            this.bottomPanel.TabIndex = 3;
            // 
            // gridControlSchedule
            // 
            this.gridControlSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlSchedule.Location = new System.Drawing.Point(3, 3);
            this.gridControlSchedule.MainView = this.gridViewSchedule;
            this.gridControlSchedule.Name = "gridControlSchedule";
            this.gridControlSchedule.Size = new System.Drawing.Size(969, 388);
            this.gridControlSchedule.TabIndex = 2;
            this.gridControlSchedule.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSchedule});
            // 
            // gridViewSchedule
            // 
            this.gridViewSchedule.Appearance.CustomizationFormHint.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gridViewSchedule.Appearance.CustomizationFormHint.Options.UseFont = true;
            this.gridViewSchedule.GridControl = this.gridControlSchedule;
            this.gridViewSchedule.Name = "gridViewSchedule";
            this.gridViewSchedule.OptionsPrint.EnableAppearanceOddRow = true;
            this.gridViewSchedule.OptionsPrint.PrintFilterInfo = true;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.gridControlSchedule, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 142);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(975, 394);
            this.tableLayoutPanel5.TabIndex = 4;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.64463F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67.35537F));
            this.tableLayoutPanel4.Controls.Add(this.lb_Size, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboSize, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.btnSync, 1, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(246, 68);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38.02817F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 61.97183F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(726, 71);
            this.tableLayoutPanel4.TabIndex = 6;
            // 
            // lb_Size
            // 
            this.lb_Size.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lb_Size.Appearance.Options.UseFont = true;
            this.lb_Size.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lb_Size.Location = new System.Drawing.Point(3, 3);
            this.lb_Size.Name = "lb_Size";
            this.lb_Size.Size = new System.Drawing.Size(231, 21);
            this.lb_Size.TabIndex = 6;
            this.lb_Size.Text = "Please select Size:";
            // 
            // comboSize
            // 
            this.comboSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.comboSize.Location = new System.Drawing.Point(3, 30);
            this.comboSize.Name = "comboSize";
            this.comboSize.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboSize.Size = new System.Drawing.Size(155, 20);
            this.comboSize.TabIndex = 0;
            this.comboSize.EditValueChanged += new System.EventHandler(this.comboSize_EditValueChanged);
            // 
            // btnSync
            // 
            this.btnSync.ImageOptions.Image = global::DigitalProduction.Properties.Resources.sync_icon;
            this.btnSync.Location = new System.Drawing.Point(240, 30);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(109, 38);
            this.btnSync.TabIndex = 5;
            this.btnSync.Text = "simpleButton1";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.lblSelectSO, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.gridLookUpEditSO, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 68);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36.61972F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 63.38028F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(237, 71);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // lblSelectSO
            // 
            this.lblSelectSO.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectSO.Appearance.Options.UseFont = true;
            this.lblSelectSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectSO.Location = new System.Drawing.Point(3, 3);
            this.lblSelectSO.Name = "lblSelectSO";
            this.lblSelectSO.Size = new System.Drawing.Size(231, 20);
            this.lblSelectSO.TabIndex = 0;
            this.lblSelectSO.Text = "Please select SO:";
            // 
            // gridLookUpEditSO
            // 
            this.gridLookUpEditSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLookUpEditSO.Location = new System.Drawing.Point(3, 29);
            this.gridLookUpEditSO.Name = "gridLookUpEditSO";
            this.gridLookUpEditSO.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.gridLookUpEditSO.Properties.NullText = "";
            this.gridLookUpEditSO.Properties.PopupView = this.gridLookUpEdit1View;
            this.gridLookUpEditSO.Size = new System.Drawing.Size(231, 20);
            this.gridLookUpEditSO.TabIndex = 1;
            // 
            // gridLookUpEdit1View
            // 
            this.gridLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.gridLookUpEdit1View.Name = "gridLookUpEdit1View";
            this.gridLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gridLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.lblFilterDate, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.dateTimePickerSchedule, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(237, 59);
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
            this.dateTimePickerSchedule.CustomFormat = "yyyy";
            this.dateTimePickerSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dateTimePickerSchedule.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerSchedule.Location = new System.Drawing.Point(3, 32);
            this.dateTimePickerSchedule.Name = "dateTimePickerSchedule";
            this.dateTimePickerSchedule.Size = new System.Drawing.Size(231, 20);
            this.dateTimePickerSchedule.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel6, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45.77465F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 54.22535F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(975, 142);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.78237F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67.21763F));
            this.tableLayoutPanel6.Controls.Add(this.lb_PartName, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.comboxPartName, 0, 1);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(246, 3);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 2;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(726, 59);
            this.tableLayoutPanel6.TabIndex = 7;
            // 
            // lb_PartName
            // 
            this.lb_PartName.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lb_PartName.Appearance.Options.UseFont = true;
            this.lb_PartName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lb_PartName.Location = new System.Drawing.Point(3, 3);
            this.lb_PartName.Name = "lb_PartName";
            this.lb_PartName.Size = new System.Drawing.Size(232, 24);
            this.lb_PartName.TabIndex = 1;
            this.lb_PartName.Text = "Please select Part Name:";
            // 
            // comboxPartName
            // 
            this.comboxPartName.Location = new System.Drawing.Point(3, 33);
            this.comboxPartName.Name = "comboxPartName";
            this.comboxPartName.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboxPartName.Size = new System.Drawing.Size(155, 20);
            this.comboxPartName.TabIndex = 0;
            this.comboxPartName.EditValueChanged += new System.EventHandler(this.comboPartName_EditValueChanged);
            // 
            // ucSchedule
            // 
            this.Controls.Add(this.tableLayoutPanel5);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucSchedule";
            this.Size = new System.Drawing.Size(975, 567);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboSize.Properties)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditSO.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboxPartName.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel bottomPanel;
        private DevExpress.XtraGrid.GridControl gridControlSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSchedule;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private DevExpress.XtraEditors.SimpleButton btnSync;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DevExpress.XtraEditors.GridLookUpEdit gridLookUpEditSO;
        private DevExpress.XtraGrid.Views.Grid.GridView gridLookUpEdit1View;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevExpress.XtraEditors.LabelControl lblFilterDate;
        private System.Windows.Forms.DateTimePicker dateTimePickerSchedule;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private DevExpress.XtraEditors.CheckedComboBoxEdit comboSize;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private DevExpress.XtraEditors.CheckedComboBoxEdit comboxPartName;
        private DevExpress.XtraEditors.LabelControl lb_Size;
        private DevExpress.XtraEditors.LabelControl lblSelectSO;
        private DevExpress.XtraEditors.LabelControl lb_PartName;
    }
}
