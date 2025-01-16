namespace DigitalProduction
{
    partial class ucDeviceManager
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
            this.gridControl_Devices = new DevExpress.XtraGrid.GridControl();
            this.gridView_Device = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_Devices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_Device)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl_Devices
            // 
            this.gridControl_Devices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl_Devices.Location = new System.Drawing.Point(0, 0);
            this.gridControl_Devices.MainView = this.gridView_Device;
            this.gridControl_Devices.Name = "gridControl_Devices";
            this.gridControl_Devices.Size = new System.Drawing.Size(825, 491);
            this.gridControl_Devices.TabIndex = 0;
            this.gridControl_Devices.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView_Device});
            // 
            // gridView_Device
            // 
            this.gridView_Device.GridControl = this.gridControl_Devices;
            this.gridView_Device.Name = "gridView_Device";
            this.gridView_Device.OptionsCustomization.AllowFilter = false;
            this.gridView_Device.OptionsCustomization.AllowRowSizing = true;
            this.gridView_Device.OptionsPrint.PrintGroupFooter = false;
            this.gridView_Device.OptionsPrint.PrintHorzLines = false;
            this.gridView_Device.OptionsPrint.PrintVertLines = false;
            this.gridView_Device.OptionsView.RowAutoHeight = true;
            this.gridView_Device.OptionsView.ShowFooter = true;
            // 
            // ucDeviceManager
            // 
            this.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl_Devices);
            this.Name = "ucDeviceManager";
            this.Size = new System.Drawing.Size(825, 491);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_Devices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_Device)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl_Devices;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_Device;
    }
}
