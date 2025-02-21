namespace DigitalProduction
{
    partial class ucDeviceOutput
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
            this.dataGrid_DeviceOutput = new System.Windows.Forms.DataGridView();
            this.lblOrder = new System.Windows.Forms.Label();
            this.lblMasterWorkOrder = new System.Windows.Forms.Label();
            this.lblSO = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid_DeviceOutput)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGrid_DeviceOutput
            // 
            this.dataGrid_DeviceOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid_DeviceOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGrid_DeviceOutput.Location = new System.Drawing.Point(3, 25);
            this.dataGrid_DeviceOutput.Name = "dataGrid_DeviceOutput";
            this.dataGrid_DeviceOutput.Size = new System.Drawing.Size(787, 422);
            this.dataGrid_DeviceOutput.TabIndex = 0;
            // 
            // lblOrder
            // 
            this.lblOrder.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblOrder.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOrder.Location = new System.Drawing.Point(3, 0);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(190, 16);
            this.lblOrder.TabIndex = 3;
            this.lblOrder.Text = "1";
            this.lblOrder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMasterWorkOrder
            // 
            this.lblMasterWorkOrder.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblMasterWorkOrder.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMasterWorkOrder.Location = new System.Drawing.Point(199, 0);
            this.lblMasterWorkOrder.Name = "lblMasterWorkOrder";
            this.lblMasterWorkOrder.Size = new System.Drawing.Size(190, 16);
            this.lblMasterWorkOrder.TabIndex = 4;
            this.lblMasterWorkOrder.Text = "2";
            this.lblMasterWorkOrder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSO
            // 
            this.lblSO.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSO.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSO.Location = new System.Drawing.Point(395, 0);
            this.lblSO.Name = "lblSO";
            this.lblSO.Size = new System.Drawing.Size(190, 16);
            this.lblSO.TabIndex = 5;
            this.lblSO.Text = "3";
            this.lblSO.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblModel
            // 
            this.lblModel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblModel.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblModel.Location = new System.Drawing.Point(591, 0);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(193, 16);
            this.lblModel.TabIndex = 6;
            this.lblModel.Text = "4";
            this.lblModel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dataGrid_DeviceOutput, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(793, 450);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.lblOrder, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblModel, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblMasterWorkOrder, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblSO, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(787, 16);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // ucDeviceOutput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucDeviceOutput";
            this.Size = new System.Drawing.Size(793, 450);
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid_DeviceOutput)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGrid_DeviceOutput;
        private System.Windows.Forms.Label lblOrder;
        private System.Windows.Forms.Label lblMasterWorkOrder;
        private System.Windows.Forms.Label lblSO;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}
