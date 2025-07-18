using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
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
        private static int currentPage = 1;
        private static int pageSize = 100;
        private int totalCount = 0;
        private static ComboBoxEdit cmbPageSize;
        private static SimpleButton btnPrev;
        private static SimpleButton btnNext;
        private static LabelControl lblPagingInfo;
     
        public Action ShowLoadingAction { get; set; }
        public Action HideLoadingAction { get; set; }
        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        private bool _isLive = true;
        public bool IsLive
        {
            get => _isLive;
            set
            {
                if (_isLive != value)
                {
                    _isLive = value;
                    OnPropertyChanged(nameof(IsLive));

                    if (_isLive)
                        StartLiveUpdates();
                    else
                        StopLiveUpdates();
                }
            }
        }

        private void StartLiveUpdates()
        {
            Console.WriteLine("Live updates started.");
            // Start your timer/WebSocket listening here
        }

        private void StopLiveUpdates()
        {
            Console.WriteLine("Live updates stopped.");
            // Stop your timer/WebSocket listening here
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
                    ReapplyFilters();
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
                    _ = SyncDataAsync();
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
                    _ = SyncDataAsync();
                }
            }
        }

        private void ReapplyFilters()
        {
            if (string.IsNullOrEmpty(_lastJsonData)) return;

            if (_syncContext != null)
                _syncContext.Post(_ => ProcessWebSocketMessage(_lastJsonData), null);
            else
                ProcessWebSocketMessage(_lastJsonData);
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

        public async Task SyncDataAsync()
        {
            try
            {
                IsLoading = false;

                // Optional: simulate delay
                await Task.Delay(1000);

                // Load real data here
                await LoadCurrentPageAsync(); // your actual data fetch method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sync WebSocket data: {ex.Message}");
            }
            finally
            {
                ReapplyFilters();
                IsLoading = true;
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
        }

        public void SetWebSocketClient(WebSocketClient webSocket)
        {
            _webSocketClient = webSocket ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseRealTime += WebSocket_OnMessage;

            // Bắt đầu polling kiểm tra thay đổi trong SQL Server
            StartPolling();
        }

        private void StartPolling()
        {
            _pollingTimer = new System.Threading.Timer(async _ =>
            {
                if (!IsLive) return; // ✅ Stop polling if live mode is off
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
                    await LoadCurrentPageAsync();
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
                if (response.Status.Equals("success") && response.Data.Count > 0)
                {
                    Console.WriteLine($"Status: {response.Status}, Total Items: {response.TotalCount}");
                    // Paging metadata
                    int currentPage = response.Page;
                    int totalPages = response.TotalPages;
                    totalCount = response.TotalCount;

                    // Display or bind to UI
                    PageNumber = currentPage;
                    lblPagingInfo.Text = $"{LocalizationManager.GetString("Page")} {currentPage} / {totalPages} | {LocalizationManager.GetString("TotalRecords")} {totalCount}";

                    // Apply keyword filtering if needed
                    string keyword = FilterKeyword?.Trim().ToLower();
                    bool hasKeyword = !string.IsNullOrWhiteSpace(keyword) && FilterKeyword != LocalizationManager.GetString("Search");
                    string cleanedKeyword = hasKeyword ? keyword : "";

                    var filteredData = response.Data
                   .Where(d =>
                       !hasKeyword ||
                       (d.PartName?.ToLower().Contains(cleanedKeyword) ?? false) ||
                       (d.MachineName?.ToLower().Contains(cleanedKeyword) ?? false) ||
                       (d.SO?.ToLower().Contains(cleanedKeyword) ?? false) ||
                       (d.OperatorName?.ToLower().Contains(cleanedKeyword) ?? false))
                   .ToList();

                    // Then update your UI or data context
                    UpdateData(filteredData);
                }
                else
                {
                    Console.WriteLine("Error or empty response.");
                    if (BindingDeviceOutputs.Count > 0)
                    {
                        BindingDeviceOutputs.RaiseListChangedEvents = false;
                        BindingDeviceOutputs.Clear();
                        BindingDeviceOutputs.RaiseListChangedEvents = true;
                        BindingDeviceOutputs.ResetBindings();
                        OnPropertyChanged(nameof(BindingDeviceOutputs));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebSocket data: {ex.Message}");
            }
        }


        private void UpdateData(List<DeviceOutput> realTimeData)
        {
            if (realTimeData == null || !realTimeData.Any())
            {
                Console.WriteLine("No OutputData received from WebSocket.");
                BindingDeviceOutputs.Clear();
                return;
            }

            // Group by core attributes including IsLeather
            List<DeviceOutput> groupedData = realTimeData
                .GroupBy(d => new
                {
                    UpdatedAt = d.UpdatedAt?.Date.ToString("yyyyMMdd") ?? "00000000",
                    d.PartName,
                    d.MachineName,
                    d.SO,
                    d.OperatorName,
                    d.IsLeather
                })
                .SelectMany(group =>
                {
                    bool isLeather = group.Key.IsLeather;
                    // Child rows
                    var children = group
                        .Select(child =>
                        {
                            child.IsGroupHeader = false;
                            child.MaterialType = child.IsLeather
                                ? LocalizationManager.GetString("leatherMaterial")
                                : LocalizationManager.GetString("rawMaterial");
                            return child;
                        })
                        .OrderByDescending(c => c.UpdatedAt);

                    if (!children.Any())
                        return Enumerable.Empty<DeviceOutput>();
                    // Header row
                    var header = new DeviceOutput
                    {
                        IsGroupHeader = true,
                        MachineName = group.Key.MachineName,
                        SO = group.Key.SO,
                        PartName = group.Key.PartName,
                        OperatorName = group.Key.OperatorName,
                        UpdatedAt = group.FirstOrDefault()?.UpdatedAt,
                        IsLeather = isLeather,
                        MaterialType = isLeather
                            ? LocalizationManager.GetString("leatherMaterial")
                            : LocalizationManager.GetString("rawMaterial"),
                    };

                    return new[] { header }.Concat(children);
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
                if (newData.Count > 200) // large batch
                {
                    BindingDeviceOutputs.ReplaceWith(newData);
                }
                else
                {
                    var newLookup = newData.ToDictionary(x => x.UniqueKey());
                    var existingKeys = new HashSet<string>(BindingDeviceOutputs.Select(x => x.UniqueKey()));

                    // Update existing rows
                    for (int i = 0; i < BindingDeviceOutputs.Count; i++)
                    {
                        var existing = BindingDeviceOutputs[i];
                        if (newLookup.TryGetValue(existing.UniqueKey(), out var updated))
                        {
                            UpdateProperties(existing, updated,
                                nameof(DeviceOutput.ActualCut),
                                nameof(DeviceOutput.ActualPieces),
                                nameof(DeviceOutput.InventoryQty),
                                nameof(DeviceOutput.ActualSizeQty));
                        }
                    }

                    // Add new rows
                    foreach (var newRow in newData)
                    {
                        if (!existingKeys.Contains(newRow.UniqueKey()))
                            BindingDeviceOutputs.Add(newRow);
                    }

                    // Remove extra rows
                    for (int i = BindingDeviceOutputs.Count - 1; i >= 0; i--)
                    {
                        if (!newLookup.ContainsKey(BindingDeviceOutputs[i].UniqueKey()))
                            BindingDeviceOutputs.RemoveAt(i);
                    }
                }
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
            bool hasChanged = false;

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

            if (hasChanged && existing is DeviceOutput row)
            {
                row.IsRecentlyUpdated = true;

                // Use SynchronizationContext to ensure safe UI-thread update
                var syncContext = SynchronizationContext.Current;
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    if (syncContext == null)
                    {
                        return;
                    }
                    syncContext.Post(_ => row.IsRecentlyUpdated = false, null);
                });
            }
        }

        public void InitPagingFooter(Control gridControl)
        {
            var padding = 10;

            var pagingPanel = new DevExpress.XtraEditors.PanelControl
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };

            // ComboBox for page size
            cmbPageSize = new DevExpress.XtraEditors.ComboBoxEdit
            {
                Width = 80,
                Location = new Point(padding, 8),
                ToolTip = LocalizationManager.GetString("SelectPageSize")
            };
            cmbPageSize.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cmbPageSize.Properties.Items.AddRange(new object[] { 50, 100, 200, 500 });
            // Set initial value before assigning the event
            cmbPageSize.SelectedItem = pageSize;
            cmbPageSize.SelectedIndexChanged += async (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int newSize))
                {
                    pageSize = newSize;
                    currentPage = 1;
                    await LoadCurrentPageAsync();
                }
            };
            pagingPanel.Controls.Add(cmbPageSize);

            // Next button
            btnNext = new DevExpress.XtraEditors.SimpleButton
            {
                Text = LocalizationManager.GetString("Next") + " »",
                Location = new Point(cmbPageSize.Right + padding, 6),
                ToolTip = LocalizationManager.GetString("GoToNextPage")
            };
            btnNext.Click += async (s, e) =>
            {
                if (currentPage < GetTotalPages())
                {
                    currentPage++;
                    await LoadCurrentPageAsync();
                }
            };
            pagingPanel.Controls.Add(btnNext);

            // Previous button
            btnPrev = new DevExpress.XtraEditors.SimpleButton
            {
                Text = "« " + LocalizationManager.GetString("Previous"),
                Location = new Point(btnNext.Right + padding, 6),
                ToolTip = LocalizationManager.GetString("GoToPreviousPage")
            };
            btnPrev.Click += async (s, e) =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    await LoadCurrentPageAsync();
                }
            };
            pagingPanel.Controls.Add(btnPrev);

            // Paging info label
            lblPagingInfo = new DevExpress.XtraEditors.LabelControl
            {
                Text = $"{LocalizationManager.GetString("Page")} {currentPage} / {GetTotalPages()}",
                Location = new Point(btnPrev.Right + padding * 2, 10),
                Width = 250,
                AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical
            };
            pagingPanel.Controls.Add(lblPagingInfo);

            // Add panel to grid control
            gridControl.Controls.Add(pagingPanel);
            pagingPanel.BringToFront();
        }
        private async Task LoadCurrentPageAsync()
        {
            SetWebSocketClient(WebSocketClient.Instance);
            if (_webSocketClient == null)
            {
                Console.WriteLine("WebSocket is not connected.");
                return;
            }
            try
            {
                var request = new
                {
                    app = Global.App,
                    action = "getActualData",
                    filter = new
                    {
                        startDate = FilterStartDate?.ToString("yyyy-MM-dd"),
                        endDate = FilterEndDate?.ToString("yyyy-MM-dd"),
                        partName = string.IsNullOrEmpty(FilterPartName) ? null : FilterPartName,
                        machineName = string.IsNullOrEmpty(FilterMachineName) ? null : FilterMachineName,
                        so = string.IsNullOrEmpty(FilterSO) ? null : FilterSO,
                        operatorName = string.IsNullOrEmpty(FilterOperatorName) ? null : FilterOperatorName,
                        page = currentPage,
                        pageSize = pageSize
                    }
                };

                string json = JsonConvert.SerializeObject(request);
                await _webSocketClient.SendRealTimeAsync(json);
            }
            catch (Exception ex )
            {
                Console.WriteLine($"Error sending LoadCurrentPageAsync: {ex.Message}");
            }
        }
        private int GetTotalPages()
        {
            return (int)Math.Ceiling((double)totalCount / pageSize);
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


        //}

        public class WebSocketResponse
        {
            public string Action { get; set; }
            public string Status { get; set; }
            public List<DeviceOutput> Data { get; set; }
            // Pagination info
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 100;
    }
    public static class BindingListExtensions
    {
        public static void ReplaceWith<T>(this BindingList<T> collection, IEnumerable<T> newItems)
        {
            collection.RaiseListChangedEvents = false;
            collection.Clear();
            foreach (var item in newItems)
                collection.Add(item);
            collection.RaiseListChangedEvents = true;
            collection.ResetBindings();
        }
    }
}
