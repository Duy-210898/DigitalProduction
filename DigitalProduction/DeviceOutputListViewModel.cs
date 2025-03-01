using DigitalProduction.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Reflection;

namespace DigitalProduction.ViewModels
{
    public class DeviceOutputListViewModel : INotifyPropertyChanged
    {
        private WebSocketClient _webSocketClient;
        private BindingList<DeviceOutput> _bindingDeviceOutputs = new BindingList<DeviceOutput>();
        private Timer _timer;

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
            // Initialize the collections once.
            _bindingDeviceOutputs = new BindingList<DeviceOutput>();

            // Initialize Timer with a 2-second interval.
            _timer = new Timer { Interval = 2000 };
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
            var request = new { app = Global.App, action = "getActualData" };
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendRealTimeAsync(jsonRequest);
        }

        // Timer tick event handler to request data.
        private void Timer_Tick(object sender, EventArgs e)
        {
            RequestData();
        }

        private void WebSocket_OnMessage(string jsonData)
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
                Console.WriteLine($"Error receiving WebSocket data: {ex.Message}");
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
                  // Check if the MachineName is uniform across the group.
                  bool machineNameUniform = g.All(x => x.MachineName == g.Key.MachineName);
                  bool soUniform = g.All(x => x.SO == g.Key.SO);
                  bool operatorUniform = g.All(x => x.OperatorName == g.Key.OperatorName);
                  // Calculate the total ActualCut for this group.
                  int totalActualCut = g.Sum(x => x.ActualCut);
                  int totalActualPieces = g.Sum(x => x.ActualPieces);
                  int totalActualSizeQty = g.Sum(x => x.ActualSizeQty);

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

                  // For each item in the group, compute its material and, if uniform, clear MachineName.
                  var items = g.Select(item =>
                  {
                      item.MaterialType = item.IsLeather ? "Leather Material" : "Raw Material";

                      // If all items have the same MachineName, clear it for non-header display.
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


            // Update the BindingDeviceOutputs in-place so that only changed cells update.
            UpdateBindingDeviceOutputs(groupedData);
        }

        /// <summary>
        /// Updates the BindingDeviceOutputs collection by comparing new data with the existing items.
        /// Only properties that differ are updated so that the UI refreshes only the changed cells.
        /// </summary>
        /// <param name="newData">The new list of DeviceOutput items.</param>
        private void UpdateBindingDeviceOutputs(IList<DeviceOutput> newData)
        {
            int minCount = Math.Min(BindingDeviceOutputs.Count, newData.Count);
            int i = 0;
            // Update existing items.
            for (; i < minCount; i++)
            {
                var existingItem = BindingDeviceOutputs[i];
                var newItem = newData[i];

            // Update the specified properties.
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
            // Add new items if there are more in newData.
            for (; i < newData.Count; i++)
            {
                BindingDeviceOutputs.Add(newData[i]);
            }
            // Remove extra items if the current list has more items than newData.
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
                            // Optionally log the error.
                            Console.WriteLine($"Error updating property '{propertyName}': {ex.Message}");
                            // Depending on your needs, you can choose to ignore or rethrow.
                            throw;
                        }
                    }
                }
            }
        }

        public class RealTimeData
        {
            //public int OrderID { get; set; }
            //public string MasterWorkOrder { get; set; }
            //public string SO { get; set; }
            //public string Model { get; set; }
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
