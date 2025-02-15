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
            this.gridControl_productionSchedule = new DevExpress.XtraGrid.GridControl();
            this.gridview_productionSchedule = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_productionSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridview_productionSchedule)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl_productionSchedule
            // 
            this.gridControl_productionSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl_productionSchedule.Location = new System.Drawing.Point(0, 0);
            this.gridControl_productionSchedule.MainView = this.gridview_productionSchedule;
            this.gridControl_productionSchedule.Name = "gridControl_productionSchedule";
            this.gridControl_productionSchedule.Size = new System.Drawing.Size(793, 736);
            this.gridControl_productionSchedule.TabIndex = 0;
            this.gridControl_productionSchedule.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridview_productionSchedule});
            // 
            // gridview_productionSchedule
            // 
            this.gridview_productionSchedule.GridControl = this.gridControl_productionSchedule;
            this.gridview_productionSchedule.GroupRowHeight = 2;
            this.gridview_productionSchedule.Name = "gridview_productionSchedule";
            this.gridview_productionSchedule.OptionsView.ShowFooter = true;
            // 
            // ucSchedule
            // 
            this.Appearance.BackColor = System.Drawing.Color.Green;
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl_productionSchedule);
            this.Name = "ucSchedule";
            this.Size = new System.Drawing.Size(793, 736);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_productionSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridview_productionSchedule)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl_productionSchedule;
        private DevExpress.XtraGrid.Views.Grid.GridView gridview_productionSchedule;
    }
}
