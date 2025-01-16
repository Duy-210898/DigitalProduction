namespace DigitalProduction.Models
{
    public class ProductionSchedule
    {
        private ProductionSchedule() { }
        public string MasterWorkOrder { get; set; }
        public string PartId { get; set; }
        public string PartName { get; set; }
        public string MaterialsId { get; set; }
        public string MaterialsName { get; set; }
        public int SizeQty { get; set; }
        public string Factory { get; set; }
        public string ART { get; set; }
        public string Model { get; set; }
        public string PO { get; set; }
        public string SO { get; set; }
        public string Size { get; set; }
        public string LastNo { get; set; }
        public float UnitUsage { get; set; }
        public string ProductionProcess { get; set; }
    }
}
