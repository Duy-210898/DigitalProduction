using System;
using System.Data;
using System.Windows.Forms;
using DigitalProduction.Models;

namespace DigitalProduction
{
    public partial class ucRegisterDevice : UserControl
    {
        public event EventHandler ExitClicked;
        public event EventHandler<Device> DeviceCreated;
        public ucRegisterDevice()
        {
            InitializeComponent();
            loadDepartments();
            loadPlants();
        }
        private void loadDepartments()
        {
            DataTable dt = DbHelper.getDepartments();

            cb_department.Properties.DataSource = dt;
            cb_department.Properties.DisplayMember = "DepartmentName";
            cb_department.Properties.ValueMember = "DepartmentID";

            cb_department.Properties.NullText = "Select a Department";
        }
        private void loadPlants()
        {
            DataTable dt = DbHelper.getPlants();

            cb_plant.Properties.DataSource = dt;
            cb_plant.Properties.DisplayMember = "PlantName";
            cb_plant.Properties.ValueMember = "PlantID";

            cb_plant.Properties.NullText = "Select a Plant";
        }

        private void btn_close_Click(object sender, System.EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btn_submit_Click(object sender, EventArgs e)
        {
            int departmentID = 0;
            if (cb_department.EditValue != null)
            {
                departmentID = Convert.ToInt32(cb_department.EditValue);
            }
            int plantId = 0;
            if (cb_plant.EditValue != null)
            {
                plantId = Convert.ToInt32(cb_plant.EditValue);
            }
            string ipAddress = txt_addressIP.Text.Trim();
            string machineName = txt_machineName.Text.Trim();

            bool statusAddDevice = DbHelper.dddNewDevice(departmentID, plantId, ipAddress, machineName);
            Console.WriteLine(statusAddDevice ? "Device added successfully!" : "Failed to add device (Duplicate IP or error).");
            if (statusAddDevice)
            {
                ShowMessage.ShowInfo($"Added new device: {ipAddress}!", "Success");
                DeviceCreated.Invoke(this, new Device(ipAddress, machineName, departmentID, plantId));
            }
            else
            {
                ShowMessage.ShowError($"Failed new device: {ipAddress}!");
            }
        }
    }
}