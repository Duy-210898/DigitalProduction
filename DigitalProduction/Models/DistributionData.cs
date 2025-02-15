using System;
using System.Collections.Generic;

namespace DigitalProduction.Models
{
    //public class DistributionData
    //{
    //    public DistributionData() { }
    //    public string MasterWorkOrder { get; set; }
    //    public string SO { get; set; }
    //    public string Model { get; set; }
    //    public string ART { get; set; }
    //    public List<SizeData> SizeData { get; set; }
    //    public List<MaterialData> MaterialData { get; set; }
    //    public string User { get; set; }
    //    public string IpAddress { get; set; }
    //}

    public class DistributionData
    {
        public int DeviceID { get; set; }

        public int PartSizeOrderID { get; set; }

        public int OperatorID { get; set; }

        public int InventoryQty { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsLeather { get; set; }

        public bool IsDelete { get; set; }
    }

    public class SizeData
    {
        public int SizeID { get; set; }
        public string Size { get; set; }
        public int SizeQty { get; set; } 
        public float UnitUsage { get; set; }
        private float _totalUsage;

        public float TotalUsage
        {
            get => _totalUsage; // Get the calculated total usage
            set
            {
                _totalUsage = value;
                 _totalUsage = SizeQty * UnitUsage;
            }
        }
        public bool SelectSize { get; set; }
    }

    public class MaterialData
    {
        public int PartID { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string VietnameseName { get; set; }
        public string Unit { get; set; }
        public bool IsSelected { get; set; }
    }


    public class Response
    {
        public string Action { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
    }
    public class ScheduleResponse
    {
        public string Action { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public Device[] Devices { get; set; }
        public Employee[] Employee { get; set; }
        public List<int> Pages { get; set; }
        public List<ProductionSchedule> Schedule { get; set; }
    }
}
