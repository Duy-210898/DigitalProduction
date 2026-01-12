namespace DigitalProduction.Models
{
    public class DeviceOutputSaveDto
    {
        public int OperatorID { get; set; }
        public int PartID { get; set; }
        public int SizeID { get; set; }
        public int OrderID { get; set; }

        public int PiecesPerPair { get; set; }
        public int MaterialLayer { get; set; }
        public int CuttingDieQty { get; set; }

        public int ActualCut { get; set; }
        public int ActualPieces { get; set; }
        public int ActualSizeQty { get; set; }
        public int InventoryQty { get; set; }
        public int TotalPiecesPerPair { get; set; }
        public bool IsLeather { get; set; }
    }

}
