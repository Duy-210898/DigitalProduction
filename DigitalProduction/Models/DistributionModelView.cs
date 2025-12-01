using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalProduction.Models
{
    public class DistributionModelView
    {
        public int DistributionID { get; set; }
        public string SO { get; set; }
        public int? DeviceID { get; set; }
        public string IpAddress { get; set; }
        public string MachineName { get; set; }
        public string PartName { get; set; }
        public string VietnameseName { get; set; }
        public string Size { get; set; }
        public string Unit { get; set; }
        // public double UnitUsage { get; set; }
        public int SizeQty { get; set; }
        public string MaterialName { get; set; }
        public string OperatorName { get; set; }
        public string EmployeeName { get; set; }
        public int? ActualSizeQty { get; set; }
        public int InventoryQty { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsLeather { get; set; }
        // New read-only property
        public string MaterialType => IsLeather ? Lang.LeatherMaterial : Lang.RawMaterial;
        public int? Note { get; set; }
        public string NoteReason
        {
            get
            {
                if (!Note.HasValue)
                    return string.Empty;

                switch (Note.Value)
                {
                    case 0:
                        return "1 - " + LocalizationManager.GetString("NotEnoughMaterials");
                    case 1:
                        return "2 - " + LocalizationManager.GetString("ChangeOfPlan");
                    case 2:
                        return "3 - " + LocalizationManager.GetString("ForgotToChooseSize");
                    default:
                        return string.Empty;
                }
            }
        }
    }
}
