using static DevExpress.XtraEditors.Mask.MaskSettings;

namespace DigitalProduction.Models
{
    public class UserRole
    {
        public int UserID { get; set; }
        public string EmployeeName { get; set; }
        public int PositionID { get; set; }
        public int DepartmentID { get; set; }

        public string Role { get; set; }

        public UserRole(int userID, string employeeName, int positionID, int departmentID, string role)
        {
            UserID = userID;
            EmployeeName = employeeName;
            PositionID = positionID;
            DepartmentID = departmentID;
            Role = role;
        }
    }

}