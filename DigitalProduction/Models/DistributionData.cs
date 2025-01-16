using System.Collections.Generic;

namespace DigitalProduction.Models
{
    public class DistributionData
    {
        public DistributionData() { }
        public string MasterWorkOrder { get; set; }
        public string SO { get; set; }
        public string Model { get; set; }
        public string ART { get; set; }
        public List<SizeData> SizeData { get; set; }
        public List<MaterialData> MaterialData { get; set; }
        public string User { get; set; }
        public string IpAddress { get; set; }
    }

    public class SizeData
    {
        public string Size { get; set; }
        public int SizeQty { get; set; }
    }

    public class MaterialData
    {
        public string PartName { get; set; }
        public string MaterialsName { get; set; }
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

        public List<int> Pages { get; set; }
        public List<ProductionSchedule> Schedule { get; set; }
    }
}
