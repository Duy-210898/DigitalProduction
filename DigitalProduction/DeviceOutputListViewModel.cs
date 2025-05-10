using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DigitalProduction.Models;
using Newtonsoft.Json;

namespace DigitalProduction.ViewModels
{
    public class DeviceOutputListViewModel : INotifyPropertyChanged
    {
        private WebSocketClient _webSocketClient;
        private BindingList<DeviceOutput> _bindingDeviceOutputs = new BindingList<DeviceOutput>();
        private System.Threading.Timer _pollingTimer; // Dùng Timer chạy nền thay vì WinForms Timer
        private SynchronizationContext _syncContext;  // To marshal updates to the UI thread
        private string _lastJsonData;


        private bool _isRecentlyUpdated;

        public bool IsRecentlyUpdated
        {
            get => _isRecentlyUpdated;
            set
            {
                if (_isRecentlyUpdated != value)
                {
                    _isRecentlyUpdated = value;
                    OnPropertyChanged(nameof(IsRecentlyUpdated)); 
                }
            }
        }
        // Filter properties for API
        private string _filterKeyword = LocalizationManager.GetString("Search");
        public string FilterKeyword
        {
            get => _filterKeyword;
            set
            {
                if (_filterKeyword != value)
                {
                    _filterKeyword = value;
                    OnPropertyChanged(nameof(FilterKeyword));
                    // Re-filter using cached WebSocket data
                    if (!string.IsNullOrEmpty(_lastJsonData))
                    {
                        if (_syncContext != null)
                        {
                            _syncContext.Post(_ => ProcessWebSocketMessage(_lastJsonData), null);
                        }
                        else
                        {
                            ProcessWebSocketMessage(_lastJsonData);
                        }
                    }
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
                    // Call RequestData when the date changes
                    RequestData();
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
                    // Call RequestData when the date changes
                    RequestData();
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
        public string FilterPartName
        {
            get => FilterService.Instance.PartName;
            set
            {
                if (FilterService.Instance.PartName != value)
                {
                    FilterService.Instance.PartName = value;
                    OnPropertyChanged(nameof(FilterPartName));
                }
            }
        }

        public void SyncData()
        {
            // Re-fetch or refresh the BindingDeviceOutputs
            RequestData(); // Or however your logic pulls data
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
        }

        public void SetWebSocketClient(WebSocketClient webSocket)
        {
            _webSocketClient = webSocket ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseRealTime += WebSocket_OnMessage;

            RequestData();
            // Bắt đầu polling kiểm tra thay đổi trong SQL Server
            StartPolling();
        }

        private void StartPolling()
        {
            _pollingTimer = new System.Threading.Timer(async _ =>
            {
                await CheckForSqlUpdates();
            }, null, 0, 2000);
        }

        private async Task CheckForSqlUpdates()
        {
            try
            {
                bool hasUpdates = await DbHelper.CheckForSqlUpdates();
                if (hasUpdates)
                {
                    Console.WriteLine("SQL data changed, requesting new data...");
                    RequestData();
                }
                else {
                    Console.WriteLine("No SQL updates found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking SQL updates: {ex.Message}");
            }
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
                        partName = string.IsNullOrEmpty(FilterPartName) ? null : FilterPartName,
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
                    ConnectionManager.Instance.IsReconnecting = true;
                    Console.WriteLine("WebSocket exception in RequestData: " + ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    ConnectionManager.Instance.IsReconnecting = true;
                    Console.WriteLine("Invalid operation in RequestData: " + ex.Message);
                }
            }
            else
            {
                ConnectionManager.Instance.IsReconnecting = true;
                Console.WriteLine("WebSocket is not open or is null in RequestData.");
            }
        }

        private void WebSocket_OnMessage(string jsonData)
        {
            _lastJsonData = jsonData; // cache last received data
            if (_syncContext != null)
            {
                _syncContext.Post(_ => ProcessWebSocketMessage(jsonData), null);
            }
            else
            {
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
                            (d.PartName?.ToLower().Contains(cleanedKeyword) ?? false) ||
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

        private void UpdateData(RealTimeData realTimeData)
        {

            if (realTimeData.OutputData == null || !realTimeData.OutputData.Any())
            {
                Console.WriteLine("No OutputData received from WebSocket.");
                BindingDeviceOutputs.Clear();
                return;
            }

            var headersDictionary = new Dictionary<string, DeviceOutput>();

            var groupedData = realTimeData.OutputData
                .GroupBy(d => new { UpdatedAt = d.UpdatedAt != null ? d.UpdatedAt.Value.ToString("yyyyMMdd") :  string.Empty, d.PartName, d.MachineName, d.SO, d.OperatorName })
                .SelectMany(group =>
                {
                    string groupKey = $"{group.Key.UpdatedAt:yyyyMMdd}-{group.Key.PartName}-{group.Key.MachineName}-{group.Key.SO}-{group.Key.OperatorName}";

                    if (!headersDictionary.TryGetValue(groupKey, out var header))
                    {
                        header = CreateHeader(group, groupKey);
                        Console.WriteLine($"[HEADER CREATED] Group: {groupKey} | IsLeather: {group.Any(x => x.IsLeather)}");
                        headersDictionary[groupKey] = header;
                    }

                    UpdateHeaderAggregates(header, group);

                    var items = group.Select(item =>
                    {
                        PrepareChildItem(item, group.Count());
                        return item;
                    })
                    .OrderByDescending(x => x.UpdatedAt)
                    .ToList();

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

        private DeviceOutput CreateHeader(IGrouping<dynamic, DeviceOutput> group, string groupKey)
        {
            return new DeviceOutput
            {
                PartName = group.Key.PartName,
                MachineName = group.Key.MachineName,
                SO = group.Key.SO,
                OperatorName = group.Key.OperatorName,
                IsGroupHeader = true,
                IsLeather = group.Any(x => x.IsLeather),
                MaterialType = group.Any(x => x.IsLeather)
                    ? LocalizationManager.GetString("leatherMaterial")
                    : LocalizationManager.GetString("rawMaterial"),
            };
        }

        private void UpdateHeaderAggregates(DeviceOutput header, IGrouping<dynamic, DeviceOutput> group)
        {
            DateTime? createdAt = group.Min(x => x.Timestamp);
            DateTime? updatedAt = group.Max(x => x.UpdatedAt);

            if (header.Timestamp != createdAt)
            {
                header.Timestamp = createdAt;
                header.OnPropertyChanged(nameof(header.Timestamp));
            }

            if (header.UpdatedAt != updatedAt)
            {
                header.UpdatedAt = updatedAt;
                header.OnPropertyChanged(nameof(header.UpdatedAt));
            }

            Console.WriteLine($"[HEADER UPDATE] Group: {header.PartName}-{header.MachineName}-{header.SO}-{header.OperatorName}");
            Console.WriteLine($"  -> Timestamp: {createdAt}, UpdatedAt: {updatedAt}");

        }

        private void PrepareChildItem(DeviceOutput item, int groupCount)
        {
            item.MaterialType = item.IsLeather
                ? LocalizationManager.GetString("leatherMaterial")
                : LocalizationManager.GetString("rawMaterial");

            // Clear repeated fields for grouped display
            item.Timestamp = null;
            item.UpdatedAt = null;
            item.MachineName = string.Empty;
            item.SO = string.Empty;
            item.OperatorName = string.Empty;
            item.PartName = string.Empty;
            item.MaterialType = string.Empty;
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

                var existingGroups = BindingDeviceOutputs
               .Where(d => d.IsGroupHeader)
               .GroupBy(d => new { UpdatedAt = d.UpdatedAt != null ? d.UpdatedAt.Value.ToString("yyyyMMdd") : string.Empty, d.MachineName, d.SO, d.OperatorName, d.PartName })
               .ToDictionary(g => g.Key, g => g.First());

                int index = 0;

                foreach (var newItem in newData)
                {
                    if (newItem.IsGroupHeader)
                    {
                        // If group header exists, update values
                        var key = new { UpdatedAt = newItem.UpdatedAt?.ToString("yyyyMMdd") ?? string.Empty, newItem.MachineName, newItem.SO, newItem.OperatorName, newItem.PartName };
                        if (existingGroups.TryGetValue(key, out var existingHeader))
                        {
                            // Update group header values
                            existingHeader.CuttingDieQty = newItem.CuttingDieQty;
                            existingHeader.PiecesPerPair = newItem.PiecesPerPair;
                            existingHeader.MaterialLayer = newItem.MaterialLayer;
                            existingHeader.TotalPiecesPerPair = newItem.TotalPiecesPerPair;
                            existingHeader.ActualCut = newItem.ActualCut;
                            existingHeader.ActualPieces = newItem.ActualPieces;
                            existingHeader.ActualSizeQty = newItem.ActualSizeQty;
                        }
                        else
                        {
                            // Add new group header if not found
                            BindingDeviceOutputs.Insert(index, newItem);
                        }
                    }
                    else
                    {
                        // Ensure correct row updates for individual items
                        if (index < BindingDeviceOutputs.Count)
                        {
                            var existingItem = BindingDeviceOutputs[index];
                            if (existingItem.IsGroupHeader == false)
                            {
                                UpdateProperties(existingItem, newItem,
                                    nameof(existingItem.MachineName),
                                    nameof(existingItem.SO),
                                    nameof(existingItem.OperatorName),
                                    nameof(existingItem.ActualCut),
                                    nameof(existingItem.ActualPieces),
                                    nameof(existingItem.Size),
                                    nameof(existingItem.SizeQty),
                                    nameof(existingItem.InventoryQty),
                                    nameof(existingItem.ActualSizeQty),
                                    nameof(existingItem.TotalPiecesPerPair));
                            }
                            else
                            {
                                BindingDeviceOutputs.Insert(index, newItem);
                            }
                        }
                        else
                        {
                            BindingDeviceOutputs.Add(newItem);
                        }
                    }
                    index++;
                }

                // Remove extra items if the new data is smaller
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
            bool hasChanged = false;

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
                            hasChanged = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating property '{propertyName}': {ex.Message}");
                            throw;
                        }
                    }
                }
            }

            // Mark as updated (for animation/highlight)
            if (hasChanged && typeof(T).GetProperty("IsRecentlyUpdated") != null)
            {
                typeof(T).GetProperty("IsRecentlyUpdated")?.SetValue(existing, true);
                // Reset after a short delay
                Task.Delay(1000).ContinueWith(_ =>
                {
                    typeof(T).GetProperty("IsRecentlyUpdated")?.SetValue(existing, false);
                });
            }
        }

        public void Dispose()
        {
            _pollingTimer?.Dispose();
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseRealTime -= WebSocket_OnMessage;
            }
            Console.WriteLine("ViewModel disposed.");
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
