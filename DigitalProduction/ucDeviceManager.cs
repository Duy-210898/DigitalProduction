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

        public class HmiConnectionStatus
        {
            public string ipAddress { get; set; }
            public string machineName { get; set; }
            public string plant { get; set; }
            public bool isConnected { get; set; }
        }

        public ucDeviceManager()
        {
            InitializeComponent();
            LanguageSettings.LanguageChanged += OnLanguageChanged;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
        }

        private async void sendButton_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            await _webSocketClient.SendAsync(message);
        }



        public void RefreshLanguage()
        {
            OnLanguageChanged();
        }

        private void OnLanguageChanged()
        {
            // Cập nhật ngôn ngữ khi có sự thay đổi
        }
    }
}
