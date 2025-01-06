using DigitalProduction;
using static DigitalProduction.frmMain;
using System.Net.WebSockets;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

public class WebSocketClient
{
    private ClientWebSocket _webSocket;
    private string _url;
    private bool _isReconnecting = false;
    private ResourceManager resourceManager;
    private TaskCompletionSource<string> ResponseCompletionSource;
    public event Action<string> OnErrorOccurred;

    public WebSocketClient()
    {
        resourceManager = new ResourceManager($"DigitalProduction.{LanguageSettings.CurrentLanguage}", typeof(WebSocketClient).Assembly);
        LanguageSettings.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        resourceManager = new ResourceManager($"DigitalProduction.{LanguageSettings.CurrentLanguage}", typeof(WebSocketClient).Assembly);
    }

    public async Task Connect(string url)
    {
        _url = url;
        _webSocket = new ClientWebSocket();

        await ReceiveMessageAsync();

        await _webSocket.ConnectAsync(new Uri(url), System.Threading.CancellationToken.None);
        Console.WriteLine(resourceManager.GetString("WebSocketOpened"));
        ConnectionManager.Instance.IsConnected = true;
        ConnectionManager.Instance.IsReconnecting = false;
        _isReconnecting = false;
    }

    private async Task ReceiveMessageAsync()
    {
        while (_webSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[1024];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received message: " + message);

                // Trigger the OnMessage event if the message is not null or empty
                if (!string.IsNullOrEmpty(message))
                {
                    OnMessage?.Invoke(message);
                    // Set the response if waiting for it
                    ResponseCompletionSource?.TrySetResult(message);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("WebSocket connection closed.");
                break;
            }
        }
    }

    public event Action<string> OnMessage;

    private async Task Reconnect()
    {
        int retryCount = 0;
        int maxRetries = 5;

        while (retryCount < maxRetries && !ConnectionManager.Instance.IsConnected)
        {
            try
            {
                Console.WriteLine(resourceManager.GetString("AttemptingReconnect"));
                await _webSocket.ConnectAsync(new Uri(_url), System.Threading.CancellationToken.None);

                if (ConnectionManager.Instance.IsConnected)
                {
                    Console.WriteLine(resourceManager.GetString("WebSocketReconnected"));
                    ConnectionManager.Instance.IsReconnecting = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(resourceManager.GetString("ReconnectFailed") + ": " + ex.Message);
            }

            retryCount++;
            await Task.Delay(5000);
        }

        var result = MessageBox.Show(resourceManager.GetString("ReconnectionFailedMessage"), resourceManager.GetString("ReconnectionFailedTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Error);

        if (result == DialogResult.Yes)
        {
            await Reconnect();
        }
        else
        {
            _isReconnecting = false;
            ConnectionManager.Instance.IsReconnecting = false;
        }
    }

    public async Task<string> SendAsync(string message)
    {
        try
        {
            if (ConnectionManager.Instance.IsConnected)
            {
                ResponseCompletionSource = new TaskCompletionSource<string>();

                await _webSocket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);

                return await ResponseCompletionSource.Task;
            }
            else
            {
                OnErrorOccurred?.Invoke("WebSocket chưa kết nối.");
                return null;
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred?.Invoke($"Lỗi khi gửi tin nhắn: {ex.Message}");
            return null;
        }
    }

    public async Task Disconnect()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", System.Threading.CancellationToken.None);
        }
    }
}
