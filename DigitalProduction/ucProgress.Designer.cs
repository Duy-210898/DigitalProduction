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
            this.gridControl_ProgressManagement = new DevExpress.XtraGrid.GridControl();
            this.gridView_ProgressManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_ProgressManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_ProgressManagement)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl_OperatorManagement
            // 
            this.gridControl_ProgressManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl_ProgressManagement.Location = new System.Drawing.Point(0, 0);
            this.gridControl_ProgressManagement.MainView = this.gridView_ProgressManagement;
            this.gridControl_ProgressManagement.Name = "gridControl_ProgressManagement";
            this.gridControl_ProgressManagement.Size = new System.Drawing.Size(793, 736);
            this.gridControl_ProgressManagement.TabIndex = 0;
            this.gridControl_ProgressManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView_ProgressManagement});
            // 
            // gridView_ProgressManagement
            // 
            this.gridView_ProgressManagement.GridControl = this.gridControl_ProgressManagement;
            this.gridView_ProgressManagement.GroupRowHeight = 2;
            this.gridView_ProgressManagement.Name = "gridView_ProgressManagement";
            this.gridView_ProgressManagement.OptionsView.ShowFooter = true;
            // 
            // ucOperatorManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl_ProgressManagement);
            this.Name = "ucProgress";
            this.Size = new System.Drawing.Size(793, 736);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_ProgressManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_ProgressManagement)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl_ProgressManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_ProgressManagement;
    }
}
