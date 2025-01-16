using DigitalProduction;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System;

public class WebSocketClient : IDisposable
{
    private ClientWebSocket _webSocket;

    // Accessor to the singleton instance
    private static WebSocketClient _instance;

    private TaskCompletionSource<string> ResponseCompletionSource;
    private string _url;
    private bool _isReconnecting = false;
    private bool _disposed = false;
    private static object _lock = new object();

    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private const int ReconnectDelaySeconds = 5;

    public event Action<string> OnErrorOccurred;
    public event Action OnDisconnected;
    public event Action<string> OnResponseReceived;

    // Private constructor to prevent external instantiation
    private WebSocketClient() { }

    public static WebSocketClient Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new WebSocketClient();
                }
                return _instance;
            }
        }
    }
    // Connect to the WebSocket server
    public async Task Connect(string url)
    {
        lock (_lock)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("WebSocket is already connected.");
                return;
            }
        }

        try
        {
            _url = url;
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(url), System.Threading.CancellationToken.None);

            ConnectionManager.Instance.IsConnected = true;
        }
        catch (Exception ex)
        {
            ConnectionManager.Instance.IsConnected = false;
            OnErrorOccurred?.Invoke(ex.Message);
            StartReconnect();
        }
    }

    // Send a message to the WebSocket server and wait for a response
    public async Task<string> SendAsync(string message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            OnErrorOccurred?.Invoke("WebSocket is not connected.");
            return null;
        }

        try
        {
            ResponseCompletionSource = new TaskCompletionSource<string>();

            // Send the message to the server
            await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                                       WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);

            // Receive response (in the same method after sending the request)
            var buffer = new ArraySegment<byte> (new byte[12840]);

            StringBuilder sb = new StringBuilder();
            WebSocketReceiveResult result;
            do {
                result = await _webSocket.ReceiveAsync(buffer, System.Threading.CancellationToken.None);
                var chunk = Encoding.UTF8.GetString(buffer.Array, 0, result.Count).Trim();
                sb.Append(chunk.ToString());
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    NotifyDisconnection();
                    return null;
                }
            } while(!result.EndOfMessage);

            // Convert the received byte array to a string and parse as JSON
            string responseMessage = sb.ToString();

            // Trigger the OnResponseReceived event with the response message
            OnResponseReceived?.Invoke(responseMessage);

            return responseMessage;
        }
        catch (Exception ex)
        {
            OnErrorOccurred?.Invoke("Error while sending message: " + ex.Message);
            return null;
        }
    }

    // Disconnect from the WebSocket server
    public async Task Disconnect()
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            return;

        try
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", System.Threading.CancellationToken.None);
            NotifyDisconnection();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while disconnecting: " + ex.Message);
        }
    }

    // Notify disconnection and attempt to reconnect
    private void NotifyDisconnection()
    {
        lock (_lock)
        {
            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        Console.WriteLine("WebSocket has been disconnected.");
        OnDisconnected?.Invoke();
        ConnectionManager.Instance.IsConnected = false;

        StartReconnect();
    }

    // Handle reconnection attempts
    private async void StartReconnect()
    {
        if (_isReconnecting || _reconnectAttempts >= MaxReconnectAttempts)
            return;

        ConnectionManager.Instance.IsReconnecting = true;
        _reconnectAttempts++;

        Console.WriteLine("Attempting to reconnect...");

        await Task.Delay(ReconnectDelaySeconds * 1000);

        try
        {
            await Connect(_url);
            ConnectionManager.Instance.IsReconnecting = false;
            Console.WriteLine("Reconnected successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while reconnecting: " + ex.Message);
            StartReconnect();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _webSocket?.Dispose();
            _disposed = true;
        }
    }
    // Method to clear all event handlers by setting the event to null
    public void ClearEventHandlers()
    {
        OnResponseReceived = null;
    }
}
