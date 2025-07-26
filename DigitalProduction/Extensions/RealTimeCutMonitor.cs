using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace DigitalProduction.Extensions
{
    public class RealTimeCutMonitor
    {
        private Timer _pollTimer;
        private Dictionary<int, DateTime> _lastDeviceUpdatedAt = new Dictionary<int, DateTime>();

        public event Action<List<CutActivityInfo>> OnNewActiveDevices;
        public RealTimeCutMonitor()
        {
            _pollTimer = new Timer(3000); // 3 seconds
            _pollTimer.Elapsed += async (s, e) => CheckCutActivity();
            _pollTimer.AutoReset = true;
        }

        public void Start() => _pollTimer.Start();
        public void Stop() => _pollTimer.Stop();

        private void CheckCutActivity()
        {
            List<CutActivityInfo> recent = DbHelper.GetRecentDeviceCutActivity(3);
            var updatedDevices = new List<CutActivityInfo>();

            foreach (var device in recent)
            {
                if (!device.UpdatedAt.HasValue)
                    continue;

                if (!_lastDeviceUpdatedAt.TryGetValue(device.DeviceID, out var lastTime) ||
                    device.UpdatedAt.Value > lastTime)
                {
                    updatedDevices.Add(device);
                    _lastDeviceUpdatedAt[device.DeviceID] = device.UpdatedAt.Value;
                }
            }
            if (updatedDevices.Any())
            {
                OnNewActiveDevices?.Invoke(updatedDevices);
            }
        }

    }
    public class CutActivityInfo
    {
        public int DeviceID { get; set; }
        public string Size { get; set; }
        public int? ActualSizeQty { get; set; }
        public int? SizeQty { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
