using System;
using static DigitalProduction.ucDistribution;
using System.Collections.Generic;
using System.Linq;

namespace DigitalProduction.Models
{
    public class ProductionSchedule
    {
        public ProductionSchedule() { }

        public int OrderID { get; set; }
        public int OperatorID { get; set; }
        public int DepartmentID { get; set; }
        public string GroupSO { get; set; }
        public string Factory { get; set; }
        public int DeviceID { get; set; }
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
        public int TargetCut { get; set; }
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
        public int? CutQuantity { get; set; } = 0;
        public string Status { get; set; }
        public List<OperatorInfo> AssignedOperators { get; set; } = new List<OperatorInfo>();
        public int ActualRemainingQuantity =>  Math.Max(0, (CutQuantity + InventoryQty ?? 0) - SizeQty);
        public int RemainingQuantity => (CutQuantity + InventoryQty ?? 0) - SizeQty;
        // "Complete" if ActualRemainingQuantity = 0
        public string StatusCode => TargetCut != 0 ? "Complete"
            : RemainingQuantity >= 0 ? "Complete" : "Pending";
    }
        public class ScheduleGroup
    {
        public string SO { get; set; }
        public List<SizeGroup> Sizes { get; set; }
    }

    public class SizeGroup
    {
        public string Size { get; set; }
        public List<ProductionSchedule> Details { get; set; }
        public int _ => Details != null && Details.Any()
             ? Details.Min(d => d.TargetCut)
             : 0;
    }

}
