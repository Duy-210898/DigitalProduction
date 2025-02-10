using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace DigitalProduction
{
    internal class DbHelper
    {
        private static string connectionString;

        static DbHelper()
        {
            // Lấy chuỗi kết nối từ file cấu hình
            connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;
        }

        // Ví dụ về phương thức để thực hiện một câu lệnh SQL
        public void ExecuteQuery(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
        }
        // login
        public static bool loginUser(string username, string hashedPassword)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Create the command to call the stored procedure
                    using (SqlCommand cmd = new SqlCommand("sp_LoginUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters for the username and hashed password
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);

                        // Execute the stored procedure
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();
                                // Assuming UserID, EmployeeName, PositionID, and DepartmentID are returned in the query
                                //int userID = reader.GetInt32(0);
                                //string employeeName = reader.GetString(1);
                                //int positionID = reader.GetInt32(2);
                                //int departmentID = reader.GetInt32(3);

                                return true;
                                // You can now use the userID, positionID, departmentID, etc., for further actions
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }
        // create user
        public static bool createUser(string username, string hashedPassword, string employeeName, int employeeID, int departmentID, int positionID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("RegisterUser", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        cmd.Parameters.AddWithValue("@EmployeeName", employeeName);
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        cmd.Parameters.AddWithValue("@DepartmentID", departmentID);
                        cmd.Parameters.AddWithValue("@PositionID", positionID);
                        cmd.Parameters.AddWithValue("@IsActive", true);

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return true;
                    }
                    con.Close();
                }
            } catch (Exception ex) {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
            return false;
        }
        // get list department
        public static DataTable getDepartments()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    string query = "SELECT DepartmentID, DepartmentName FROM Department WHERE IsActive = 1";
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                return dt;
            }
        }
        public static List<string> GetSOList()
        {
            List<string> soList = new List<string>(); 

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    string query = "SELECT DISTINCT SO FROM PRODUCTORDER";
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        soList.Add(reader["SO"].ToString());
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            return soList;
        }

        //get list position
        public static DataTable getPositions()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    string query = "SELECT PositionID, PositionName FROM Position";
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            return dt;
        }
        public string GetIpAddress(string machineName)
        {
            string ipAddress = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT IpAddress FROM DeviceList WHERE MachineName = @MachineName";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MachineName", machineName);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ipAddress = reader["IpAddress"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
            return ipAddress;
        }
        // Phương thức để lấy danh sách các MachineName từ bảng DeviceList
        public List<string> GetMachineNames()
        {
            List<string> machineNames = new List<string>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MachineName FROM DeviceList";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                machineNames.Add(reader["MachineName"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }

            return machineNames;
        }
    }
}

