using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Models;

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
        // create operator
        public static bool createOperator(string employeeName, int employeeID, int departmentID, int positionID)
        {
            string query = "INSERT INTO Operator (OperatorName, EmployeeID, DepartmentID, PositionID, IsActive) " +
                  "VALUES (@OperatorName, @EmployeeID, @DepartmentID, @PositionID, @IsActive)";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Correctly set the command type to Text
                        cmd.CommandType = CommandType.Text;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@OperatorName", employeeName);
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        cmd.Parameters.AddWithValue("@DepartmentID", departmentID);
                        cmd.Parameters.AddWithValue("@PositionID", positionID);
                        cmd.Parameters.AddWithValue("@IsActive", true);

                        // Open the connection
                        con.Open();

                        // Execute the query and check for affected rows
                        int rowsAffected = cmd.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error: {ex.Message}");
                return false; // Indicate an error occurred
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false; // Indicate an error occurred
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
        public List<Device> getlistMachines()
        {
            List<Device> machines = new List<Device>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MachineName, DeviceID FROM DeviceList where IsActive = 1 AND ConnectionStatus = 1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Device device = new Device
                                {
                                    DeviceID = (int)reader["DeviceID"],
                                    MachineName = reader["MachineName"].ToString()
                                };
                                machines.Add(device);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }

            return machines;
        }
        // Get IPaddress from device
        public async Task<string> GetIPAddressByDeviceIDAsync(int deviceID)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT IPAddress FROM DeviceList WHERE IsActive = 1 AND DeviceID = @DeviceID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@DeviceID", SqlDbType.Int).Value = deviceID;

                    object result = await cmd.ExecuteScalarAsync(); // Chờ kết quả
                    return result?.ToString(); // Trả về IP nếu có, nếu không thì null
                }
            }
        }

        public List<Employee> getOperatorsByDepartment(int departmentID)
        {
            List<Employee> operators = new List<Employee>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Define the query with the parameter
                string query = "SELECT * FROM [CuttingProjectData].[dbo].[Operator] WHERE DepartmentID = @DepartmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add the parameter and set its value
                    command.Parameters.AddWithValue("@DepartmentID", departmentID);

                    // Execute the query and read the data
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Create a new Operator object and populate it
                            Employee operatorData = new Employee
                            {
                                EmployeeID = (int)reader["OperatorID"],
                                OperatorName = reader["OperatorName"].ToString()
                            };

                            // Add the operator to the list
                            operators.Add(operatorData);
                        }
                    }
                }
            }

            return operators;
        }
        // Method to get the PartSizeOrderId based on some condition (for example, PartId, SizeId, or OrderId)
        public int getPartSizeOrderId(int partId, int sizeId, int orderId)
        {
            int partSizeOrderId = -1;  // Default value if no match is found

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Define the query to retrieve PartSizeOrderId
                string query = "SELECT PartSizeOrderId FROM PartSizeOrder WHERE PartId = @PartId AND SizeId = @SizeId AND OrderId = @OrderId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameters to the query
                    command.Parameters.AddWithValue("@PartId", partId);
                    command.Parameters.AddWithValue("@SizeId", sizeId);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    // Execute the query and retrieve the PartSizeOrderId
                    var result = command.ExecuteScalar();

                    // If result is not null, convert to integer
                    if (result != DBNull.Value)
                    {
                        partSizeOrderId = Convert.ToInt32(result);
                    }
                }
            }

            return partSizeOrderId;
        }
        public int? GetPartSizeOrderId(int partId, int sizeId, int orderId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT PartSizeOrderId FROM PartSizeOrder WHERE PartId = @PartId AND SizeId = @SizeId AND OrderId = @OrderId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PartId", partId);
                    command.Parameters.AddWithValue("@SizeId", sizeId);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    connection.Open();
                    object result = command.ExecuteScalar();

                    return result != null ? (int?)result : null;
                }
            }
        }
        public int getProductIdByArt(string art)
        {
            if (string.IsNullOrEmpty(art))
            {
                throw new ArgumentException("ART value cannot be null or empty.", nameof(art));
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT ProductId FROM Product WHERE LTRIM(RTRIM(ART)) = LTRIM(RTRIM(@ART))"; 

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ART", art.Trim());
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : -1; 
                }
            }
        }

        public DefaultInfo getDefaultValueFromART(string art)
        {
            if (string.IsNullOrEmpty(art))
            {
                throw new ArgumentException("ART value cannot be null or empty.", nameof(art));
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                                SELECT TOP 1 d.* 
                                FROM DefaultInfo d 
                                LEFT JOIN Product pr ON pr.ProductId = d.ProductId 
                                WHERE pr.ART = @ART";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ART", art.Trim());
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DefaultInfo
                            {
                                PiecesPerPair = reader.GetInt32(reader.GetOrdinal("PiecesPerPair")),
                                CuttingDieQty = reader.GetInt32(reader.GetOrdinal("CuttingDieQty")),
                                MaterialLayer = reader.GetInt32(reader.GetOrdinal("MaterialLayer")),
                                TotalPiecesPerPair = reader.GetInt32(reader.GetOrdinal("TotalPiecesPerPair")),
                            };
                        }
                    }
                }
            }

            return null; // Return null if no data found
        }

        // Method to delete a record by DistributionID
        public static bool deleteDistribution(int distributionID)
        {
            // SQL DELETE query
            string query = "DELETE FROM DistributionData WHERE DistributionID = @DistributionID;";
            try
            {
                // Create a connection to the database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    // Create a SqlCommand with the DELETE query
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add the parameter for DistributionID
                        command.Parameters.Add(new SqlParameter("@DistributionID", SqlDbType.Int));
                        command.Parameters["@DistributionID"].Value = distributionID;

                        // Execute the DELETE command
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if the record was deleted successfully
                       return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
    }
}

