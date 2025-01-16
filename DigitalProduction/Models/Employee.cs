using System;

namespace DigitalProduction.Models
{
    public class Employee
    {
        private Employee() { }
        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
