namespace DigitalProduction.Models
{
    public class DeviceOutput
    {
        public string PartName { get; set; }
        public string Size { get; set; }
        public int SizeQty { get; set; }
        public int? PiecesPerPair { get; set; }
        public int? MaterialLayer { get; set; }
        public int? CuttingDieQty { get; set; }
        public int ActualCut { get; set; }
        public int ActualSizeQty { get; set; }
        public int ActualPieces { get; set; }
    }
}
