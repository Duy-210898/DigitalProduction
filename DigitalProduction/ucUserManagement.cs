﻿using DevExpress.XtraEditors;
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
    public partial class ucUserManagement : DevExpress.XtraEditors.XtraUserControl
    {
        public ucUserManagement()
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
