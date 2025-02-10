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
                                // Assuming EmployeeName, PositionID, and DepartmentID are returned in the query
                                string employeeName = reader.GetString(1);
                                int positionID = reader.GetInt32(2);
                                int departmentID = reader.GetInt32(3);
                                string role = determineUserRole(positionID, departmentID);
                                // Set the global user
                                Global.SetUser(employeeName, positionID, departmentID, role);

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
        private static string determineUserRole(int positionID, int departmentID)
        {
            // the role based on position
            if (positionID == 3)
            {
                return "Manager";
            }
            else
            {
                return "Employee";
            }
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
        //update user
        public static bool updateUser(string username, string employeeName, int employeeID, int departmentID, int positionID, bool isActive)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_UpdateUser", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@NewEmployeeID", employeeID);
                    cmd.Parameters.AddWithValue("@NewEmployeeName", employeeName);
                    cmd.Parameters.AddWithValue("@NewDepartmentID", departmentID);
                    cmd.Parameters.AddWithValue("@NewPositionID", positionID);
                    cmd.Parameters.AddWithValue("@NewIsActive", isActive);

                    // Add OUTPUT parameter to capture success/failure
                    SqlParameter outputParam = new SqlParameter("@UpdateStatus", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    // Retrieve output parameter value
                    int updateStatus = (int)cmd.Parameters["@UpdateStatus"].Value;

                    if (updateStatus == 1)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating user: " + ex.Message);
                return false;
            }
        }
        // delete user
        public static bool DeleteUser(string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL DELETE query
                    string query = "DELETE FROM Users WHERE Username = @Username";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        int rowsAffected = cmd.ExecuteNonQuery(); // Execute the query

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"User '{username}' deleted successfully.");
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting user: " + ex.Message);
                return false;
            }
        }
        // get list plant
        public static DataTable getPlants()
        {
            DataTable dt = new DataTable();
            SqlConnection con = new SqlConnection(connectionString);
            try
            {
                string query = "SELECT PlantID, PlantName FROM Plant";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
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
        public static bool dddNewDevice(int departmentId, int plantId, string ipAddress, string machineName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Check if IP Address already exists
                string checkQuery = "SELECT COUNT(*) FROM DeviceList WHERE IpAddress = @IpAddress";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        return false; // IP already exists, return false
                    }
                }

                // Insert new device if IP is unique
                string insertQuery = @"INSERT INTO DeviceList (DepartmentID, PlantID, IpAddress, MachineName, IsActive, ConnectionStatus) 
                           VALUES (@DepartmentID, @PlantID, @IpAddress, @MachineName, @IsActive, @ConnectionStatus);";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@DepartmentID", departmentId);
                    insertCmd.Parameters.AddWithValue("@PlantID", plantId);
                    insertCmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    insertCmd.Parameters.AddWithValue("@MachineName", machineName);
                    insertCmd.Parameters.AddWithValue("@IsActive", true);
                    insertCmd.Parameters.AddWithValue("@ConnectionStatus", true);

                    int rowsAffected = insertCmd.ExecuteNonQuery();
                    return rowsAffected > 0; // Return true if inserted successfully, otherwise false
                }
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

