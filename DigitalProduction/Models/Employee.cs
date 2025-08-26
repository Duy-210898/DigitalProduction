using System;
using System.Globalization;
using System.Text;

namespace DigitalProduction.Models
{
    public class Employee
    {
        public Employee() { }

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

        public Employee(string employeeName, int employeeID, string positionName, string departmentName, bool isActive)
        {
            EmployeeID = employeeID;
            OperatorName = employeeName;
            DepartmentName = departmentName;
            PositionName = positionName;
            IsActive = isActive;
        }

        public Employee(int operatorID, string operatorName, int employeeID, int positionID, int departmentID, bool isActive)
        {
            OperatorID = operatorID;
            EmployeeID = employeeID;
            OperatorName = operatorName;
            DepartmentID = departmentID;
            PositionID = positionID;
            IsActive = isActive;
        }
        public string OperatorNameUnaccented
        {
            get
            {
                return RemoveDiacritics(OperatorName);
            }
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Thay thế riêng ký tự Đ/đ
            text = text.Replace('Đ', 'D').Replace('đ', 'd');

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public int OperatorID { get; set; }
        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmployeeName { get; set; }
        public string OperatorName { get; set; }
        public int DepartmentID { get; set; }
        public int PositionID { get; set; }
        public string PositionName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
