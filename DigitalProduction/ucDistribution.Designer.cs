namespace DigitalProduction
{
    partial class ucDistribution
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
            DevExpress.XtraGrid.GridLevelNode gridLevelNode1 = new DevExpress.XtraGrid.GridLevelNode();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cbxDevice = new System.Windows.Forms.ComboBox();
            this.lblSelect = new System.Windows.Forms.Label();
            this.txtSO = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblWorkCenter = new System.Windows.Forms.Label();
            this.lblProductionDate = new System.Windows.Forms.Label();
            this.lblCreatedDate = new System.Windows.Forms.Label();
            this.lblFactory = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblLastNo = new System.Windows.Forms.Label();
            this.lblArt = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.lblPO = new System.Windows.Forms.Label();
            this.btnSend = new DevExpress.XtraEditors.SimpleButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.tbl = new System.Windows.Forms.TableLayoutPanel();
            this.lblSO = new System.Windows.Forms.Label();
            this.lblMasterWorkOrder = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnlMaterial = new System.Windows.Forms.Panel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSize = new System.Windows.Forms.Panel();
            this.gridControl_Size = new DevExpress.XtraGrid.GridControl();
            this.gridView_Size = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.cbxPart = new DevExpress.XtraEditors.LookUpEdit();
            this.gridLookUpEdit1 = new DevExpress.XtraEditors.GridLookUpEdit();
            this.gridLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.panel2.SuspendLayout();
            this.tbl.SuspendLayout();
            this.pnlMaterial.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.pnlSize.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_Size)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_Size)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cbxPart.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).BeginInit();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.cbxDevice);
            this.panel2.Controls.Add(this.lblSelect);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(695, 3);
            this.panel2.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(250, 160);
            this.panel2.TabIndex = 6;
            // 
            // cbxDevice
            // 
            this.cbxDevice.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbxDevice.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxDevice.FormattingEnabled = true;
            this.cbxDevice.Location = new System.Drawing.Point(0, 28);
            this.cbxDevice.Margin = new System.Windows.Forms.Padding(0, 3, 20, 3);
            this.cbxDevice.Name = "cbxDevice";
            this.cbxDevice.Size = new System.Drawing.Size(250, 26);
            this.cbxDevice.TabIndex = 5;
            // 
            // lblSelect
            // 
            this.lblSelect.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSelect.Location = new System.Drawing.Point(0, 0);
            this.lblSelect.Name = "lblSelect";
            this.lblSelect.Size = new System.Drawing.Size(250, 28);
            this.lblSelect.TabIndex = 0;
            this.lblSelect.Text = "Select Machine";
            this.lblSelect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSO
            // 
            this.txtSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSO.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSO.Location = new System.Drawing.Point(235, 10);
            this.txtSO.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
            this.txtSO.Name = "txtSO";
            this.txtSO.Size = new System.Drawing.Size(226, 23);
            this.txtSO.TabIndex = 0;
            this.txtSO.Leave += new System.EventHandler(this.txtSO_Leave);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(84, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(40, 10, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(145, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Input SO:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWorkCenter
            // 
            this.lblWorkCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWorkCenter.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWorkCenter.Location = new System.Drawing.Point(3, 45);
            this.lblWorkCenter.Name = "lblWorkCenter";
            this.lblWorkCenter.Size = new System.Drawing.Size(226, 23);
            this.lblWorkCenter.TabIndex = 2;
            this.lblWorkCenter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProductionDate
            // 
            this.lblProductionDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProductionDate.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProductionDate.Location = new System.Drawing.Point(235, 45);
            this.lblProductionDate.Name = "lblProductionDate";
            this.lblProductionDate.Size = new System.Drawing.Size(226, 23);
            this.lblProductionDate.TabIndex = 2;
            this.lblProductionDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCreatedDate
            // 
            this.lblCreatedDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCreatedDate.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCreatedDate.Location = new System.Drawing.Point(467, 45);
            this.lblCreatedDate.Name = "lblCreatedDate";
            this.lblCreatedDate.Size = new System.Drawing.Size(234, 23);
            this.lblCreatedDate.TabIndex = 2;
            this.lblCreatedDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFactory
            // 
            this.lblFactory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFactory.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFactory.Location = new System.Drawing.Point(707, 45);
            this.lblFactory.Name = "lblFactory";
            this.lblFactory.Size = new System.Drawing.Size(221, 23);
            this.lblFactory.TabIndex = 2;
            this.lblFactory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(707, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(221, 23);
            this.label6.TabIndex = 2;
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastNo
            // 
            this.lblLastNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastNo.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLastNo.Location = new System.Drawing.Point(467, 68);
            this.lblLastNo.Name = "lblLastNo";
            this.lblLastNo.Size = new System.Drawing.Size(234, 23);
            this.lblLastNo.TabIndex = 2;
            this.lblLastNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblArt
            // 
            this.lblArt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblArt.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblArt.Location = new System.Drawing.Point(235, 68);
            this.lblArt.Name = "lblArt";
            this.lblArt.Size = new System.Drawing.Size(226, 23);
            this.lblArt.TabIndex = 2;
            this.lblArt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblModel
            // 
            this.lblModel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModel.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblModel.Location = new System.Drawing.Point(3, 68);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(226, 23);
            this.lblModel.TabIndex = 2;
            this.lblModel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPO
            // 
            this.lblPO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPO.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPO.Location = new System.Drawing.Point(3, 91);
            this.lblPO.Name = "lblPO";
            this.lblPO.Size = new System.Drawing.Size(226, 26);
            this.lblPO.TabIndex = 2;
            this.lblPO.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSend
            // 
            this.btnSend.Appearance.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSend.Appearance.Options.UseFont = true;
            this.btnSend.Location = new System.Drawing.Point(742, 169);
            this.btnSend.LookAndFeel.SkinName = "Dark Side";
            this.btnSend.LookAndFeel.UseDefaultLookAndFeel = false;
            this.btnSend.Margin = new System.Windows.Forms.Padding(50, 3, 3, 3);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(170, 29);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "simpleButton1";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // panel3
            // 
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(3, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(686, 160);
            this.panel3.TabIndex = 7;
            // 
            // tbl
            // 
            this.tbl.ColumnCount = 2;
            this.tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 71.73489F));
            this.tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28.26511F));
            this.tbl.Controls.Add(this.panel2, 1, 0);
            this.tbl.Controls.Add(this.btnSend, 1, 1);
            this.tbl.Controls.Add(this.panel3, 0, 0);
            this.tbl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbl.Location = new System.Drawing.Point(3, 412);
            this.tbl.Name = "tbl";
            this.tbl.RowCount = 2;
            this.tbl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 51.9685F));
            this.tbl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48.0315F));
            this.tbl.Size = new System.Drawing.Size(965, 321);
            this.tbl.TabIndex = 3;
            // 
            // lblSO
            // 
            this.lblSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSO.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSO.Location = new System.Drawing.Point(235, 91);
            this.lblSO.Name = "lblSO";
            this.lblSO.Size = new System.Drawing.Size(226, 26);
            this.lblSO.TabIndex = 2;
            this.lblSO.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMasterWorkOrder
            // 
            this.lblMasterWorkOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMasterWorkOrder.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMasterWorkOrder.Location = new System.Drawing.Point(467, 91);
            this.lblMasterWorkOrder.Name = "lblMasterWorkOrder";
            this.lblMasterWorkOrder.Size = new System.Drawing.Size(234, 26);
            this.lblMasterWorkOrder.TabIndex = 2;
            this.lblMasterWorkOrder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(467, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(234, 28);
            this.panel1.TabIndex = 3;
            // 
            // pnlMaterial
            // 
            this.pnlMaterial.BackColor = System.Drawing.Color.IndianRed;
            this.pnlMaterial.Controls.Add(this.gridLookUpEdit1);
            this.pnlMaterial.Controls.Add(this.cbxPart);
            this.pnlMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMaterial.Location = new System.Drawing.Point(3, 126);
            this.pnlMaterial.Name = "pnlMaterial";
            this.pnlMaterial.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.pnlMaterial.Size = new System.Drawing.Size(965, 142);
            this.pnlMaterial.TabIndex = 2;
            this.pnlMaterial.Visible = false;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.88614F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24.27497F));
            this.tableLayoutPanel2.Controls.Add(this.txtSO, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblWorkCenter, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.lblProductionDate, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.lblCreatedDate, 2, 2);
            this.tableLayoutPanel2.Controls.Add(this.lblFactory, 3, 2);
            this.tableLayoutPanel2.Controls.Add(this.label6, 3, 3);
            this.tableLayoutPanel2.Controls.Add(this.lblLastNo, 2, 3);
            this.tableLayoutPanel2.Controls.Add(this.lblArt, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.lblModel, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.lblPO, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.lblSO, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.lblMasterWorkOrder, 2, 4);
            this.tableLayoutPanel2.Controls.Add(this.panel1, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(20, 3);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(20, 3, 20, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 29.65517F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.655172F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(931, 117);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.White;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.pnlSize, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbl, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.pnlMaterial, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27.4776F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 148F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 138F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 72.5224F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(971, 736);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // pnlSize
            // 
            this.pnlSize.BackColor = System.Drawing.Color.LightGray;
            this.pnlSize.Controls.Add(this.gridControl_Size);
            this.pnlSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSize.Location = new System.Drawing.Point(3, 274);
            this.pnlSize.Name = "pnlSize";
            this.pnlSize.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.pnlSize.Size = new System.Drawing.Size(965, 132);
            this.pnlSize.TabIndex = 1;
            this.pnlSize.Visible = false;
            // 
            // gridControl_Size
            // 
            this.gridControl_Size.Dock = System.Windows.Forms.DockStyle.Fill;
            gridLevelNode1.RelationName = "Level1";
            this.gridControl_Size.LevelTree.Nodes.AddRange(new DevExpress.XtraGrid.GridLevelNode[] {
            gridLevelNode1});
            this.gridControl_Size.Location = new System.Drawing.Point(20, 0);
            this.gridControl_Size.MainView = this.gridView_Size;
            this.gridControl_Size.Name = "gridControl_Size";
            this.gridControl_Size.Size = new System.Drawing.Size(925, 132);
            this.gridControl_Size.TabIndex = 0;
            this.gridControl_Size.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView_Size});
            // 
            // gridView_Size
            // 
            this.gridView_Size.GridControl = this.gridControl_Size;
            this.gridView_Size.GroupPanelText = " ";
            this.gridView_Size.Name = "gridView_Size";
            this.gridView_Size.OptionsFilter.AllowFilterEditor = false;
            this.gridView_Size.OptionsFind.AllowFindPanel = false;
            this.gridView_Size.OptionsPrint.EnableAppearanceEvenRow = true;
            this.gridView_Size.OptionsPrint.PrintFilterInfo = true;
            this.gridView_Size.OptionsPrint.PrintGroupFooter = false;
            this.gridView_Size.OptionsPrint.PrintSelectedRowsOnly = true;
            // 
            // cbxPart
            // 
            this.cbxPart.Location = new System.Drawing.Point(3, 3);
            this.cbxPart.Name = "cbxPart";
            this.cbxPart.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cbxPart.Size = new System.Drawing.Size(962, 20);
            this.cbxPart.TabIndex = 0;
            // 
            // gridLookUpEdit1
            // 
            this.gridLookUpEdit1.Location = new System.Drawing.Point(3, 55);
            this.gridLookUpEdit1.Name = "gridLookUpEdit1";
            this.gridLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.gridLookUpEdit1.Properties.PopupView = this.gridLookUpEdit1View;
            this.gridLookUpEdit1.Size = new System.Drawing.Size(945, 20);
            this.gridLookUpEdit1.TabIndex = 1;
            // 
            // gridLookUpEdit1View
            // 
            this.gridLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.gridLookUpEdit1View.Name = "gridLookUpEdit1View";
            this.gridLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gridLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // ucDistribution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucDistribution";
            this.Size = new System.Drawing.Size(971, 736);
            this.Load += new System.EventHandler(this.ucDistribution_Load);
            this.panel2.ResumeLayout(false);
            this.tbl.ResumeLayout(false);
            this.pnlMaterial.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.pnlSize.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl_Size)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView_Size)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cbxPart.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEdit1View)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ComboBox cbxDevice;
        private System.Windows.Forms.Label lblSelect;
        private System.Windows.Forms.TextBox txtSO;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblWorkCenter;
        private System.Windows.Forms.Label lblProductionDate;
        private System.Windows.Forms.Label lblCreatedDate;
        private System.Windows.Forms.Label lblFactory;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblLastNo;
        private System.Windows.Forms.Label lblArt;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.Label lblPO;
        private DevExpress.XtraEditors.SimpleButton btnSend;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TableLayoutPanel tbl;
        private System.Windows.Forms.Label lblSO;
        private System.Windows.Forms.Label lblMasterWorkOrder;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pnlMaterial;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel pnlSize;
        private DevExpress.XtraGrid.GridControl gridControl_Size;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_Size;
        private DevExpress.XtraEditors.LookUpEdit cbxPart;
        private DevExpress.XtraEditors.GridLookUpEdit gridLookUpEdit1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridLookUpEdit1View;
    }
}
