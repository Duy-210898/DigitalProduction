namespace DigitalProduction.Frm_Admin
{
    partial class frmDashboard
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmDashboard));
            DevExpress.XtraBars.Docking2010.WindowsUIButtonImageOptions windowsUIButtonImageOptions1 = new DevExpress.XtraBars.Docking2010.WindowsUIButtonImageOptions();
            DevExpress.XtraBars.Docking2010.WindowsUIButtonImageOptions windowsUIButtonImageOptions2 = new DevExpress.XtraBars.Docking2010.WindowsUIButtonImageOptions();
            this.navigationPage3 = new DevExpress.XtraBars.Navigation.NavigationPage();
            this.navigationPage2 = new DevExpress.XtraBars.Navigation.NavigationPage();
            this.tableLayoutMainDevice = new System.Windows.Forms.TableLayoutPanel();
            this.gridControlDeviceManagement = new DevExpress.XtraGrid.GridControl();
            this.gridViewDeviceManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.navigationPage1 = new DevExpress.XtraBars.Navigation.NavigationPage();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectDevice = new DevExpress.XtraEditors.LabelControl();
            this.dateTo = new DevExpress.XtraEditors.DateEdit();
            this.lblDateTo = new DevExpress.XtraEditors.LabelControl();
            this.lblDateFrom = new DevExpress.XtraEditors.LabelControl();
            this.dateFrom = new DevExpress.XtraEditors.DateEdit();
            this.lblInputSO = new DevExpress.XtraEditors.LabelControl();
            this.txtSO = new DevExpress.XtraEditors.TextEdit();
            this.btnFilter = new DevExpress.XtraEditors.SimpleButton();
            this.cbxDevice = new System.Windows.Forms.ComboBox();
            this.cbx_Status = new System.Windows.Forms.ComboBox();
            this.lblStatus = new DevExpress.XtraEditors.LabelControl();
            this.btnDeleteSelected = new DevExpress.XtraEditors.SimpleButton();
            this.gridDistribution = new DevExpress.XtraGrid.GridControl();
            this.gridViewDistribution = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.pic_Admin = new DevExpress.XtraEditors.PictureEdit();
            this.windowsUIButtonPanel1 = new DevExpress.XtraBars.Docking2010.WindowsUIButtonPanel();
            this.navigationPage4 = new DevExpress.XtraBars.Navigation.NavigationPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.navigationFrame1 = new DevExpress.XtraBars.Navigation.NavigationFrame();
            this.navigationPage2.SuspendLayout();
            this.tableLayoutMainDevice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlDeviceManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDeviceManagement)).BeginInit();
            this.navigationPage1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSO.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDistribution)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDistribution)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Admin.Properties)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.navigationFrame1)).BeginInit();
            this.navigationFrame1.SuspendLayout();
            this.SuspendLayout();
            // 
            // navigationPage3
            // 
            this.navigationPage3.Caption = "navigationPage3";
            this.navigationPage3.Name = "navigationPage3";
            this.navigationPage3.Size = new System.Drawing.Size(970, 647);
            // 
            // navigationPage2
            // 
            this.navigationPage2.Caption = "navigationPage2";
            this.navigationPage2.Controls.Add(this.tableLayoutMainDevice);
            this.navigationPage2.Name = "navigationPage2";
            this.navigationPage2.Size = new System.Drawing.Size(970, 647);
            // 
            // tableLayoutMainDevice
            // 
            this.tableLayoutMainDevice.ColumnCount = 1;
            this.tableLayoutMainDevice.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMainDevice.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutMainDevice.Controls.Add(this.gridControlDeviceManagement, 0, 1);
            this.tableLayoutMainDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutMainDevice.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutMainDevice.Name = "tableLayoutMainDevice";
            this.tableLayoutMainDevice.RowCount = 2;
            this.tableLayoutMainDevice.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.800618F));
            this.tableLayoutMainDevice.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 93.19938F));
            this.tableLayoutMainDevice.Size = new System.Drawing.Size(970, 647);
            this.tableLayoutMainDevice.TabIndex = 2;
            // 
            // gridControlDeviceManagement
            // 
            this.gridControlDeviceManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlDeviceManagement.Location = new System.Drawing.Point(3, 47);
            this.gridControlDeviceManagement.MainView = this.gridViewDeviceManagement;
            this.gridControlDeviceManagement.Name = "gridControlDeviceManagement";
            this.gridControlDeviceManagement.Size = new System.Drawing.Size(964, 597);
            this.gridControlDeviceManagement.TabIndex = 0;
            this.gridControlDeviceManagement.UseEmbeddedNavigator = true;
            this.gridControlDeviceManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewDeviceManagement});
            // 
            // gridViewDeviceManagement
            // 
            this.gridViewDeviceManagement.GridControl = this.gridControlDeviceManagement;
            this.gridViewDeviceManagement.Name = "gridViewDeviceManagement";
            // 
            // navigationPage1
            // 
            this.navigationPage1.Caption = "navigationPage1";
            this.navigationPage1.Controls.Add(this.tableLayoutPanel3);
            this.navigationPage1.Name = "navigationPage1";
            this.navigationPage1.Size = new System.Drawing.Size(970, 647);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.gridDistribution, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13.74502F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 86.25498F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(970, 647);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 7;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10.16598F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.31535F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.61826F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.21162F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10.47718F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20.43568F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.77593F));
            this.tableLayoutPanel4.Controls.Add(this.lblSelectDevice, 4, 0);
            this.tableLayoutPanel4.Controls.Add(this.dateTo, 3, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblDateTo, 2, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblDateFrom, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.dateFrom, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblInputSO, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.txtSO, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.btnFilter, 2, 1);
            this.tableLayoutPanel4.Controls.Add(this.cbxDevice, 5, 0);
            this.tableLayoutPanel4.Controls.Add(this.cbx_Status, 5, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblStatus, 4, 1);
            this.tableLayoutPanel4.Controls.Add(this.btnDeleteSelected, 6, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(964, 82);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // lblSelectDevice
            // 
            this.lblSelectDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectDevice.Location = new System.Drawing.Point(487, 3);
            this.lblSelectDevice.Name = "lblSelectDevice";
            this.lblSelectDevice.Size = new System.Drawing.Size(95, 35);
            this.lblSelectDevice.TabIndex = 8;
            this.lblSelectDevice.Text = "Select Device: ";
            // 
            // dateTo
            // 
            this.dateTo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dateTo.EditValue = null;
            this.dateTo.Location = new System.Drawing.Point(350, 18);
            this.dateTo.Name = "dateTo";
            this.dateTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateTo.Size = new System.Drawing.Size(131, 20);
            this.dateTo.TabIndex = 3;
            // 
            // lblDateTo
            // 
            this.lblDateTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDateTo.Location = new System.Drawing.Point(238, 3);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(106, 35);
            this.lblDateTo.TabIndex = 2;
            this.lblDateTo.Text = "To: ";
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDateFrom.Location = new System.Drawing.Point(3, 3);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(92, 35);
            this.lblDateFrom.TabIndex = 0;
            this.lblDateFrom.Text = "From: ";
            // 
            // dateFrom
            // 
            this.dateFrom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dateFrom.EditValue = null;
            this.dateFrom.Location = new System.Drawing.Point(101, 18);
            this.dateFrom.Name = "dateFrom";
            this.dateFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFrom.Size = new System.Drawing.Size(131, 20);
            this.dateFrom.TabIndex = 1;
            // 
            // lblInputSO
            // 
            this.lblInputSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputSO.Location = new System.Drawing.Point(3, 44);
            this.lblInputSO.Name = "lblInputSO";
            this.lblInputSO.Size = new System.Drawing.Size(92, 35);
            this.lblInputSO.TabIndex = 4;
            this.lblInputSO.Text = "SO Input:";
            // 
            // txtSO
            // 
            this.txtSO.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtSO.Location = new System.Drawing.Point(101, 59);
            this.txtSO.Name = "txtSO";
            this.txtSO.Size = new System.Drawing.Size(131, 20);
            this.txtSO.TabIndex = 5;
            // 
            // btnFilter
            // 
            this.btnFilter.Appearance.BackColor = System.Drawing.Color.SeaShell;
            this.btnFilter.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFilter.Appearance.ForeColor = System.Drawing.Color.Black;
            this.btnFilter.Appearance.Options.UseBackColor = true;
            this.btnFilter.Appearance.Options.UseFont = true;
            this.btnFilter.Appearance.Options.UseForeColor = true;
            this.btnFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFilter.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnFilter.ImageOptions.Image")));
            this.btnFilter.Location = new System.Drawing.Point(238, 44);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(106, 35);
            this.btnFilter.TabIndex = 6;
            this.btnFilter.Text = "Filter";
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
            // 
            // cbxDevice
            // 
            this.cbxDevice.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbxDevice.FormattingEnabled = true;
            this.cbxDevice.Location = new System.Drawing.Point(588, 17);
            this.cbxDevice.Name = "cbxDevice";
            this.cbxDevice.Size = new System.Drawing.Size(190, 21);
            this.cbxDevice.TabIndex = 7;
            // 
            // cbx_Status
            // 
            this.cbx_Status.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbx_Status.Items.AddRange(new object[] {
            "Pending",
            "Complete",
            "Stop"});
            this.cbx_Status.Location = new System.Drawing.Point(588, 58);
            this.cbx_Status.Name = "cbx_Status";
            this.cbx_Status.Size = new System.Drawing.Size(190, 21);
            this.cbx_Status.TabIndex = 9;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(487, 44);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(95, 35);
            this.lblStatus.TabIndex = 10;
            this.lblStatus.Text = "Status: ";
            // 
            // btnDeleteSelected
            // 
            this.btnDeleteSelected.Location = new System.Drawing.Point(784, 44);
            this.btnDeleteSelected.Name = "btnDeleteSelected";
            this.btnDeleteSelected.Size = new System.Drawing.Size(129, 35);
            this.btnDeleteSelected.TabIndex = 11;
            this.btnDeleteSelected.Text = "simpleButton1";
            // 
            // gridDistribution
            // 
            this.gridDistribution.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDistribution.Location = new System.Drawing.Point(3, 91);
            this.gridDistribution.MainView = this.gridViewDistribution;
            this.gridDistribution.Name = "gridDistribution";
            this.gridDistribution.Size = new System.Drawing.Size(964, 553);
            this.gridDistribution.TabIndex = 1;
            this.gridDistribution.UseEmbeddedNavigator = true;
            this.gridDistribution.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewDistribution});
            // 
            // gridViewDistribution
            // 
            this.gridViewDistribution.GridControl = this.gridDistribution;
            this.gridViewDistribution.Name = "gridViewDistribution";
            this.gridViewDistribution.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.True;
            this.gridViewDistribution.RowDeleted += new DevExpress.Data.RowDeletedEventHandler(this.gridViewDítribution_RowDeleted);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.pic_Admin, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.windowsUIButtonPanel1, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.07692F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 76.92308F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(183, 647);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // pic_Admin
            // 
            this.pic_Admin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pic_Admin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pic_Admin.EditValue = ((object)(resources.GetObject("pic_Admin.EditValue")));
            this.pic_Admin.Location = new System.Drawing.Point(3, 3);
            this.pic_Admin.Name = "pic_Admin";
            this.pic_Admin.Properties.Caption.Alignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.pic_Admin.Properties.Caption.Appearance.BackColor = System.Drawing.Color.White;
            this.pic_Admin.Properties.Caption.Appearance.BorderColor = System.Drawing.Color.White;
            this.pic_Admin.Properties.Caption.Appearance.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pic_Admin.Properties.Caption.Appearance.ForeColor = System.Drawing.Color.Blue;
            this.pic_Admin.Properties.Caption.Appearance.Options.UseBackColor = true;
            this.pic_Admin.Properties.Caption.Appearance.Options.UseBorderColor = true;
            this.pic_Admin.Properties.Caption.Appearance.Options.UseFont = true;
            this.pic_Admin.Properties.Caption.Appearance.Options.UseForeColor = true;
            this.pic_Admin.Properties.NullText = "Dashboard";
            this.pic_Admin.Properties.PictureAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.pic_Admin.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pic_Admin.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            this.pic_Admin.Size = new System.Drawing.Size(177, 143);
            this.pic_Admin.TabIndex = 1;
            // 
            // windowsUIButtonPanel1
            // 
            this.windowsUIButtonPanel1.AppearanceButton.Hovered.BackColor = System.Drawing.Color.White;
            this.windowsUIButtonPanel1.AppearanceButton.Hovered.Options.UseBackColor = true;
            this.windowsUIButtonPanel1.AppearanceButton.Pressed.BackColor = System.Drawing.Color.White;
            this.windowsUIButtonPanel1.AppearanceButton.Pressed.Options.UseBackColor = true;
            this.windowsUIButtonPanel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            windowsUIButtonImageOptions1.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("windowsUIButtonImageOptions1.SvgImage")));
            windowsUIButtonImageOptions2.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("windowsUIButtonImageOptions2.SvgImage")));
            this.windowsUIButtonPanel1.Buttons.AddRange(new DevExpress.XtraEditors.ButtonPanel.IBaseButton[] {
            new DevExpress.XtraBars.Docking2010.WindowsUIButton("Distribution Management", true, windowsUIButtonImageOptions1, DevExpress.XtraBars.Docking2010.ButtonStyle.PushButton, "", -1, true, null, true, false, true, "DistributionManagement", -1, false),
            new DevExpress.XtraBars.Docking2010.WindowsUIButton("Device Management", true, windowsUIButtonImageOptions2, DevExpress.XtraBars.Docking2010.ButtonStyle.PushButton, "", -1, true, null, true, false, true, "CuttingManagement", -1, false)});
            this.windowsUIButtonPanel1.ContentAlignment = System.Drawing.ContentAlignment.TopCenter;
            this.windowsUIButtonPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.windowsUIButtonPanel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.windowsUIButtonPanel1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.windowsUIButtonPanel1.Location = new System.Drawing.Point(3, 152);
            this.windowsUIButtonPanel1.Name = "windowsUIButtonPanel1";
            this.windowsUIButtonPanel1.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.windowsUIButtonPanel1.Padding = new System.Windows.Forms.Padding(20);
            this.windowsUIButtonPanel1.Size = new System.Drawing.Size(177, 492);
            this.windowsUIButtonPanel1.TabIndex = 0;
            this.windowsUIButtonPanel1.Tag = "";
            this.windowsUIButtonPanel1.Text = "windowsUIButtonPanel1";
            this.windowsUIButtonPanel1.ButtonClick += new DevExpress.XtraBars.Docking2010.ButtonEventHandler(this.WindowsUIButtonPanel1_ButtonClick);
            // 
            // navigationPage4
            // 
            this.navigationPage4.Caption = "navigationPage4";
            this.navigationPage4.Name = "navigationPage4";
            this.navigationPage4.Size = new System.Drawing.Size(144, 336);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.29268F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 83.70731F));
            this.tableLayoutPanel1.Controls.Add(this.navigationPage4);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.navigationFrame1, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1165, 653);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // navigationFrame1
            // 
            this.navigationFrame1.Controls.Add(this.navigationPage1);
            this.navigationFrame1.Controls.Add(this.navigationPage2);
            this.navigationFrame1.Controls.Add(this.navigationPage3);
            this.navigationFrame1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navigationFrame1.Location = new System.Drawing.Point(192, 3);
            this.navigationFrame1.Name = "navigationFrame1";
            this.navigationFrame1.Pages.AddRange(new DevExpress.XtraBars.Navigation.NavigationPageBase[] {
            this.navigationPage1,
            this.navigationPage2,
            this.navigationPage3});
            this.navigationFrame1.SelectedPage = this.navigationPage1;
            this.navigationFrame1.Size = new System.Drawing.Size(970, 647);
            this.navigationFrame1.TabIndex = 3;
            this.navigationFrame1.Text = "navigationFrame1";
            // 
            // frmDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1165, 653);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.navigationPage2.ResumeLayout(false);
            this.tableLayoutMainDevice.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlDeviceManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDeviceManagement)).EndInit();
            this.navigationPage1.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSO.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDistribution)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDistribution)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pic_Admin.Properties)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.navigationFrame1)).EndInit();
            this.navigationFrame1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage3;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage2;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private DevExpress.XtraEditors.LabelControl lblSelectDevice;
        private DevExpress.XtraEditors.DateEdit dateTo;
        private DevExpress.XtraEditors.LabelControl lblDateTo;
        private DevExpress.XtraEditors.LabelControl lblDateFrom;
        private DevExpress.XtraEditors.DateEdit dateFrom;
        private DevExpress.XtraEditors.LabelControl lblInputSO;
        private DevExpress.XtraEditors.TextEdit txtSO;
        private DevExpress.XtraEditors.SimpleButton btnFilter;
        private System.Windows.Forms.ComboBox cbxDevice;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevExpress.XtraEditors.PictureEdit pic_Admin;
        private DevExpress.XtraBars.Docking2010.WindowsUIButtonPanel windowsUIButtonPanel1;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private DevExpress.XtraBars.Navigation.NavigationFrame navigationFrame1;
        private DevExpress.XtraGrid.GridControl gridDistribution;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewDistribution;
        private System.Windows.Forms.ComboBox cbx_Status;
        private DevExpress.XtraEditors.LabelControl lblStatus;
        private System.Windows.Forms.TableLayoutPanel tableLayoutMainDevice;
        private DevExpress.XtraGrid.GridControl gridControlDeviceManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewDeviceManagement;
        private DevExpress.XtraEditors.SimpleButton btnDeleteSelected;
    }
}