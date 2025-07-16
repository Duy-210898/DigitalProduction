using System;
using System.ComponentModel;

namespace DigitalProduction.Models
{
    public class Device : INotifyPropertyChanged
    {
        public Device() { }
        private int deviceID;
        private string ipAddress;
        private string machineName;
        private bool connectionStatus;
        private bool isActive;
        private DateTime createdAt;
        private int departmentID;
        private string departmentName;
        private string plantName;
        private int plantID;

        public Device( string ipAddress, string machineName, int departmentID, int plantID)
        {
            this.ipAddress = ipAddress;
            this.machineName = machineName;
            this.departmentID = departmentID;
            this.plantID = plantID;
        }

        public int DeviceID { get => deviceID; set => deviceID = value; }
        public string IpAddress { get => ipAddress; set => ipAddress = value; }
        public string MachineName { get => machineName; set => machineName = value; }
        public bool ConnectionStatus { get => connectionStatus; set => connectionStatus = value; }
        public bool IsActive { get => isActive; set => isActive = value; }
        public DateTime CreatedAt { get => createdAt; set => createdAt = value; }
        public string PlantName { get => plantName; set => plantName = value; }
        public int PlantID { get => plantID; set => plantID = value; }
        public int DepartmentID { get => departmentID; set => departmentID = value; }
        public string DepartmentName { get => departmentName; set => departmentName = value; }


        // check cutting or not
        // ⏱️ Cutting logic
        private DateTime? lastCutTime;
        public DateTime? LastCutTime
        {
            get => lastCutTime;
            set
            {
                if (lastCutTime != value)
                {
                    lastCutTime = value;
                    OnPropertyChanged(nameof(LastCutTime));
                    UpdateIsCutting();
                }
            }
        }

        private bool isCutting;
        public bool IsCutting
        {
            get => isCutting;
            private set
            {
                if (isCutting != value)
                {
                    isCutting = value;
                    OnPropertyChanged(nameof(IsCutting));
                }
            }
        }

        // New properties to track cutting size
        private string _lastSize;
        public string LastSize
        {
            get => _lastSize;
            set
            {
                _lastSize = value;
                OnPropertyChanged(nameof(LastSize));
            }
        }

        private int? _lastCutQty;
        public int? LastCutQty
        {
            get => _lastCutQty;
            set
            {
                _lastCutQty = value;
                OnPropertyChanged(nameof(LastCutQty));
            }
        }
        private int? _lastSizeQty;
        public int? LastSizeQty
        {
            get => _lastSizeQty;
            set
            {
                _lastSizeQty = value;
                OnPropertyChanged(nameof(LastSizeQty));
            }
        }

        public void RefreshCuttingStatus()
        {
            if (LastCutTime.HasValue)
            {
                var elapsed = (DateTime.Now - LastCutTime.Value).TotalSeconds;
                IsCutting = elapsed <= 3;
            }
            else
            {
                IsCutting = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
        private void UpdateIsCutting()
        {
            if (LastCutTime.HasValue)
            {
                var secondsSinceLastCut = (DateTime.Now - LastCutTime.Value).TotalSeconds;
                IsCutting = secondsSinceLastCut <= 3;
            }
            else
            {
                IsCutting = false;
            }
        }
    }
}
