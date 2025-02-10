using System;
using DevExpress.XtraSpellChecker.Parser;

namespace DigitalProduction.Models
{
    public class Employee
    {
        private Employee() { }

        public Employee(int employeeID, string username, string password, string employeeName, int positionID, int departmentID, DateTime? createdAt, DateTime? updatedAt, bool isActive)
        {
            EmployeeID = employeeID;
            Username = username;
            Password = password;
            EmployeeName = employeeName;
            DepartmentID = departmentID;
            PositionID = positionID;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            IsActive = isActive;
        }

        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmployeeName { get; set; }
        public int DepartmentID { get; set; }
        public int PositionID { get; set; }
        public string PositionName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
