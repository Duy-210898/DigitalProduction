namespace DigitalProduction
{
    partial class frmMain
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
            this.components = new System.ComponentModel.Container();
            this.repositoryItemProgressBar1 = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            this.repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.ribbonPage2 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.accordionControl1 = new DevExpress.XtraBars.Navigation.AccordionControl();
            this.accordionControlElement1 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.btnMonthlyPlan = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.accordionControlElement3 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.btnDistribution = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.btnDeviceOutput = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.accordionControlElement4 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.btnDeviceManager = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.btnUserManager = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.fluentDesignFormControl1 = new DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl();
            this.toggleLanguage = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.Language = new DevExpress.XtraBars.BarStaticItem();
            this.barSubItem1 = new DevExpress.XtraBars.BarSubItem();
            this.statusItem = new DevExpress.XtraBars.BarStaticItem();
            this.pnlControl = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accordionControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fluentDesignFormControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // repositoryItemProgressBar1
            // 
            this.repositoryItemProgressBar1.Name = "repositoryItemProgressBar1";
            // 
            // repositoryItemTextEdit1
            // 
            this.repositoryItemTextEdit1.AutoHeight = false;
            this.repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // ribbonPage2
            // 
            this.ribbonPage2.Name = "ribbonPage2";
            this.ribbonPage2.Text = "ribbonPage2";
            // 
            // accordionControl1
            // 
            this.accordionControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.accordionControl1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.accordionControlElement1,
            this.accordionControlElement3,
            this.accordionControlElement4});
            this.accordionControl1.Location = new System.Drawing.Point(0, 31);
            this.accordionControl1.LookAndFeel.SkinMaskColor = System.Drawing.Color.DeepSkyBlue;
            this.accordionControl1.LookAndFeel.SkinName = "Office 2019 Colorful";
            this.accordionControl1.LookAndFeel.UseDefaultLookAndFeel = false;
            this.accordionControl1.Name = "accordionControl1";
            this.accordionControl1.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Hidden;
            this.accordionControl1.Size = new System.Drawing.Size(229, 736);
            this.accordionControl1.TabIndex = 1;
            this.accordionControl1.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
            // 
            // accordionControlElement1
            // 
            this.accordionControlElement1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.btnMonthlyPlan});
            this.accordionControlElement1.Expanded = true;
            this.accordionControlElement1.Name = "accordionControlElement1";
            this.accordionControlElement1.Text = "Production Schedule";
            // 
            // btnMonthlyPlan
            // 
            this.btnMonthlyPlan.Name = "btnMonthlyPlan";
            this.btnMonthlyPlan.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.btnMonthlyPlan.Text = "Monthly Plan";
            this.btnMonthlyPlan.Click += new System.EventHandler(this.btnSchedule_Click);
            // 
            // accordionControlElement3
            // 
            this.accordionControlElement3.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.btnDistribution,
            this.btnDeviceOutput});
            this.accordionControlElement3.Expanded = true;
            this.accordionControlElement3.Name = "accordionControlElement3";
            this.accordionControlElement3.Text = "PO Distribution";
            // 
            // btnDistribution
            // 
            this.btnDistribution.Name = "btnDistribution";
            this.btnDistribution.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.btnDistribution.Text = "PO Distribution";
            this.btnDistribution.Click += new System.EventHandler(this.btnDistribution_Click);
            // 
            // btnDeviceOutput
            // 
            this.btnDeviceOutput.Name = "btnDeviceOutput";
            this.btnDeviceOutput.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.btnDeviceOutput.Text = "Device Output";
            this.btnDeviceOutput.Click += new System.EventHandler(this.btnDeviceOutput_Click);
            // 
            // accordionControlElement4
            // 
            this.accordionControlElement4.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.btnDeviceManager,
            this.btnUserManager});
            this.accordionControlElement4.Expanded = true;
            this.accordionControlElement4.Name = "accordionControlElement4";
            this.accordionControlElement4.Text = "System Management";
            // 
            // btnDeviceManager
            // 
            this.btnDeviceManager.Name = "btnDeviceManager";
            this.btnDeviceManager.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.btnDeviceManager.Text = "Device Management";
            this.btnDeviceManager.Click += new System.EventHandler(this.btnDeviceManager_Click);
            // 
            // btnUserManager
            // 
            this.btnUserManager.Name = "btnUserManager";
            this.btnUserManager.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.btnUserManager.Text = "User Management";
            this.btnUserManager.Click += new System.EventHandler(this.btnUserManager_Click);
            // 
            // fluentDesignFormControl1
            // 
            this.fluentDesignFormControl1.FluentDesignForm = this;
            this.fluentDesignFormControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.toggleLanguage,
            this.Language,
            this.barSubItem1,
            this.statusItem});
            this.fluentDesignFormControl1.Location = new System.Drawing.Point(0, 0);
            this.fluentDesignFormControl1.Name = "fluentDesignFormControl1";
            this.fluentDesignFormControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemTextEdit1,
            this.repositoryItemProgressBar1});
            this.fluentDesignFormControl1.Size = new System.Drawing.Size(1200, 31);
            this.fluentDesignFormControl1.TabIndex = 2;
            this.fluentDesignFormControl1.TabStop = false;
            this.fluentDesignFormControl1.TitleItemLinks.Add(this.toggleLanguage);
            this.fluentDesignFormControl1.TitleItemLinks.Add(this.Language);
            this.fluentDesignFormControl1.TitleItemLinks.Add(this.barSubItem1);
            this.fluentDesignFormControl1.TitleItemLinks.Add(this.statusItem);
            // 
            // toggleLanguage
            // 
            this.toggleLanguage.Id = 0;
            this.toggleLanguage.Name = "toggleLanguage";
            this.toggleLanguage.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.toggleLanguage_CheckedChanged);
            // 
            // Language
            // 
            this.Language.Caption = "barStaticItem1";
            this.Language.Id = 1;
            this.Language.Name = "Language";
            // 
            // barSubItem1
            // 
            this.barSubItem1.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
            this.barSubItem1.Caption = "barSubItem1";
            this.barSubItem1.Id = 2;
            this.barSubItem1.Name = "barSubItem1";
            // 
            // statusItem
            // 
            this.statusItem.Caption = "barStaticItem1";
            this.statusItem.Id = 2;
            this.statusItem.Name = "statusItem";
            // 
            // pnlControl
            // 
            this.pnlControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlControl.Location = new System.Drawing.Point(229, 31);
            this.pnlControl.Name = "pnlControl";
            this.pnlControl.Size = new System.Drawing.Size(971, 736);
            this.pnlControl.TabIndex = 3;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 767);
            this.Controls.Add(this.pnlControl);
            this.Controls.Add(this.accordionControl1);
            this.Controls.Add(this.fluentDesignFormControl1);
            this.FluentDesignFormControl = this.fluentDesignFormControl1;
            this.Name = "frmMain";
            this.NavigationControl = this.accordionControl1;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Main";
            this.Load += new System.EventHandler(this.frmMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accordionControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fluentDesignFormControl1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage2;
        private DevExpress.XtraBars.Navigation.AccordionControl accordionControl1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordionControlElement1;
        private DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl fluentDesignFormControl1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordionControlElement3;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordionControlElement4;
        private System.Windows.Forms.Panel pnlControl;
        private DevExpress.XtraBars.BarToggleSwitchItem toggleLanguage;
        private DevExpress.XtraBars.BarStaticItem Language;
        private DevExpress.XtraBars.BarSubItem barSubItem1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement btnMonthlyPlan;
        private DevExpress.XtraBars.Navigation.AccordionControlElement btnDistribution;
        private DevExpress.XtraBars.Navigation.AccordionControlElement btnDeviceOutput;
        private DevExpress.XtraBars.Navigation.AccordionControlElement btnDeviceManager;
        private DevExpress.XtraBars.Navigation.AccordionControlElement btnUserManager;
        private DevExpress.XtraBars.BarStaticItem statusItem;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private DevExpress.XtraEditors.Repository.RepositoryItemProgressBar repositoryItemProgressBar1;
    }
}

