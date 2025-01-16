using System;

namespace DigitalProduction.Models
{
    public class Device
    {
        private Device() { }
        private int deviceID;
        private string ipAddress;
        private string machineName;
        private bool connectionStatus;
        private bool isActive;
        private DateTime createdAt;
        private string plant;
        private int[] plantID;

        public int DeviceID { get => deviceID; set => deviceID = value; }
        public string IpAddress { get => ipAddress; set => ipAddress = value; }
        public string MachineName { get => machineName; set => machineName = value; }
        public bool ConnectionStatus { get => connectionStatus; set => connectionStatus = value; }
        public bool IsActive { get => isActive; set => isActive = value; }
        public DateTime CreatedAt { get => createdAt; set => createdAt = value; }
        public string Plant { get => plant; set => plant = value; }
        public int[] PlantID { get => plantID; set => plantID = value; }
    }
}
