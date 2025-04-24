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
        private static DateTime _lastCheckTime = DateTime.MinValue;

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
                                int userID = reader.GetInt32(0);
                                string employeeName = reader.GetString(1);
                                int positionID = reader.GetInt32(2);
                                int departmentID = reader.GetInt32(3);
                                string role = determineUserRole(positionID, departmentID);
                                // Set the global user
                                Global.SetUser(userID, employeeName, positionID, departmentID, role);

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
            }
            catch (Exception ex)
            {
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
        public static bool updateOperator(Employee updatedOperator)
        {
            try
            {
                string query = @"
                UPDATE Operator
                SET 
                    DepartmentID = @DepartmentID,
                    PositionID = @PositionID,
                    OperatorName = @OperatorName,
                    EmployeeID = @EmployeeID,
                    IsActive = @IsActive
                WHERE OperatorID = @OperatorID";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OperatorID", updatedOperator.OperatorID);
                        cmd.Parameters.AddWithValue("@DepartmentID", updatedOperator.DepartmentID);
                        cmd.Parameters.AddWithValue("@PositionID", updatedOperator.PositionID);
                        cmd.Parameters.AddWithValue("@OperatorName", updatedOperator.OperatorName);
                        cmd.Parameters.AddWithValue("@EmployeeID", updatedOperator.EmployeeID);
                        cmd.Parameters.AddWithValue("@IsActive", updatedOperator.IsActive);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
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
                    string query = "SELECT DISTINCT SO FROM ProductOrder WHERE OrderID NOT IN ( SELECT DISTINCT OrderID FROM DeviceOutput WHERE OrderID IN ( SELECT OrderID FROM DeviceOutput WHERE IsLeather IN (0, 1) GROUP BY OrderID HAVING COUNT(DISTINCT IsLeather) = 2 ) );";
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
        public static List<Device> getlistMachines()
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
        // Update device
        public static bool updateDevice(int deviceId, int departmentId, int plantId, string ipAddress, string machineName, bool isActive, bool connectionStatus)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                    UPDATE DeviceList 
                    SET 
                        DepartmentID = @DepartmentID, 
                        PlantID = @PlantID, 
                        IpAddress = @IpAddress, 
                        MachineName = @MachineName, 
                        IsActive = @IsActive, 
                        ConnectionStatus = @ConnectionStatus 
                    WHERE DeviceID = @DeviceID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add parameters to prevent SQL injection
                        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = departmentId;
                        cmd.Parameters.Add("@PlantID", SqlDbType.Int).Value = plantId;
                        cmd.Parameters.Add("@IpAddress", SqlDbType.VarChar, 50).Value = ipAddress;
                        cmd.Parameters.Add("@MachineName", SqlDbType.VarChar, 100).Value = machineName;
                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
                        cmd.Parameters.Add("@ConnectionStatus", SqlDbType.Bit).Value = connectionStatus;
                        cmd.Parameters.Add("@DeviceID", SqlDbType.Int).Value = deviceId;

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();

                        return rowsAffected > 0; // Return true if update was successful
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }

        public static DataTable reportOrderbyOperator(int year, int month)
        {
            DataTable dt = new DataTable();
            string query = @"
                    SELECT 
                        d.CreatedAt,
                        o.EmployeeID, 
                        o.OperatorName, 
                        COUNT(DISTINCT ps.OrderID) AS OrderCount 
                    FROM DistributionData d 
                    JOIN PartSizeOrder ps ON ps.PartSizeOrderId = d.PartSizeOrderId 
                    JOIN Operator o ON d.OperatorID = o.OperatorID 
                    WHERE d.Status = 'Complete'
                      AND YEAR(d.CreatedAt) = @Year
                      AND MONTH(d.CreatedAt) = @Month
                    GROUP BY o.EmployeeID, o.OperatorName, d.CreatedAt
                    ORDER BY OrderCount DESC;";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return dt;
        }

        public static List<CuttingReportModel> getProductionSummary(int year, int month)
        {
            List<CuttingReportModel> summaryList = new List<CuttingReportModel>();

            string query = @"
                SELECT 
                    ac.CreatedAt,
                    dl.MachineName,
                    p.SO,
                    ac.OrderID,
                    o.OperatorName,
                    SUM(DISTINCT ac.ActualCut) AS TotalActualCut,
                    SUM(DISTINCT ac.ActualPieces) AS TotalPieces,
                    SUM(DISTINCT ac.ActualSizeQty) AS TotalSizeQty
                FROM 
                    DeviceOutput ac
                LEFT JOIN PartSizeOrder pso ON pso.OrderId = ac.OrderID AND pso.SizeId = ac.SizeID
                JOIN ProductOrder p ON p.OrderID = pso.OrderId
                JOIN DistributionData dt ON dt.PartSizeOrderId = pso.PartSizeOrderId
                JOIN DeviceList dl ON dl.DeviceID = dt.DeviceID
                JOIN Operator o ON dt.OperatorID = o.OperatorID
                WHERE 
                    YEAR(ac.CreatedAt) = @Year AND 
                    MONTH(p.CreatedAt) = @Month
                GROUP BY 
                    ac.CreatedAt,
                    dl.MachineName,
                    p.SO,
                    ac.OrderID, 
                    o.OperatorName;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            summaryList.Add(new CuttingReportModel
                            {
                                CreatedAt = reader["CreatedAt"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["CreatedAt"]).ToString("MM/dd/yyyy")
                                    : "N/A",
                                MachineName = reader["MachineName"] != DBNull.Value
                                    ? reader["MachineName"].ToString()
                                    : string.Empty,
                                SO = reader["SO"] != DBNull.Value
                                    ? reader["SO"].ToString()
                                    : string.Empty,
                                OrderID = reader["OrderID"] != DBNull.Value
                                    ? Convert.ToInt32(reader["OrderID"])
                                    : 0,
                                OperatorName = reader["OperatorName"] != DBNull.Value
                                    ? reader["OperatorName"].ToString()
                                    : string.Empty,
                                TotalActualCut = reader["TotalActualCut"] != DBNull.Value
                                    ? Convert.ToInt32(reader["TotalActualCut"])
                                    : 0,
                                TotalPieces = reader["TotalPieces"] != DBNull.Value
                                    ? Convert.ToInt32(reader["TotalPieces"])
                                    : 0,
                                TotalSizeQty = reader["TotalSizeQty"] != DBNull.Value
                                    ? Convert.ToInt32(reader["TotalSizeQty"])
                                    : 0
                            });
                        }
                    }
                }
            }
            return summaryList;
        }
        public static bool CheckLogin(string username, string hashedPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT IsActive FROM dbo.Users WHERE Username=@Username AND Password=@Password", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool isActive = reader.GetBoolean(0);
                            return isActive;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        public static bool IsNewPasswordValid(string newPassword)
        {
            // Độ dài tối thiểu
            if (newPassword.Length < 5)
            {
                return false;
            }
            return true;
        }
        public static bool ChangePassword(string username, string currentPassword, string newPassword)
        {
            if (!CheckLogin(username, SecurityHelper. ComputeHash(currentPassword)))
            {
                return false;
            }

            if (!IsNewPasswordValid(newPassword))
            {
                return false;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UPDATE dbo.Users SET Password=@Password WHERE Username=@Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", SecurityHelper.ComputeHash(newPassword));

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        public static async Task<bool> CheckForSqlUpdates()
        {
            // Câu truy vấn để lấy timestamp mới nhất trong bảng DeviceOutputs
            string query = "SELECT MAX(UpdatedAt) FROM DeviceOutput";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    await conn.OpenAsync(); // Mở kết nối đến cơ sở dữ liệu

                    // Thực thi truy vấn và lấy kết quả
                    var result = await cmd.ExecuteScalarAsync();

                    // Kiểm tra giá trị trả về
                    if (result != DBNull.Value && result != null)
                    {
                        DateTime latestTimestamp = Convert.ToDateTime(result);

                        // So sánh timestamp mới nhất với thời gian đã lưu
                        if (latestTimestamp > _lastCheckTime)
                        {
                            _lastCheckTime = latestTimestamp; // Cập nhật thời gian kiểm tra mới nhất
                            return true; // Trả về true nếu có thay đổi
                        }
                    }

                    return false; // Trả về false nếu không có thay đổi
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking SQL updates: {ex.Message}");
                    return false; // Xử lý lỗi và trả về false
                }
            }
        }
        public static bool UpdateDistributionNoteAndStatus(int distributionId, int note, string status)
        {
            string query = @"
            UPDATE DistributionData
            SET Note = @Note, Status = @Status
            WHERE DistributionID = @DistributionID;
            ";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Note", note);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@DistributionID", distributionId);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                // Log or handle error as needed
                Console.WriteLine("Error updating distribution: " + ex.Message);
                return false;
            }
        }
        public static bool UpdateDistributionDevice(int distributionId, int deviceId)
        {
            string query = @"
                    UPDATE DistributionData
                    SET DeviceID = @DeviceID
                    WHERE DistributionID = @DistributionID AND Status = 'Pending'; -- Ensure only pending rows are affected
                ";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DeviceID", deviceId);
                    command.Parameters.AddWithValue("@DistributionID", distributionId);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating DeviceID: " + ex.Message);
                return false;
            }
        }
    }
}

