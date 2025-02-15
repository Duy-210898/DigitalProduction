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
            this.gridControl_OperatorManagement = new DevExpress.XtraGrid.GridControl();
            this.gridView_OperatorManagement = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_OperatorManagement)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_OperatorManagement)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl_OperatorManagement
            // 
            this.gridControl_OperatorManagement.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl_OperatorManagement.Location = new System.Drawing.Point(0, 0);
            this.gridControl_OperatorManagement.MainView = this.gridView_OperatorManagement;
            this.gridControl_OperatorManagement.Name = "gridControl_OperatorManagement";
            this.gridControl_OperatorManagement.Size = new System.Drawing.Size(793, 736);
            this.gridControl_OperatorManagement.TabIndex = 0;
            this.gridControl_OperatorManagement.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView_OperatorManagement});
            // 
            // gridView_OperatorManagement
            // 
            this.gridView_OperatorManagement.GridControl = this.gridControl_OperatorManagement;
            this.gridView_OperatorManagement.GroupRowHeight = 2;
            this.gridView_OperatorManagement.Name = "gridView_OperatorManagement";
            this.gridView_OperatorManagement.OptionsView.ShowFooter = true;
            // 
            // ucOperatorManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl_OperatorManagement);
            this.Name = "ucOperatorManagement";
            this.Size = new System.Drawing.Size(793, 736);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_OperatorManagement)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_OperatorManagement)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl_OperatorManagement;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_OperatorManagement;
    }
}
