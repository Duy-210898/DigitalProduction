using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;

namespace DigitalProduction.Frm_User
{
    public partial class SubDistributionDetailForm : Form
    {
        private List<SubDistribution> _subList;

        public SubDistributionDetailForm(List<SubDistribution> subList)
        {
            InitializeComponent();
            _subList = subList ?? new List<SubDistribution>();
            Load += SubDistributionDetailForm_Load;
        }

        private void SubDistributionDetailForm_Load(object sender, EventArgs e)
        {
            GridControl grid = new GridControl
            {
                Dock = DockStyle.Fill,
                DataSource = _subList
            };

            GridView view = new GridView(grid);
            grid.MainView = view;
            grid.ViewCollection.Add(view);

            view.OptionsView.ShowGroupPanel = false;
            view.OptionsBehavior.Editable = false;

            Controls.Add(grid);
        }
    }
}
