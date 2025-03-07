using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
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

        // Filter properties for API
        public string FilterKeyword
        {
            get => FilterService.Instance.FilterKeyword;
            set
            {
                if (FilterService.Instance.FilterKeyword != value)
                {
                    FilterService.Instance.FilterKeyword = value;
                    OnPropertyChanged(nameof(FilterKeyword));
                }
            }
        }

        public DateTime? FilterStartDate
        {
            get => FilterService.Instance.FilterStartDate;
            set
            {
                if (FilterService.Instance.FilterStartDate != value)
                {
                    FilterService.Instance.FilterStartDate = value ?? DateTime.Today;
                    OnPropertyChanged(nameof(FilterStartDate));
                }
            }
        }

        public DateTime? FilterEndDate
        {
            get => FilterService.Instance.FilterEndDate;
            set
            {
                if (FilterService.Instance.FilterEndDate != value)
                {
                    FilterService.Instance.FilterEndDate = value ?? DateTime.Today;
                    OnPropertyChanged(nameof(FilterEndDate));
                }
            }
        }

        public string FilterMachineName
        {
            get => FilterService.Instance.FilterMachineName;
            set
            {
                if (FilterService.Instance.FilterMachineName != value)
                {
                    FilterService.Instance.FilterMachineName = value;
                    OnPropertyChanged(nameof(FilterMachineName));
                }
            }
        }

        public string FilterSO
        {
            get => FilterService.Instance.FilterSO;
            set
            {
                if (FilterService.Instance.FilterSO != value)
                {
                    FilterService.Instance.FilterSO = value;
                    OnPropertyChanged(nameof(FilterSO));
                }
            }
        }

        public string FilterOperatorName
        {
            get => FilterService.Instance.FilterOperatorName;
            set
            {
                if (FilterService.Instance.FilterOperatorName != value)
                {
                    FilterService.Instance.FilterOperatorName = value;
                    OnPropertyChanged(nameof(FilterOperatorName));
                }
            }
        }


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
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

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
                // Build the request object including the filter parameters.
                var request = new
                {
                    app = Global.App,
                    action = "getActualData",
                    filter = new
                    {
                        // Format dates as "yyyy-MM-dd" or adjust as required.
                        startDate = FilterStartDate.HasValue ? FilterStartDate.Value.ToString("yyyy-MM-dd") : null,
                        endDate = FilterEndDate.HasValue ? FilterEndDate.Value.ToString("yyyy-MM-dd") : null,
                        machineName = string.IsNullOrEmpty(FilterMachineName) ? null : FilterMachineName,
                        so = string.IsNullOrEmpty(FilterSO) ? null : FilterSO,
                        operatorName = string.IsNullOrEmpty(FilterOperatorName) ? null : FilterOperatorName
                    }
                };
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
                    var keyword = FilterKeyword?.Trim().ToLower(); // Normalize keyword for comparison

                    var cleanedKeyword = string.IsNullOrWhiteSpace(keyword) || FilterKeyword == LocalizationManager.GetString("Search") ? "" : keyword.ToLower();
                    var filteredData = response.RealTime.OutputData
                        .Where(d =>
                            string.IsNullOrEmpty(keyword) || // Show all if no keyword
                            (d.MachineName?.ToLower().Contains(cleanedKeyword) ?? false) ||
                            (d.SO?.ToLower().Contains(cleanedKeyword) ?? false) ||
                            (d.OperatorName?.ToLower().Contains(cleanedKeyword) ?? false))
                        .ToList();

                    UpdateData(new RealTimeData { OutputData = filteredData });
                }
                else
                {
                    Console.WriteLine("error.");
                    if (BindingDeviceOutputs.Count > 0)
                    {
                        // Reset DataGridView by clearing BindingDeviceOutputs
                        BindingDeviceOutputs.RaiseListChangedEvents = false;
                        BindingDeviceOutputs.Clear();
                        BindingDeviceOutputs.RaiseListChangedEvents = true;
                        BindingDeviceOutputs.ResetBindings();

                        OnPropertyChanged(nameof(BindingDeviceOutputs)); // Ensure UI updates
                    }
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
                BindingDeviceOutputs.Clear();
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
                      item.MaterialType = item.IsLeather ? LocalizationManager.GetString("leatherMaterial") : LocalizationManager.GetString("rawMaterial");
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
            try
            {
                UpdateBindingDeviceOutputs(groupedData);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"InvalidOperationException in UpdateBindingDeviceOutputs: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void UpdateBindingDeviceOutputs(IList<DeviceOutput> newData)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(_ =>
                {
                    PerformUpdateBindingDeviceOutputs(newData);
                }, null);
            }
            else
            {
                // Direct UI update if no context (fallback)
                PerformUpdateBindingDeviceOutputs(newData);
            }
        }

        private void PerformUpdateBindingDeviceOutputs(IList<DeviceOutput> newData)
        {
            try
            {
                Console.WriteLine($"Performing UI update with {newData.Count} items.");

                int minCount = Math.Min(BindingDeviceOutputs.Count, newData.Count);
                int i = 0;

                // Update existing items
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

                // Add new items
                for (; i < newData.Count; i++)
                {
                    BindingDeviceOutputs.Add(newData[i]);
                }

                // Remove extra items
                while (BindingDeviceOutputs.Count > newData.Count)
                {
                    BindingDeviceOutputs.RemoveAt(BindingDeviceOutputs.Count - 1);
                }

                OnPropertyChanged(nameof(BindingDeviceOutputs));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating UI: {ex.Message}\n{ex.StackTrace}");
            }
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
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer.Dispose();
                _timer = null;
            }

            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseRealTime -= WebSocket_OnMessage;
            }

            Console.WriteLine("DeviceOutputListViewModel disposed successfully.");
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
