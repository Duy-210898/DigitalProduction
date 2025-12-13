namespace DigitalProduction
{
    partial class ucOperatorManagement
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
            this.components = new System.ComponentModel.Container();
            this.tablePanel2 = new DevExpress.Utils.Layout.TablePanel();
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.lblResult = new DevExpress.XtraEditors.LabelControl();
            this.stackPanel1 = new DevExpress.Utils.Layout.StackPanel();
            this.txtOperatorId = new DevExpress.XtraEditors.TextEdit();
            this.btnFind = new DevExpress.XtraEditors.SimpleButton();
            this.btnRegister = new DevExpress.XtraEditors.SimpleButton();
            this.btnSync = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.simpleSeparator1 = new DevExpress.XtraLayout.SimpleSeparator();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.gridControlOperatorManagement = new DevExpress.XtraGrid.GridControl();
            this.gridViewOperatorManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.toastNotificationsManager1 = new DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager(this.components);
            this.toastNotificationsManager2 = new DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager(this.components);
            this.toastNotificationsManager3 = new DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.tablePanel2)).BeginInit();
            this.tablePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.stackPanel1)).BeginInit();
            this.stackPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtOperatorId.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleSeparator1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlOperatorManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewOperatorManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager3)).BeginInit();
            this.SuspendLayout();
            // 
            // tablePanel2
            // 
            this.tablePanel2.Columns.AddRange(new DevExpress.Utils.Layout.TablePanelColumn[] {
            new DevExpress.Utils.Layout.TablePanelColumn(DevExpress.Utils.Layout.TablePanelEntityStyle.Relative, 55F)});
            this.tablePanel2.Controls.Add(this.layoutControl1);
            this.tablePanel2.Controls.Add(this.gridControlOperatorManagement);
            this.tablePanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanel2.Location = new System.Drawing.Point(0, 0);
            this.tablePanel2.Name = "tablePanel2";
            this.tablePanel2.Rows.AddRange(new DevExpress.Utils.Layout.TablePanelRow[] {
            new DevExpress.Utils.Layout.TablePanelRow(DevExpress.Utils.Layout.TablePanelEntityStyle.Absolute, 91F),
            new DevExpress.Utils.Layout.TablePanelRow(DevExpress.Utils.Layout.TablePanelEntityStyle.Absolute, 26F)});
            this.tablePanel2.Size = new System.Drawing.Size(793, 736);
            this.tablePanel2.TabIndex = 2;
            this.tablePanel2.UseSkinIndents = true;
            // 
            // layoutControl1
            // 
            this.tablePanel2.SetColumn(this.layoutControl1, 0);
            this.layoutControl1.Controls.Add(this.lblResult);
            this.layoutControl1.Controls.Add(this.stackPanel1);
            this.layoutControl1.Location = new System.Drawing.Point(13, 12);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.Root;
            this.tablePanel2.SetRow(this.layoutControl1, 0);
            this.layoutControl1.Size = new System.Drawing.Size(767, 87);
            this.layoutControl1.TabIndex = 1;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // lblResult
            // 
            this.lblResult.Location = new System.Drawing.Point(12, 61);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(63, 13);
            this.lblResult.StyleController = this.layoutControl1;
            this.lblResult.TabIndex = 5;
            this.lblResult.Text = "labelControl1";
            // 
            // stackPanel1
            // 
            this.stackPanel1.Controls.Add(this.txtOperatorId);
            this.stackPanel1.Controls.Add(this.btnFind);
            this.stackPanel1.Controls.Add(this.btnRegister);
            this.stackPanel1.Controls.Add(this.btnSync);
            this.stackPanel1.Location = new System.Drawing.Point(12, 12);
            this.stackPanel1.Name = "stackPanel1";
            this.stackPanel1.Size = new System.Drawing.Size(743, 45);
            this.stackPanel1.TabIndex = 4;
            this.stackPanel1.UseSkinIndents = true;
            // 
            // txtOperatorId
            // 
            this.txtOperatorId.Location = new System.Drawing.Point(13, 12);
            this.txtOperatorId.Name = "txtOperatorId";
            this.txtOperatorId.Size = new System.Drawing.Size(100, 20);
            this.txtOperatorId.TabIndex = 0;
            // 
            // btnFind
            // 
            this.btnFind.Location = new System.Drawing.Point(117, 10);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(123, 23);
            this.btnFind.TabIndex = 1;
            this.btnFind.Text = "simpleButton1";
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(244, 10);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(156, 23);
            this.btnRegister.TabIndex = 2;
            this.btnRegister.Text = "simpleButton2";
            // 
            // btnSync
            // 
            this.btnSync.Location = new System.Drawing.Point(404, 10);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(126, 23);
            this.btnSync.TabIndex = 3;
            this.btnSync.Text = "simpleButton3";
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.simpleSeparator1,
            this.layoutControlItem2});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(767, 87);
            this.Root.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.stackPanel1;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(747, 49);
            this.layoutControlItem1.TextVisible = false;
            // 
            // simpleSeparator1
            // 
            this.simpleSeparator1.Location = new System.Drawing.Point(0, 66);
            this.simpleSeparator1.Name = "simpleSeparator1";
            this.simpleSeparator1.Size = new System.Drawing.Size(747, 1);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.lblResult;
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 49);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(747, 17);
            this.layoutControlItem2.TextVisible = false;
            // 
            // gridControlOperatorManagement
            // 
            this.tablePanel2.SetColumn(this.gridControlOperatorManagement, 0);
            this.gridControlOperatorManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlOperatorManagement.Location = new System.Drawing.Point(13, 103);
            this.gridControlOperatorManagement.MainView = this.gridViewOperatorManagement;
            this.gridControlOperatorManagement.Name = "gridControlOperatorManagement";
            this.tablePanel2.SetRow(this.gridControlOperatorManagement, 1);
            this.gridControlOperatorManagement.Size = new System.Drawing.Size(767, 620);
            this.gridControlOperatorManagement.TabIndex = 0;
            this.gridControlOperatorManagement.UseEmbeddedNavigator = true;
            this.gridControlOperatorManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewOperatorManagement,
            this.gridView1});
            // 
            // gridViewOperatorManagement
            // 
            this.gridViewOperatorManagement.GridControl = this.gridControlOperatorManagement;
            this.gridViewOperatorManagement.Name = "gridViewOperatorManagement";
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControlOperatorManagement;
            this.gridView1.Name = "gridView1";
            // 
            // toastNotificationsManager1
            // 
            this.toastNotificationsManager1.ApplicationId = "b4238b7a-ff51-44c1-9b31-d2fb770e4896";
            // 
            // toastNotificationsManager2
            // 
            this.toastNotificationsManager2.ApplicationId = "b4238b7a-ff51-44c1-9b31-d2fb770e4896";
            // 
            // toastNotificationsManager3
            // 
            this.toastNotificationsManager3.ApplicationId = "b4238b7a-ff51-44c1-9b31-d2fb770e4896";
            // 
            // ucOperatorManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tablePanel2);
            this.Name = "ucOperatorManagement";
            this.Size = new System.Drawing.Size(793, 736);
            ((System.ComponentModel.ISupportInitialize)(this.tablePanel2)).EndInit();
            this.tablePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.stackPanel1)).EndInit();
            this.stackPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtOperatorId.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleSeparator1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlOperatorManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewOperatorManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toastNotificationsManager3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.Utils.Layout.TablePanel tablePanel2;
        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraGrid.GridControl gridControlOperatorManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewOperatorManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.Utils.Layout.StackPanel stackPanel1;
        private DevExpress.XtraEditors.TextEdit txtOperatorId;
        private DevExpress.XtraEditors.SimpleButton btnFind;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraEditors.SimpleButton btnRegister;
        private DevExpress.XtraEditors.SimpleButton btnSync;
        private DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager toastNotificationsManager1;
        private DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager toastNotificationsManager2;
        private DevExpress.XtraBars.ToastNotifications.ToastNotificationsManager toastNotificationsManager3;
        private DevExpress.XtraEditors.LabelControl lblResult;
        private DevExpress.XtraLayout.SimpleSeparator simpleSeparator1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
    }
}
