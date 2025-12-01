namespace DigitalProduction.Frm_User
{
    partial class uc_DetailProgress
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
            this.gridControlDetailDistribution = new DevExpress.XtraGrid.GridControl();
            this.gridViewDetailDistribution = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlDetailDistribution)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDetailDistribution)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControlDetailDistribution
            // 
            this.gridControlDetailDistribution.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlDetailDistribution.Location = new System.Drawing.Point(0, 0);
            this.gridControlDetailDistribution.MainView = this.gridViewDetailDistribution;
            this.gridControlDetailDistribution.Name = "gridControlDetailDistribution";
            this.gridControlDetailDistribution.Size = new System.Drawing.Size(980, 609);
            this.gridControlDetailDistribution.TabIndex = 0;
            this.gridControlDetailDistribution.UseEmbeddedNavigator = true;
            this.gridControlDetailDistribution.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewDetailDistribution});
            // 
            // gridViewDetailDistribution
            // 
            this.gridViewDetailDistribution.GridControl = this.gridControlDetailDistribution;
            this.gridViewDetailDistribution.Name = "gridViewDetailDistribution";
            // 
            // uc_DetailProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControlDetailDistribution);
            this.Name = "uc_DetailProgress";
            this.Size = new System.Drawing.Size(980, 609);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlDetailDistribution)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewDetailDistribution)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControlDetailDistribution;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewDetailDistribution;
    }
}
