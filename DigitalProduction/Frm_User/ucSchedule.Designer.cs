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
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.flayoutTableSelectSO = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.lb_PartName = new DevExpress.XtraEditors.LabelControl();
            this.comboxPartName = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblFilterDate = new DevExpress.XtraEditors.LabelControl();
            this.dateTimePickerSchedule = new System.Windows.Forms.DateTimePicker();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectSO = new DevExpress.XtraEditors.LabelControl();
            this.gridLookUpEditSO = new DevExpress.XtraEditors.GridLookUpEdit();
            this.gridLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lb_Size = new DevExpress.XtraEditors.LabelControl();
            this.comboSize = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.btnSync = new DevExpress.XtraEditors.SimpleButton();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.rdLeather = new System.Windows.Forms.RadioButton();
            this.rdRawMaterial = new System.Windows.Forms.RadioButton();
            this.lbSelectMaterialType = new DevExpress.XtraEditors.LabelControl();
            this.fpRequireSelectTypeMaterial = new DevExpress.Utils.FlyoutPanel();
            this.lblFlyoutMessage = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.tableMainLayout.SuspendLayout();
            this.flayoutTableSelectSO.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboxPartName.Properties)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditSO.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboSize.Properties)).BeginInit();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fpRequireSelectTypeMaterial)).BeginInit();
            this.fpRequireSelectTypeMaterial.SuspendLayout();
            this.SuspendLayout();
            // 
            // bottomPanel
            // 
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 521);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(975, 46);
            this.bottomPanel.TabIndex = 3;
            // 
            // gridControlSchedule
            // 
            this.gridControlSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlSchedule.Location = new System.Drawing.Point(3, 203);
            this.gridControlSchedule.MainView = this.gridViewSchedule;
            this.gridControlSchedule.Name = "gridControlSchedule";
            this.gridControlSchedule.Size = new System.Drawing.Size(969, 315);
            this.gridControlSchedule.TabIndex = 2;
            this.gridControlSchedule.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSchedule,
            this.gridView1});
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
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControlSchedule;
            this.gridView1.Name = "gridView1";
            // 
            // tableMainLayout
            // 
            this.tableMainLayout.ColumnCount = 1;
            this.tableMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableMainLayout.Controls.Add(this.gridControlSchedule, 0, 2);
            this.tableMainLayout.Controls.Add(this.flayoutTableSelectSO, 0, 1);
            this.tableMainLayout.Controls.Add(this.tableLayoutPanel7, 0, 0);
            this.tableMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableMainLayout.Location = new System.Drawing.Point(0, 0);
            this.tableMainLayout.Name = "tableMainLayout";
            this.tableMainLayout.RowCount = 3;
            this.tableMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableMainLayout.Size = new System.Drawing.Size(975, 521);
            this.tableMainLayout.TabIndex = 4;
            // 
            // flayoutTableSelectSO
            // 
            this.flayoutTableSelectSO.ColumnCount = 2;
            this.flayoutTableSelectSO.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.flayoutTableSelectSO.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.flayoutTableSelectSO.Controls.Add(this.tableLayoutPanel6, 1, 0);
            this.flayoutTableSelectSO.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.flayoutTableSelectSO.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.flayoutTableSelectSO.Controls.Add(this.tableLayoutPanel4, 1, 1);
            this.flayoutTableSelectSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flayoutTableSelectSO.Enabled = false;
            this.flayoutTableSelectSO.Location = new System.Drawing.Point(3, 53);
            this.flayoutTableSelectSO.Name = "flayoutTableSelectSO";
            this.flayoutTableSelectSO.RowCount = 2;
            this.flayoutTableSelectSO.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 54.62963F));
            this.flayoutTableSelectSO.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45.37037F));
            this.flayoutTableSelectSO.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.flayoutTableSelectSO.Size = new System.Drawing.Size(969, 144);
            this.flayoutTableSelectSO.TabIndex = 1;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.78237F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67.21763F));
            this.tableLayoutPanel6.Controls.Add(this.lb_PartName, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.comboxPartName, 0, 1);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(245, 3);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 2;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(721, 72);
            this.tableLayoutPanel6.TabIndex = 7;
            // 
            // lb_PartName
            // 
            this.lb_PartName.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lb_PartName.Appearance.Options.UseFont = true;
            this.lb_PartName.Location = new System.Drawing.Point(3, 3);
            this.lb_PartName.Name = "lb_PartName";
            this.lb_PartName.Size = new System.Drawing.Size(135, 14);
            this.lb_PartName.TabIndex = 1;
            this.lb_PartName.Text = "Please select Part Name:";
            // 
            // comboxPartName
            // 
            this.comboxPartName.Location = new System.Drawing.Point(3, 40);
            this.comboxPartName.Name = "comboxPartName";
            this.comboxPartName.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboxPartName.Size = new System.Drawing.Size(155, 20);
            this.comboxPartName.TabIndex = 0;
            this.comboxPartName.EditValueChanged += new System.EventHandler(this.comboPartName_EditValueChanged);
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
            this.tableLayoutPanel2.Size = new System.Drawing.Size(236, 72);
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
            this.dateTimePickerSchedule.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dateTimePickerSchedule.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerSchedule.Location = new System.Drawing.Point(3, 39);
            this.dateTimePickerSchedule.Name = "dateTimePickerSchedule";
            this.dateTimePickerSchedule.Size = new System.Drawing.Size(230, 29);
            this.dateTimePickerSchedule.TabIndex = 2;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.lblSelectSO, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.gridLookUpEditSO, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 81);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36.61972F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 63.38028F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(236, 60);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // lblSelectSO
            // 
            this.lblSelectSO.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectSO.Appearance.Options.UseFont = true;
            this.lblSelectSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectSO.Location = new System.Drawing.Point(3, 3);
            this.lblSelectSO.Name = "lblSelectSO";
            this.lblSelectSO.Size = new System.Drawing.Size(230, 15);
            this.lblSelectSO.TabIndex = 0;
            this.lblSelectSO.Text = "Please select SO:";
            // 
            // gridLookUpEditSO
            // 
            this.gridLookUpEditSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLookUpEditSO.Location = new System.Drawing.Point(3, 24);
            this.gridLookUpEditSO.Name = "gridLookUpEditSO";
            this.gridLookUpEditSO.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.gridLookUpEditSO.Properties.NullText = "";
            this.gridLookUpEditSO.Properties.PopupView = this.gridLookUpEdit1View;
            this.gridLookUpEditSO.Size = new System.Drawing.Size(230, 20);
            this.gridLookUpEditSO.TabIndex = 1;
            // 
            // gridLookUpEdit1View
            // 
            this.gridLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.gridLookUpEdit1View.Name = "gridLookUpEdit1View";
            this.gridLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gridLookUpEdit1View.OptionsView.ShowGroupPanel = false;
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
            this.tableLayoutPanel4.Location = new System.Drawing.Point(245, 81);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36.76471F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 63.23529F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(721, 60);
            this.tableLayoutPanel4.TabIndex = 6;
            // 
            // lb_Size
            // 
            this.lb_Size.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lb_Size.Appearance.Options.UseFont = true;
            this.lb_Size.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lb_Size.Location = new System.Drawing.Point(3, 3);
            this.lb_Size.Name = "lb_Size";
            this.lb_Size.Size = new System.Drawing.Size(229, 16);
            this.lb_Size.TabIndex = 6;
            this.lb_Size.Text = "Please select Size:";
            // 
            // comboSize
            // 
            this.comboSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.comboSize.Location = new System.Drawing.Point(3, 25);
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
            this.btnSync.Location = new System.Drawing.Point(238, 25);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(109, 32);
            this.btnSync.TabIndex = 5;
            this.btnSync.Text = "simpleButton1";
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 4;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel7.Controls.Add(this.rdLeather, 2, 0);
            this.tableLayoutPanel7.Controls.Add(this.rdRawMaterial, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.lbSelectMaterialType, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.fpRequireSelectTypeMaterial, 3, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(969, 44);
            this.tableLayoutPanel7.TabIndex = 3;
            // 
            // rdLeather
            // 
            this.rdLeather.AutoSize = true;
            this.rdLeather.BackColor = System.Drawing.Color.Transparent;
            this.rdLeather.Dock = System.Windows.Forms.DockStyle.Left;
            this.rdLeather.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rdLeather.Location = new System.Drawing.Point(323, 3);
            this.rdLeather.Name = "rdLeather";
            this.rdLeather.Size = new System.Drawing.Size(107, 38);
            this.rdLeather.TabIndex = 5;
            this.rdLeather.TabStop = true;
            this.rdLeather.Text = "radioButton1";
            this.rdLeather.UseVisualStyleBackColor = false;
            // 
            // rdRawMaterial
            // 
            this.rdRawMaterial.AutoSize = true;
            this.rdRawMaterial.BackColor = System.Drawing.Color.Transparent;
            this.rdRawMaterial.Dock = System.Windows.Forms.DockStyle.Left;
            this.rdRawMaterial.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rdRawMaterial.Location = new System.Drawing.Point(203, 3);
            this.rdRawMaterial.Name = "rdRawMaterial";
            this.rdRawMaterial.Size = new System.Drawing.Size(107, 38);
            this.rdRawMaterial.TabIndex = 4;
            this.rdRawMaterial.TabStop = true;
            this.rdRawMaterial.Text = "radioButton1";
            this.rdRawMaterial.UseVisualStyleBackColor = false;
            // 
            // lbSelectMaterialType
            // 
            this.lbSelectMaterialType.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.lbSelectMaterialType.Appearance.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbSelectMaterialType.Appearance.Options.UseBackColor = true;
            this.lbSelectMaterialType.Appearance.Options.UseFont = true;
            this.lbSelectMaterialType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSelectMaterialType.Location = new System.Drawing.Point(3, 3);
            this.lbSelectMaterialType.Name = "lbSelectMaterialType";
            this.lbSelectMaterialType.Size = new System.Drawing.Size(194, 38);
            this.lbSelectMaterialType.TabIndex = 2;
            this.lbSelectMaterialType.Text = "Please select material type:";
            // 
            // fpRequireSelectTypeMaterial
            // 
            this.fpRequireSelectTypeMaterial.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.fpRequireSelectTypeMaterial.Appearance.Options.UseBackColor = true;
            this.fpRequireSelectTypeMaterial.Controls.Add(this.lblFlyoutMessage);
            this.fpRequireSelectTypeMaterial.Location = new System.Drawing.Point(443, 3);
            this.fpRequireSelectTypeMaterial.Name = "fpRequireSelectTypeMaterial";
            this.fpRequireSelectTypeMaterial.OptionsBeakPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.fpRequireSelectTypeMaterial.Size = new System.Drawing.Size(461, 38);
            this.fpRequireSelectTypeMaterial.TabIndex = 6;
            // 
            // lblFlyoutMessage
            // 
            this.lblFlyoutMessage.Appearance.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFlyoutMessage.Appearance.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblFlyoutMessage.Appearance.Options.UseFont = true;
            this.lblFlyoutMessage.Appearance.Options.UseForeColor = true;
            this.lblFlyoutMessage.Location = new System.Drawing.Point(153, 11);
            this.lblFlyoutMessage.Name = "lblFlyoutMessage";
            this.lblFlyoutMessage.Size = new System.Drawing.Size(85, 16);
            this.lblFlyoutMessage.TabIndex = 0;
            this.lblFlyoutMessage.Text = "labelControl2";
            // 
            // ucSchedule
            // 
            this.Controls.Add(this.tableMainLayout);
            this.Controls.Add(this.bottomPanel);
            this.Name = "ucSchedule";
            this.Size = new System.Drawing.Size(975, 567);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.tableMainLayout.ResumeLayout(false);
            this.flayoutTableSelectSO.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboxPartName.Properties)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditSO.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboSize.Properties)).EndInit();
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fpRequireSelectTypeMaterial)).EndInit();
            this.fpRequireSelectTypeMaterial.ResumeLayout(false);
            this.fpRequireSelectTypeMaterial.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel bottomPanel;
        private DevExpress.XtraGrid.GridControl gridControlSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private System.Windows.Forms.TableLayoutPanel tableMainLayout;
        private System.Windows.Forms.TableLayoutPanel flayoutTableSelectSO;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private DevExpress.XtraEditors.LabelControl lb_PartName;
        private DevExpress.XtraEditors.CheckedComboBoxEdit comboxPartName;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevExpress.XtraEditors.LabelControl lblFilterDate;
        private System.Windows.Forms.DateTimePicker dateTimePickerSchedule;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DevExpress.XtraEditors.LabelControl lblSelectSO;
        private DevExpress.XtraEditors.GridLookUpEdit gridLookUpEditSO;
        private DevExpress.XtraGrid.Views.Grid.GridView gridLookUpEdit1View;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private DevExpress.XtraEditors.LabelControl lb_Size;
        private DevExpress.XtraEditors.CheckedComboBoxEdit comboSize;
        private DevExpress.XtraEditors.SimpleButton btnSync;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.RadioButton rdLeather;
        private System.Windows.Forms.RadioButton rdRawMaterial;
        private DevExpress.XtraEditors.LabelControl lbSelectMaterialType;
        private DevExpress.Utils.FlyoutPanel fpRequireSelectTypeMaterial;
        private DevExpress.XtraEditors.LabelControl lblFlyoutMessage;
    }
}
