using System;
using System.Threading;
using System.Threading.Tasks;
using DigitalProduction.Models;

namespace DigitalProduction
{
    public static class DeviceOutputUpdater
    {
        public static void UpdateDeviceOutput(DeviceOutput target, DeviceOutput source)
        {
            bool hasChanged = false;

            void SetIfDifferent<T>(ref T field, T newValue, Action<string> notify, string propertyName)
            {
                if (!Equals(field, newValue))
                {
                    field = newValue;
                    notify(propertyName);
                    hasChanged = true;
                }
            }

            SetIfDifferent(ref target._actualCut, source.ActualCut, target.OnPropertyChanged, nameof(DeviceOutput.ActualCut));
            SetIfDifferent(ref target._actualPieces, source.ActualPieces, target.OnPropertyChanged, nameof(DeviceOutput.ActualPieces));
            SetIfDifferent(ref target._inventoryQty, source.InventoryQty, target.OnPropertyChanged, nameof(DeviceOutput.InventoryQty));
            SetIfDifferent(ref target._actualSizeQty, source.ActualSizeQty, target.OnPropertyChanged, nameof(DeviceOutput.ActualSizeQty));

            if (hasChanged)
            {
                target.IsRecentlyUpdated = true;

                var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    syncContext.Post(_ => target.IsRecentlyUpdated = false, null);
                });
            }
        }
    }
}
