using System;

namespace DigitalProduction.Models
{
    public class CuttingReportModel
    {
        public string CreatedAt { get; set; }
        public string MachineName { get; set; }
        public string SO { get; set; }
       // public int OrderID { get; set; }
        public string OperatorName { get; set; }
        public int TotalActualCut { get; set; }
        public int TotalPieces { get; set; }
        public int TotalSizeQty { get; set; }
    }
}
