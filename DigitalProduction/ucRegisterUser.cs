using System;
using System.Data;
using System.Windows.Forms;
using DigitalProduction.Models;

namespace DigitalProduction
{
    public partial class ucRegisterUser : DevExpress.XtraEditors.XtraUserControl
    {
        public event EventHandler ExitClicked;
        public event EventHandler<Employee> UserCreated;
        public ucRegisterUser()
        {
            InitializeComponent();
            LoadDepartments();
            LoadPositions();
        }

        private void btn_close_Click(object sender, System.EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btn_submit_Click(object sender, EventArgs e)
        {
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
                MessageBox.Show("Registration successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ExitClicked.Invoke(this, EventArgs.Empty);
                UserCreated.Invoke(this, new Employee(employeeID, username, hashedPassword, employeeName, departmentID, DateTime.Now, null, true));
            }
            else
            {
                MessageBox.Show("Registration failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadDepartments()
        {
            DataTable dt = DbHelper.getDepartments();

            cb_department.Properties.DataSource = dt;
            cb_department.Properties.DisplayMember = "DepartmentName";
            cb_department.Properties.ValueMember = "DepartmentID";

            cb_department.Properties.NullText = "Select a Department";

            //if (dt.Rows.Count > 0)
            //    cb_department.EditValue = dt.Rows[0]["DepartmentID"]; // Auto-select first item
        }
        private void LoadPositions()
        {
            DataTable dt = DbHelper.getPositions();

            cb_position.Properties.DataSource = dt;
            cb_position.Properties.DisplayMember = "PositionName";
            cb_position.Properties.ValueMember = "PositionID";

            cb_position.Properties.NullText = "Select a Position";
        }
    }
}
