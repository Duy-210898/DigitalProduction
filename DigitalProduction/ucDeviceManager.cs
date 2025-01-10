using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static DigitalProduction.frmMain;

namespace DigitalProduction
{
    public partial class ucDeviceManager : XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        private List<HmiConnectionStatus> currentDeviceStatuses = new List<HmiConnectionStatus>();


        public ucDeviceManager()
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
public class HmiConnectionStatus
{
    public string ipAddress { get; set; }
    public string machineName { get; set; }
    public string plant { get; set; }
    public bool isConnected { get; set; }
}

