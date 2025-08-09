using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DigitalProduction.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace DigitalProduction
{
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
        public event Action<string> OnResponseRealTime;
        private readonly Dictionary<string, Action<string>> _responseHandlers = new Dictionary<string, Action<string>>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

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

            while (true) // Loop to keep trying to reconnect
            {
                try
                {
                    _url = url;
                    var uri = new Uri(_url);
                    bool portIsOpen = await IsPortOpenAsync(uri.Host, uri.Port);
                    if (!portIsOpen)
                    {
                        Console.WriteLine("❌ Cannot connect to localhost:8000.");
                        ConnectionManager.Instance.IsConnected = false;
                        continue;
                    }
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);

                    if (_webSocket == null && _webSocket.State != WebSocketState.Open)
                    {
                        Console.WriteLine("WebSocket not connected.");
                        continue;
                    }
                    ConnectionManager.Instance.IsConnected = true;
                    Console.WriteLine("WebSocket connected.");
                    await Task.Delay(5000);
                    //break; // Exit the loop on successful connection
                }
                catch (WebSocketException ex)
                {
                    ConnectionManager.Instance.IsConnected = false;
                    OnErrorOccurred?.Invoke($"WebSocket error: {ex.Message}");

                    // Optional: Add specific logic to check the type of error
                    // For instance, if the error indicates that the server is temporarily unavailable,
                    // you might want to wait before trying to reconnect.
                    Console.WriteLine("Failed to connect. Retrying in 2 seconds...");

                    await Task.Delay(2000); // Wait before retrying. Increase if necessary
                }
                catch (Exception ex)
                {
                    ConnectionManager.Instance.IsConnected = false;
                    OnErrorOccurred?.Invoke($"General error: {ex.Message}");
                    // Optionally implement a delay before retrying
                    await Task.Delay(2000); // Wait before trying to reconnect.
                }
            }
        }
        private void HandleConnectionError(string message)
        {
            ConnectionManager.Instance.IsConnected = false;
            OnErrorOccurred?.Invoke(message);
            Console.WriteLine("Failed to connect. Retrying...");
        }
        public static async Task<bool> IsPortOpenAsync(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(1000); // 1 second timeout

                    var completed = await Task.WhenAny(connectTask, timeoutTask);
                    return completed == connectTask && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }


        public void RegisterHandler(string requestId, Action<string> handler)
        {
            lock (_responseHandlers)
            {
                _responseHandlers[requestId] = handler;
            }
        }

        public void UnregisterHandler(string requestId)
        {
            lock (_responseHandlers)
            {
                _responseHandlers.Remove(requestId);
            }
        }


        public async Task<string> SendAsync(string message)
        {
            try
            {
                if (_webSocket == null ||
                    _webSocket.State == WebSocketState.Aborted ||
                    _webSocket.State == WebSocketState.Closed)
                {
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri(_url), _cts.Token);
                }

                ResponseCompletionSource = new TaskCompletionSource<string>();

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token);

                string responseMessage = await ReceiveFullWebSocketMessageAsync();
                if (string.IsNullOrEmpty(responseMessage))
                {
                    // Rollback due to null or empty WebSocket response
                    await SendAsync(message);

                    OnErrorOccurred?.Invoke("Empty or null response received from WebSocket.");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(responseMessage))
                {
                    OnErrorOccurred?.Invoke("Empty or null response received.");
                    return null;
                }

                if (!IsValidJson(responseMessage, out string validationError))
                {
                    OnErrorOccurred?.Invoke("Invalid JSON format: " + validationError);
                    return null;
                }
                OnResponseReceived?.Invoke(responseMessage);
                ValidateEntries(responseMessage);
                return responseMessage;
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke("Error while sending message: " + ex.Message);
                return null;
            }
        }

        public void ValidateEntries(string json)
        {
            try
            {
                var root = JObject.Parse(json);
                var items = root["distributionData"] as JArray;
                if (items == null)
                {
                    OnErrorOccurred?.Invoke("distributionData is missing or invalid.");
                    return;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var token = items[i];
                    if (token == null)
                    {
                        OnErrorOccurred?.Invoke($"Null entry at index {i}.");
                        continue;
                    }

                    try
                    {
                        var materialName = token["MaterialName"]?.ToString();
                        if (materialName == null)
                        {
                            OnErrorOccurred?.Invoke($"Missing MaterialName at index {i}.");
                        }

                        var entry = token.ToObject<DistributionData>();
                        if (entry == null)
                        {
                            OnErrorOccurred?.Invoke($"Failed to deserialize entry at index {i}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred?.Invoke($"Invalid item at index {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke("JSON parsing failed: " + ex.Message);
            }
        }

        private bool IsValidJson(string json, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON string is empty or null.";
                return false;
            }

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch (JsonReaderException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private async Task<string> ReceiveFullWebSocketMessageAsync()
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                }
                catch (WebSocketException ex)
                {
                    OnErrorOccurred?.Invoke("WebSocket error: " + ex.Message);
                    NotifyDisconnection();
                    return null;
                }
                catch (InvalidOperationException ex)
                {
                    OnErrorOccurred?.Invoke("Invalid operation: " + ex.Message);
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    NotifyDisconnection();
                    return null;
                }

                // Defensive check
                if (buffer == null || result.Count == 0)
                {
                    OnErrorOccurred?.Invoke("Received empty or null buffer.");
                    return null;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                    break;
            }

            string json = sb.ToString();

            return json;
        }

        public async Task<string> SendRealTimeAsync(string message)
        {
            if (_webSocket == null ||
                    _webSocket.State == WebSocketState.Aborted ||
                    _webSocket.State == WebSocketState.Closed)
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(_url), _cts.Token);
            }

            try
            {
                ResponseCompletionSource = new TaskCompletionSource<string>();

                // Send the message to the server
                await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                                           WebSocketMessageType.Text, true, _cts.Token);

                // Receive response (in the same method after sending the request)
                var buffer = new ArraySegment<byte>(new byte[12840]);

                StringBuilder sb = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
                    var messageChunk = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    sb.Append(messageChunk);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        NotifyDisconnection();
                        return null;
                    }
                } while (!result.EndOfMessage);

                // Convert the received byte array to a string and parse as JSON
                string responseMessage = sb.ToString();
                string normalizedMessage = responseMessage.Normalize(NormalizationForm.FormC);
                OnResponseRealTime?.Invoke(normalizedMessage);
                return normalizedMessage;
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
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
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

            _isReconnecting = true;
            ConnectionManager.Instance.IsReconnecting = true;

            Console.WriteLine($"🔁 Reconnect attempt {_reconnectAttempts + 1}/{MaxReconnectAttempts}");

            _reconnectAttempts++;

            await Task.Delay(ReconnectDelaySeconds * 1000);

            try
            {
                await Connect(_url);
                _isReconnecting = false;
                _reconnectAttempts = 0; // Reset on success
                ConnectionManager.Instance.IsReconnecting = false;
                Console.WriteLine("✅ Reconnected successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error while reconnecting: " + ex.Message);
                _isReconnecting = false;
                StartReconnect(); // Recursive call with safety above
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
            OnResponseRealTime = null;
            OnResponseReceived = null;
        }
    }
}
