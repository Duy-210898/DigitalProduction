using System;

namespace DigitalProduction
{
    public class ConnectionManager
    {
        private static ConnectionManager _instance;
        public bool _isConnected;
        public bool _isReconnecting;

        public static ConnectionManager Instance => _instance ?? (_instance = new ConnectionManager());

        public event Action<bool> ConnectionStatusChanged;
        public event Action<bool> ReconnectionStatusChanged;

        private ConnectionManager() { } // Prevent direct instantiation
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStatusChanged?.Invoke(_isConnected);
                }
            }
        }

        public bool IsReconnecting
        {
            get => _isReconnecting;
            set
            {
                if (_isReconnecting != value)
                {
                    _isReconnecting = value;
                    ReconnectionStatusChanged?.Invoke(_isReconnecting); 
                }
            }
        }
    }
}
