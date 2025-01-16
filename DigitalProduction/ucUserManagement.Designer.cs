namespace DigitalProduction
{
    partial class ucUserManagement
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
            this.gridControl_UserManagement = new DevExpress.XtraGrid.GridControl();
            this.gridView_UserManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_UserManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_UserManagement)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl_UserManagement
            // 
            this.gridControl_UserManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl_UserManagement.Location = new System.Drawing.Point(0, 0);
            this.gridControl_UserManagement.MainView = this.gridView_UserManagement;
            this.gridControl_UserManagement.Name = "gridControl_UserManagement";
            this.gridControl_UserManagement.Size = new System.Drawing.Size(793, 736);
            this.gridControl_UserManagement.TabIndex = 0;
            this.gridControl_UserManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView_UserManagement});
            // 
            // gridView_UserManagement
            // 
            this.gridView_UserManagement.GridControl = this.gridControl_UserManagement;
            this.gridView_UserManagement.Name = "gridView_UserManagement";
            this.gridView_UserManagement.OptionsView.ShowFooter = true;
            // 
            // ucUserManagement
            // 
            this.Appearance.BackColor = System.Drawing.Color.Green;
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl_UserManagement);
            this.Name = "ucUserManagement";
            this.Size = new System.Drawing.Size(793, 736);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_UserManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_UserManagement)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl_UserManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_UserManagement;
    }
}
