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
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class ucDeviceOutput : DevExpress.XtraEditors.XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        public ucDeviceOutput() 
        {
            InitializeComponent();
            LanguageSettings.LanguageChanged += OnLanguageChanged;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
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
