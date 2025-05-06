using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraEditors;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using DigitalProduction.Validation;

namespace DigitalProduction
{
    public partial class ucRegisterOperator : UserControl
    {
        public event EventHandler ExitClicked;
      //  public event EventHandler<Employee> UserCreated;
        public ucRegisterOperator()
        {
            InitializeComponent();
            ApplyLocalization();
        }
        private void btn_submit_Click(object sender, EventArgs e)
        {
            if (!txt_employeeID.ValidateInput(ValidationType.NotEmptyString) ||
               !txt_employeeName.ValidateInput(ValidationType.NotEmptyString))
            {
                MessageBox.Show("Invalid input! Please correct the highlighted fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string employeeName = txt_employeeName.Text.Trim();
            int employeeID = int.Parse(txt_employeeID.Text.Trim());
            int departmentID = 0;
            int positionID = 0;
            if (Global.CurrentUser != null)
            {
                departmentID = Global.CurrentUser.DepartmentID;
                positionID = 1; // worker
            }

            // create operator
            bool result = false;
            result = DbHelper.createOperator(employeeName, employeeID, departmentID, positionID);
            if (result)
            {
                ShowMessage.ShowInfo($"Registration successful with {employeeName}!", "Success");
               // ExitClicked.Invoke(this, EventArgs.Empty);
                DataTable dtDepartment = DbHelper.getDepartments();
                DataTable dtPosition = DbHelper.getPositions();

                string departmentName = Extentions.getNameFromDataTable(dtDepartment, departmentID, "departmentID", "departmentName");
                string positionName = Extentions.getNameFromDataTable(dtPosition, positionID, "positionID", "positionName");

               // UserCreated.Invoke(this, new Employee(employeeName, employeeID, positionName, departmentName, true));
            }
            else
            {
                ShowMessage.ShowError($"Registration failed with {employeeName}!");
            }
        }
        private void btn_close_Click(object sender, EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }
        private void ApplyLocalization()
        {
            var controls = new Dictionary<Control, string>
            {
                { groupControlRegisterOperator, "RegisterOperator" },
                { lblEmployeeID, "EmployeeID" },
                { lblEmployeeName, "EmployeeName" },
                { btn_submit, "Submit" },
                { btn_close, "Cancle" }
            };

            foreach (var control in controls)
            {
                control.Key.Text = LocalizationManager.GetString(control.Value) + (control.Key is LabelControl ? ":" : "");
            }
        }
    }
}
