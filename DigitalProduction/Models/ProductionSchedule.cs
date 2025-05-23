using System;

namespace DigitalProduction.Models
{
    public class ProductionSchedule
    {
        public ProductionSchedule() { }

        public int OrderID { get; set; }
        public int DepartmentID { get; set; }
        public string GroupSO { get; set; }
        public string Factory { get; set; }
        public string SO { get; set; }
        public string PO { get; set; }
        public string MasterWorkOrder { get; set; }
        public string LastNo { get; set; }
        public string Process { get; set; }
        public int SizeID { get; set; }
        public string Size { get; set; }
        public string ART { get; set; }
        public string Model { get; set; }
        public int SizeQty { get; set; }
        public string PartSizeUnit { get; set; }
        public float UnitUsage { get; set; }
        public int MaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialUnit { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public string PartCode { get; set; }
        public string VietnameseName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? InventoryQty { get; set; } = 0;
        public int? PeicesPerPair { get; set; } = 0;
        public int? CuttingDieQty { get; set; } = 0;
        public int? MaterialLayer { get; set; } = 0;
        public int? TotalPiecesPerPair { get; set; } = 0;
        public string Status { get; set; }
    }

}
