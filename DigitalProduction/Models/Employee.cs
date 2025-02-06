using System;

namespace DigitalProduction.Models
{
    public class Employee
    {
        private Employee() { }

        public Employee(int employeeID, string username, string password, string employeeName, int department, DateTime? createdAt, DateTime? updatedAt, bool isActive)
        {
            EmployeeID = employeeID;
            Username = username;
            Password = password;
            EmployeeName = employeeName;
            Department = department;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            IsActive = isActive;
        }

        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmployeeName { get; set; }
        public int Department { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
