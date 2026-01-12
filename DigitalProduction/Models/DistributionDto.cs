using System;

namespace DigitalProduction.Models
{
    public class DistributionDto
    {
        public int DistributionID { get; set; }

        public int? DeviceID { get; set; }

        public int PartSizeOrderID { get; set; }
        public int UserID { get; set; }
        public int PartID { get; set; }
        public int? OperatorID { get; set; }


        public int InventoryQty { get; set; }

        public int CuttingDieQty { get; set; }

        public int PiecesPerPair { get; set; }

        public int MaterialLayer { get; set; }

        public int TotalPiecesPerPair { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsLeather { get; set; }

        public bool IsAutoCutting { get; set; }

        public bool IsDelete { get; set; }

    }

}
