using System;

namespace DigitalProduction.Models
{
    public class Device
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
    }
}
