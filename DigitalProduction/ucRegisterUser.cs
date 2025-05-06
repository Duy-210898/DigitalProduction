using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DigitalProduction.Models;
using DigitalProduction.Validation;

namespace DigitalProduction
{
    public partial class ucRegisterUser : DevExpress.XtraEditors.XtraUserControl
    {
        public event EventHandler ExitClicked;
       // public event EventHandler<Employee> UserCreated;
        public ucRegisterUser()
        {
            InitializeComponent();
            LoadDepartments();
            LoadPositions();
            ApplyLocalization();
        }

        private void btn_close_Click(object sender, System.EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btn_submit_Click(object sender, EventArgs e)
        {
            if (!cb_department.ValidateInput(ValidationType.NotNull) ||
               !cb_position.ValidateInput(ValidationType.NotNull) ||
                !txt_username.ValidateInput(ValidationType.NotEmptyString) ||
                 !txt_pwd.ValidateInput(ValidationType.NotEmptyString) ||
                 !txt_employeeID.ValidateInput(ValidationType.NotEmptyString) ||
                !txt_employeeName.ValidateInput(ValidationType.NotEmptyString))
            {
                MessageBox.Show("Invalid input! Please correct the highlighted fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string username = txt_username.Text;
            string password = txt_pwd.Text;
            string hashedPassword = SecurityHelper.HashPassword(password);
            string employeeName = txt_employeeName.Text;
            int employeeID = Convert.ToInt32(txt_employeeID.Text);
            int departmentID = 0;
            if (cb_department.EditValue != null)
            {
                departmentID = Convert.ToInt32(cb_department.EditValue);
            }
            int positionID = 0;
            if (cb_position.EditValue != null)
            {
                positionID = Convert.ToInt32(cb_position.EditValue);
            }

            // create user
            bool checkRegisteruser = false;
            checkRegisteruser = DbHelper.createUser(username, hashedPassword, employeeName, employeeID, departmentID, positionID);
            if (checkRegisteruser)
            {
                ShowMessage.ShowInfo($"Registration successful with {username}!", "Success");
                //ExitClicked.Invoke(this, EventArgs.Empty);
                //UserCreated.Invoke(this, new Employee(employeeID, username, null, employeeName, positionID, departmentID, DateTime.Now, null, true));
            }
            else
            {
                ShowMessage.ShowError($"Registration failed with {username}!");
            }
        }
        private void LoadDepartments()
        {
            DataTable dt = DbHelper.getDepartments();

            cb_department.Properties.DataSource = dt;
            cb_department.Properties.DisplayMember = "DepartmentName";
            cb_department.Properties.ValueMember = "DepartmentID";

            cb_department.Properties.NullText = LocalizationManager.GetString("SelectDepartment");

            //if (dt.Rows.Count > 0)
            //    cb_department.EditValue = dt.Rows[0]["DepartmentID"]; // Auto-select first item
        }
        private void LoadPositions()
        {
            DataTable dt = DbHelper.getPositions();

            cb_position.Properties.DataSource = dt;
            cb_position.Properties.DisplayMember = "PositionName";
            cb_position.Properties.ValueMember = "PositionID";

            cb_position.Properties.NullText = LocalizationManager.GetString("SelectPosition");
        }

        private void ApplyLocalization()
        {
            var controls = new Dictionary<Control, string>
            {
                { groupControlRegisterUser, "RegisterUser" },
                { lblUserName, "Username" },
                { lblPassword, "Password" },
                { lblEmployeeID, "EmployeeID" },
                { lblEmployeeName, "EmployeeName" },
                { lblDepartment, "Department" },
                { lblPosition, "Position" },
                { btn_submit, "Submit" },
                { btn_close, "Cancle" }
            };

            foreach (var control in controls)
            {
                string localizedText = LocalizationManager.GetString(control.Value)?.TrimEnd();

                if (control.Key is LabelControl)
                {
                    // Add colon only if it doesn't already end with ":"
                    if (!localizedText.EndsWith(":"))
                    {
                        localizedText += ":";
                    }
                }

                control.Key.Text = localizedText;
            }
        }
    }
}
