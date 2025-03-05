using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction.ViewModels
{
    public class DeviceOutputListViewModel : INotifyPropertyChanged
    {
        private WebSocketClient _webSocketClient;
        private BindingList<DeviceOutput> _bindingDeviceOutputs = new BindingList<DeviceOutput>();
        private System.Windows.Forms.Timer _timer;
        private SynchronizationContext _syncContext;  // To marshal updates to the UI thread

        public BindingList<DeviceOutput> BindingDeviceOutputs
        {
            get => _bindingDeviceOutputs;
            set { _bindingDeviceOutputs = value; OnPropertyChanged(nameof(BindingDeviceOutputs)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DeviceOutputListViewModel()
        {
            // Capture the UI thread synchronization context (assuming this is created on the UI thread)
            _syncContext = SynchronizationContext.Current;

            // Initialize the BindingList.
            _bindingDeviceOutputs = new BindingList<DeviceOutput>();

            // Initialize Timer with a 2-second interval.
            _timer = new System.Windows.Forms.Timer { Interval = 2000 };
            _timer.Tick += Timer_Tick;
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseRealTime += WebSocket_OnMessage;
            // Start the timer to request data every 2 seconds.
            _timer.Start();
        }

        public async void RequestData()
        {
            // Check if the WebSocket is open before sending.
            if (_webSocketClient != null)
            {
                var request = new { app = Global.App, action = "getActualData" };
                string jsonRequest = JsonConvert.SerializeObject(request);
                try
                {
                    await _webSocketClient.SendRealTimeAsync(jsonRequest);
                }
                catch (System.Net.WebSockets.WebSocketException ex)
                {
                    Console.WriteLine("WebSocket exception in RequestData: " + ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Invalid operation in RequestData: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("WebSocket is not open or is null in RequestData.");
            }
        }

        // Timer tick event handler to request data.
        private void Timer_Tick(object sender, EventArgs e)
        {
            RequestData();
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            // Use the captured synchronization context to ensure UI updates occur on the UI thread.
            if (_syncContext != null)
            {
                _syncContext.Post(_ => ProcessWebSocketMessage(jsonData), null);
            }
            else
            {
                // Fallback if no synchronization context is available.
                ProcessWebSocketMessage(jsonData);
            }
        }

        private void ProcessWebSocketMessage(string jsonData)
        {
            try
            {
                Console.WriteLine($"Received WebSocket Data: {jsonData}");
                var response = JsonConvert.DeserializeObject<WebSocketResponse>(jsonData);
                if (response?.RealTime != null && response.Status == "success")
                {
                    UpdateData(response.RealTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebSocket data: {ex.Message}");
            }
        }

        // Update only the values so that the UI only refreshes changed cells.
        private void UpdateData(RealTimeData realTimeData)
        {
            if (realTimeData.OutputData == null || !realTimeData.OutputData.Any())
            {
                Console.WriteLine("No OutputData received from WebSocket.");
                return;
            }

            // Group the output data.
            var groupedData = realTimeData.OutputData
              .GroupBy(d => new { d.MachineName, d.SO, d.OperatorName })
              .SelectMany(g =>
              {
                  bool machineNameUniform = g.All(x => x.MachineName == g.Key.MachineName);
                  bool soUniform = g.All(x => x.SO == g.Key.SO);
                  bool operatorUniform = g.All(x => x.OperatorName == g.Key.OperatorName);
                  int totalActualCut = (int)g.Sum(x => x.ActualCut);
                  int totalActualPieces = (int)g.Sum(x => x.ActualPieces);
                  int totalActualSizeQty = (int)g.Sum(x => x.ActualSizeQty);

                  // Create the header row using the group key.
                  var header = new DeviceOutput
                  {
                      MachineName = g.Key.MachineName,
                      SO = g.Key.SO,
                      OperatorName = g.Key.OperatorName,
                      IsGroupHeader = true,
                      ActualCut = totalActualCut,
                      ActualPieces = totalActualPieces,
                      ActualSizeQty = totalActualSizeQty
                  };

                  // For each item in the group, update the MaterialType and clear common values if uniform.
                  var items = g.Select(item =>
                  {
                      item.MaterialType = item.IsLeather ? "Leather Material" : "Raw Material";
                      if (machineNameUniform)
                      {
                          item.MachineName = string.Empty;
                      }
                      if (soUniform)
                      {
                          item.SO = string.Empty;
                      }
                      if (operatorUniform)
                      {
                          item.OperatorName = string.Empty;
                      }
                      return item;
                  });

                  // Return header followed by items.
                  return new[] { header }.Concat(items);
              })
              .ToList();

            Console.WriteLine($"Updating DeviceOutputs with {groupedData.Count} items.");
            UpdateBindingDeviceOutputs(groupedData);
        }

        private void UpdateBindingDeviceOutputs(IList<DeviceOutput> newData)
        {
            // Giả sử bạn có một control UI (đã khởi tạo trên UI thread)
            if (!System.Windows.Forms.Application.OpenForms[0].InvokeRequired)
            {
                PerformUpdateBindingDeviceOutputs(newData);
            }
            else
            {
                System.Windows.Forms.Application.OpenForms[0].Invoke(new Action(() =>
                {
                    PerformUpdateBindingDeviceOutputs(newData);
                }));
            }
        }

        private void PerformUpdateBindingDeviceOutputs(IList<DeviceOutput> newData)
        {
            int minCount = Math.Min(BindingDeviceOutputs.Count, newData.Count);
            int i = 0;
            // Cập nhật các phần tử đã có.
            for (; i < minCount; i++)
            {
                var existingItem = BindingDeviceOutputs[i];
                var newItem = newData[i];

                UpdateProperties(existingItem, newItem,
                    nameof(existingItem.MachineName),
                    nameof(existingItem.ActualCut),
                    nameof(existingItem.ActualPieces),
                    nameof(existingItem.Size),
                    nameof(existingItem.SizeQty),
                    nameof(existingItem.InventoryQty),
                    nameof(existingItem.ActualSizeQty),
                    nameof(existingItem.TotalPiecesPerPair));
            }
            // Thêm các phần tử mới nếu newData có nhiều hơn.
            for (; i < newData.Count; i++)
            {
                BindingDeviceOutputs.Add(newData[i]);
            }
            // Loại bỏ các phần tử dư nếu BindingDeviceOutputs có nhiều hơn newData.
            while (BindingDeviceOutputs.Count > newData.Count)
            {
                BindingDeviceOutputs.RemoveAt(BindingDeviceOutputs.Count - 1);
            }
            OnPropertyChanged(nameof(BindingDeviceOutputs));
        }



        public static void UpdateProperties<T>(T existing, T updated, params string[] propertyNames)
        {
            if (existing == null || updated == null)
                throw new ArgumentNullException("Neither the existing nor updated object can be null.");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            foreach (string propertyName in propertyNames)
            {
                PropertyDescriptor prop = properties[propertyName];
                if (prop != null && !prop.IsReadOnly)
                {
                    var currentValue = prop.GetValue(existing);
                    var newValue = prop.GetValue(updated);
                    if (!object.Equals(currentValue, newValue))
                    {
                        try
                        {
                            prop.SetValue(existing, newValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating property '{propertyName}': {ex.Message}");
                            throw;
                        }
                    }
                }
            }
        }

        public class RealTimeData
        {
            public List<DeviceOutput> OutputData { get; set; }
        }

        public class WebSocketResponse
        {
            public string Action { get; set; }
            public string Status { get; set; }
            public RealTimeData RealTime { get; set; }
        }
    }
}
