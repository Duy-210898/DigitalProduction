namespace DigitalProduction.Models
{
    public class UserRole
    {
        public string EmployeeName { get; set; }
        public int PositionID { get; set; }
        public int DepartmentID { get; set; }

        public string Role { get; set; }

        public UserRole(string employeeName, int positionID, int departmentID, string role)
        {
            EmployeeName = employeeName;
            PositionID = positionID;
            DepartmentID = departmentID;
            Role = role;
        }
    }

}