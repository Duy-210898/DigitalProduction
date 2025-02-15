namespace DigitalProduction
{
    public partial class ucDeviceOutput : DevExpress.XtraEditors.XtraUserControl
    {
        private WebSocketClient _webSocketClient;
        public ucDeviceOutput() 
        {
            InitializeComponent();
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient;
        }
    }
}
