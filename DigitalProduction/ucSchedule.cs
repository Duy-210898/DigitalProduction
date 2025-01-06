using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalProduction
{
    public partial class ucSchedule : DevExpress.XtraEditors.XtraUserControl
    {
        public ucSchedule()
        {
            InitializeComponent();
        }
        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }
        private void OnLanguageChanged()
        {
        }

    }
}
