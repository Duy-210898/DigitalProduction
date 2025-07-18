using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using DigitalProduction.Extensions;
using DigitalProduction.Models;
using static DigitalProduction.ucProgress;
using static DigitalProduction.ucReportOrder;

namespace DigitalProduction
{
    internal class DbHelper
    {
        private readonly static string connectionString;
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
        public static bool LoginUser(string username, string hashedPassword)
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
                                string role = determineUserRole(positionID);
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
        private static string determineUserRole(int positionID)
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
        public static List<Employee> getOperatorsByDepartment()
        {
            List<Employee> operators = new List<Employee>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Define the query with the parameter
                string query = "SELECT * FROM [CuttingProjectData].[dbo].[Operator]";

                using (SqlCommand command = new SqlCommand(query, connection))
                {

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
        public static List<Employee> getOperatorsByDepartment(int departmentID)
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
                                //OrderID = reader["OrderID"] != DBNull.Value
                                //    ? Convert.ToInt32(reader["OrderID"])
                                //    : 0,
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
        public static void UpdateInventoryQty(int distributionID, int newInventoryQty, string status)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL command to update the inventory quantity
                    string updateQuery = "UPDATE DistributionData SET InventoryQty = @InventoryQty, Status = @Status WHERE DistributionID = @DistributionID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        // Adding parameters to prevent SQL injection
                        cmd.Parameters.AddWithValue("@InventoryQty", newInventoryQty);
                        cmd.Parameters.AddWithValue("@DistributionID", distributionID);
                        cmd.Parameters.AddWithValue("@Status", status);

                        // Execute the query
                        int rowsAffected = cmd.ExecuteNonQuery();

                        // Check if the update was successful
                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Inventory quantity updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine("No rows updated, check if the DistributionID is valid.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any errors
                Console.WriteLine($"Error updating inventory quantity: {ex.Message}");
            }
        }

        public static void UpsertTargetInDay(
         DateTime targetDate,
         int departmentId,
         int productId,
         int targetQuantity,
         int employeeId)
        {
            string sql = @"
            IF EXISTS (
                SELECT 1 FROM TargetInDay 
                WHERE TargetDate = @TargetDate AND DepartmentId = @DepartmentId AND ProductId = @ProductId
            )
            BEGIN
                UPDATE TargetInDay
                SET 
                    TargetQuantity = @TargetQuantity,
                    EmployeeId = @EmployeeId,
                    UpdatedAt = GETDATE()
                WHERE TargetDate = @TargetDate AND DepartmentId = @DepartmentId AND ProductId = @ProductId;
            END
            ELSE
            BEGIN
                INSERT INTO TargetInDay (TargetDate, DepartmentId, ProductId, TargetQuantity, EmployeeId)
                VALUES (@TargetDate, @DepartmentId, @ProductId, @TargetQuantity, @EmployeeId);
            END";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.Add("@TargetDate", SqlDbType.Date).Value = targetDate;
                    cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;
                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                    cmd.Parameters.Add("@TargetQuantity", SqlDbType.Int).Value = targetQuantity;
                    cmd.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;

                    cmd.ExecuteNonQuery();
                }
            }
        }
        //public static async Task<List<TargetRealtimeInfo>> GetRealtimeTargetDataAsync(int month, int year)
        //{
        //    var list = new List<TargetRealtimeInfo>();

        //    string sql = @"
        //          SELECT 
        //            ISNULL(o.OperatorName, 'Unknown') AS OperatorName,
        //            CAST(ac.UpdatedAt AS DATE) AS Timestamp,
        //            o.OperatorID,
        //            CASE 
        //                WHEN CAST(t.TargetDate AS DATE) = CAST(ac.UpdatedAt AS DATE) THEN ISNULL(t.TargetQuantity, 0)
        //                ELSE 0
        //            END AS TargetQuantity,
        //            SUM(ac.ActualCut) AS TargetActualQuantity
        //        FROM DeviceOutput ac
        //        LEFT JOIN PartSizeOrder pso 
        //            ON ac.OrderID = pso.OrderId 
        //            AND ac.SizeID = pso.SizeId 
        //            AND ac.PartID = pso.PartId
        //        LEFT JOIN DistributionData dd 
        //            ON pso.PartSizeOrderId = dd.PartSizeOrderId 
        //        LEFT JOIN ProductOrder po 
        //            ON po.OrderId = ac.OrderId
        //        LEFT JOIN Operator o 
        //            ON o.OperatorID = dd.OperatorID

        //        -- OUTER APPLY: fetch latest target by Operator (EmployeeId)
        //        OUTER APPLY (
        //            SELECT TOP 1 *
        //            FROM TargetInDay tid
        //            WHERE 
        //                tid.EmployeeId = o.EmployeeID
        //                AND tid.TargetDate <= CAST(ac.UpdatedAt AS DATE)
        //            ORDER BY tid.TargetDate DESC
        //        ) t
        //        WHERE 
        //            YEAR(ac.UpdatedAt) = @Year
        //            AND MONTH(ac.UpdatedAt) = @Month
        //            AND o.OperatorID IS NOT NULL

        //        GROUP BY 
        //            ISNULL(o.OperatorName, 'Unknown'),
        //            CAST(ac.UpdatedAt AS DATE),
        //            t.TargetDate,
        //            o.OperatorID,
        //            t.TargetQuantity

        //        ORDER BY 
        //            CAST(ac.UpdatedAt AS DATE)
        //    ";
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    using (SqlCommand cmd = new SqlCommand(sql, conn))
        //    {
        //        cmd.Parameters.AddWithValue("@Month", month);
        //        cmd.Parameters.AddWithValue("@Year", year);

        //        await conn.OpenAsync();
        //        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //        {
        //            while (await reader.ReadAsync())
        //            {
        //                list.Add(new TargetRealtimeInfo
        //                {
        //                    OperatorID = Convert.ToInt32(reader["OperatorID"]),
        //                    OperatorName = reader["OperatorName"].ToString(),
        //                    Timestamp = Convert.ToDateTime(reader["Timestamp"]),
        //                    TargetQuantity = Convert.ToInt32(reader["TargetQuantity"]),
        //                    TargetActualQuantity = Convert.ToInt32(reader["TargetActualQuantity"]),
        //                });
        //            }
        //        }
        //    }

        //    return list;
        //}

        public static async Task<List<TargetRealtimeInfo>> GetRealtimeTargetDataAsync(int month, int year)
        {
            var list = new List<TargetRealtimeInfo>();

            string sql = @"
                SELECT 
                    ISNULL(o.OperatorName, 'Unknown') AS OperatorName,
                    ch.CutDate AS Timestamp,
                    o.OperatorID,
                    CASE 
                        WHEN CAST(t.TargetDate AS DATE) = ch.CutDate THEN ISNULL(t.TargetQuantity, 0)
                        ELSE 0
                    END AS TargetQuantity,
                    SUM(ch.CutQuantity) AS TargetActualQuantity
                FROM CutHistory ch
                LEFT JOIN PartSizeOrder pso 
                    ON ch.OrderID = pso.OrderId 
                    AND ch.SizeID = pso.SizeId 
                    AND ch.PartID = pso.PartId
                LEFT JOIN DistributionData dd 
                    ON pso.PartSizeOrderId = dd.PartSizeOrderId
                LEFT JOIN Operator o
                    ON o.OperatorID = dd.OperatorID

                OUTER APPLY (
                    SELECT TOP 1 *
                    FROM TargetInDay tid
                    WHERE 
                        tid.EmployeeId = o.EmployeeID
                        AND tid.TargetDate <= ch.CutDate
                    ORDER BY tid.TargetDate DESC
                ) t
                WHERE 
                    YEAR(ch.CutDate) = @Year
                    AND MONTH(ch.CutDate) = @Month
                    AND o.OperatorID IS NOT NULL

                GROUP BY 
                    ISNULL(o.OperatorName, 'Unknown'),
                    ch.CutDate,
                    t.TargetDate,
                    o.OperatorID,
                    t.TargetQuantity

                ORDER BY ch.CutDate;
            ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Month", month);
                cmd.Parameters.AddWithValue("@Year", year);

                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new TargetRealtimeInfo
                        {
                            OperatorID = Convert.ToInt32(reader["OperatorID"]),
                            OperatorName = reader["OperatorName"].ToString(),
                            Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                            TargetQuantity = Convert.ToInt32(reader["TargetQuantity"]),
                            TargetActualQuantity = Convert.ToInt32(reader["TargetActualQuantity"]),
                        });
                    }
                }
            }

            return list;
        }

        //public static async Task<List<TargetRealtimeInfo>> GetRealtimeTargetDataAsync(int month, int year)
        //{
        //    var list = new List<TargetRealtimeInfo>();

        //    string selectSql = @"
        //        SELECT 
        //            ISNULL(o.OperatorName, 'Unknown') AS OperatorName,
        //            CAST(ac.UpdatedAt AS DATE) AS Timestamp,
        //            o.OperatorID,
        //            CASE 
        //                WHEN CAST(t.TargetDate AS DATE) = CAST(ac.UpdatedAt AS DATE) THEN ISNULL(t.TargetQuantity, 0)
        //                ELSE 0
        //            END AS TargetQuantity,
        //            SUM(ac.ActualCut) AS TargetActualQuantity
        //        FROM DeviceOutput ac
        //        LEFT JOIN PartSizeOrder pso 
        //            ON ac.OrderID = pso.OrderId 
        //            AND ac.SizeID = pso.SizeId 
        //            AND ac.PartID = pso.PartId
        //        LEFT JOIN DistributionData dd 
        //            ON pso.PartSizeOrderId = dd.PartSizeOrderId 
        //        LEFT JOIN ProductOrder po 
        //            ON po.OrderId = ac.OrderId
        //        LEFT JOIN Operator o 
        //            ON o.OperatorID = dd.OperatorID
        //        OUTER APPLY (
        //            SELECT TOP 1 *
        //            FROM TargetInDay tid
        //            WHERE 
        //                tid.EmployeeId = o.EmployeeID
        //                AND tid.TargetDate <= CAST(ac.UpdatedAt AS DATE)
        //            ORDER BY tid.TargetDate DESC
        //        ) t
        //        WHERE 
        //            YEAR(ac.UpdatedAt) = @Year
        //            AND MONTH(ac.UpdatedAt) = @Month
        //            AND o.OperatorID IS NOT NULL
        //        GROUP BY 
        //            ISNULL(o.OperatorName, 'Unknown'),
        //            CAST(ac.UpdatedAt AS DATE),
        //            t.TargetDate,
        //            o.OperatorID,
        //            t.TargetQuantity
        //        ORDER BY CAST(ac.UpdatedAt AS DATE)
        //    ";

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        await conn.OpenAsync();

        //        using (SqlCommand cmd = new SqlCommand(selectSql, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@Month", month);
        //            cmd.Parameters.AddWithValue("@Year", year);

        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                while (await reader.ReadAsync())
        //                {
        //                    var info = new TargetRealtimeInfo
        //                    {
        //                        OperatorID = Convert.ToInt32(reader["OperatorID"]),
        //                        OperatorName = reader["OperatorName"].ToString(),
        //                        Timestamp = Convert.ToDateTime(reader["Timestamp"]),
        //                        TargetQuantity = Convert.ToInt32(reader["TargetQuantity"]),
        //                        TargetActualQuantity = Convert.ToInt32(reader["TargetActualQuantity"]),
        //                    };

        //                    list.Add(info);
        //                }
        //            }
        //        }

        //        // Loop and save each if quantity == 0 or not exists
        //        foreach (var item in list)
        //        {
        //            // Get EmployeeID using OperatorID
        //            var getEmpIdCommand = new SqlCommand("SELECT EmployeeID FROM dbo.Operator WHERE OperatorID = @OperatorID", conn);
        //            getEmpIdCommand.Parameters.Add("@OperatorID", SqlDbType.Int).Value = item.OperatorID;

        //            var employeeIdObj = await getEmpIdCommand.ExecuteScalarAsync();
        //            if (employeeIdObj == null)
        //            {
        //                throw new InvalidOperationException($"OperatorID {item.OperatorID} does not exist in the Operator table.");
        //            }

        //            int employeeId = Convert.ToInt32(employeeIdObj);
        //            string insertIfZeroSql = @"
        //                INSERT INTO TargetInDay (TargetDate, DepartmentId, TargetQuantity, EmployeeId)
        //                SELECT 
        //                    @TargetDate, 
        //                    @DepartmentID, 
        //                    @TargetActualQuantity, 
        //                    @EmployeeID
        //                WHERE NOT EXISTS (
        //                    SELECT 1 FROM TargetInDay tid
        //                    WHERE tid.EmployeeId = @EmployeeID
        //                      AND tid.TargetDate = @TargetDate
        //                      AND tid.TargetQuantity = 0
        //                )";

        //            using (SqlCommand saveCmd = new SqlCommand(insertIfZeroSql, conn))
        //            {
        //                saveCmd.Parameters.AddWithValue("@TargetDate", item.Timestamp);
        //                saveCmd.Parameters.AddWithValue("@DepartmentID", Global.CurrentUser.DepartmentID);
        //                saveCmd.Parameters.AddWithValue("@TargetActualQuantity", 0);
        //                saveCmd.Parameters.AddWithValue("@EmployeeID", employeeId);

        //                try
        //                {
        //                    await saveCmd.ExecuteNonQueryAsync();
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"Error saving target for OperatorID: {item.OperatorID}, Date: {item.Timestamp}");
        //                    Console.WriteLine($"Exception: {ex.Message}");
        //                }
        //            }
        //        }
        //    }
        //        return list;
        //}


        public static async Task SaveTargetQuantityAsync(TargetRealtimeInfo item)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Get EmployeeID using OperatorID
                var getEmpIdCommand = new SqlCommand("SELECT EmployeeID FROM dbo.Operator WHERE OperatorID = @OperatorID", connection);
                getEmpIdCommand.Parameters.Add("@OperatorID", SqlDbType.Int).Value = item.OperatorID;

                var employeeIdObj = await getEmpIdCommand.ExecuteScalarAsync();
                if (employeeIdObj == null)
                {
                    throw new InvalidOperationException($"OperatorID {item.OperatorID} does not exist in the Operator table.");
                }

                int employeeId = Convert.ToInt32(employeeIdObj);

                // Proceed with MERGE
                using (var command = new SqlCommand(@"
                    MERGE TargetInDay AS target
                    USING (
                        SELECT 
                            @TargetDate AS TargetDate,
                            @DepartmentId AS DepartmentId,
                            @EmployeeId AS EmployeeId
                    ) AS source
                    ON (
                        target.TargetDate = source.TargetDate
                        AND target.DepartmentId = source.DepartmentId 
                        AND target.EmployeeId = source.EmployeeId
                    )
                    WHEN MATCHED THEN
                        UPDATE SET 
                            TargetQuantity = @TargetQuantity,
                            UpdatedAt = GETDATE()
                    WHEN NOT MATCHED BY TARGET
                    THEN
                        INSERT (TargetDate, DepartmentId, EmployeeId, TargetQuantity, CreatedAt)
                        VALUES (@TargetDate, @DepartmentId, @EmployeeId, @TargetQuantity, GETDATE())
                    OUTPUT $action, inserted.*, deleted.*;", connection))
                {
                    command.Parameters.Add("@TargetDate", SqlDbType.Date).Value = item.Timestamp.Date;
                    command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = item.DepartmentId;
                    command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                    command.Parameters.Add("@TargetQuantity", SqlDbType.Int).Value = item.TargetQuantity;

                    await command.ExecuteNonQueryAsync();
                }

            }
        }

        public static void SaveFilteredSchedulesToDatabase(List<ProductionSchedule> schedules)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string query = @"
                        IF NOT EXISTS (
                            SELECT 1 FROM ProductionSchedule
                            WHERE SO = @SO AND PartID = @PartID AND SizeID = @SizeID AND OrderID = @OrderID
                        )
                        BEGIN
                            INSERT INTO ProductionSchedule 
                            (Factory, DepartmentID, ART, Model, PO, SO, MasterWorkOrder, SizeID, PartID, OrderID, 
                             MaterialCode, MaterialName, 
                             UNIT, ProductionProcess, LastNo, UnitUsage, CreatedAt)
                            VALUES 
                            (@Factory, @DepartmentID, @ART, @Model, @PO, @SO, @MasterWorkOrder, @SizeID, @PartID, @OrderID, 
                             @MaterialCode, @MaterialName, 
                             @UNIT, @ProductionProcess, @LastNo, @UnitUsage, @CreatedAt)
                        END";

                        foreach (var schedule in schedules)
                        {
                            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                            {
                                cmd.Parameters.Add("@Factory", SqlDbType.NVarChar).Value = (object)schedule.Factory ?? DBNull.Value;
                                cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = Global.CurrentUser?.DepartmentID ?? 0;
                                cmd.Parameters.Add("@ART", SqlDbType.NVarChar).Value = (object)schedule.ART ?? DBNull.Value;
                                cmd.Parameters.Add("@Model", SqlDbType.NVarChar).Value = (object)schedule.Model ?? DBNull.Value;
                                cmd.Parameters.Add("@PO", SqlDbType.NVarChar).Value = (object)schedule.PO ?? DBNull.Value;
                                cmd.Parameters.Add("@SO", SqlDbType.NVarChar).Value = (object)schedule.SO ?? DBNull.Value;
                                cmd.Parameters.Add("@MasterWorkOrder", SqlDbType.NVarChar).Value = (object)schedule.MasterWorkOrder ?? DBNull.Value;
                                cmd.Parameters.Add("@SizeID", SqlDbType.Int).Value = schedule.SizeID;
                                cmd.Parameters.Add("@PartID", SqlDbType.Int).Value = schedule.PartId;
                                cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = schedule.OrderID;
                                cmd.Parameters.Add("@MaterialCode", SqlDbType.NVarChar).Value = (object)schedule.MaterialCode ?? DBNull.Value;
                                cmd.Parameters.Add("@MaterialName", SqlDbType.NVarChar).Value = (object)schedule.MaterialName ?? DBNull.Value;
                                cmd.Parameters.Add("@UNIT", SqlDbType.NVarChar).Value = (object)schedule.PartSizeUnit ?? DBNull.Value;
                                cmd.Parameters.Add("@ProductionProcess", SqlDbType.NVarChar).Value = (object)schedule.Process ?? DBNull.Value;
                                cmd.Parameters.Add("@LastNo", SqlDbType.NVarChar).Value = (object)schedule.LastNo ?? DBNull.Value;
                                cmd.Parameters.Add("@UnitUsage", SqlDbType.Decimal).Value = schedule.UnitUsage;
                                cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = schedule.CreatedAt;

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        MessageBox.Show("Data saved to ProductionSchedule table successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error saving data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public static List<string> GetDistinctSOListByMonthAndDepartment(int month, int year, int departmentId)
        {
            var soList = new List<string>();
            string query = @"
                SELECT DISTINCT SO 
                FROM ProductionSchedule 
                WHERE SO IS NOT NULL AND SO <> ''
                AND MONTH(CreatedAt) = @Month 
                AND YEAR(CreatedAt) = @Year
                AND DepartmentID = @DepartmentID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@DepartmentID", departmentId);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            soList.Add(reader["SO"].ToString());
                        }
                    }
                }
            }

            return soList;
        }
        public static List<ProductionSchedule> GetSchedulesBySOList(List<string> soList)
        {
            var result = new List<ProductionSchedule>();

            if (soList == null || soList.Count == 0)
                return result;

            string inClause = string.Join(",", soList.Select((s, i) => $"@SO{i}"));
            string query = $@"
            SELECT 
                po.OrderID,
                po.Factory,
                po.SO,
                po.PO,
                po.MasterWorkOrder,
                po.LastNo,
                po.Process,
                s.Size,
                s.SizeID,
                p.ART,
                p.Model,
                pso.SizeQty,
                pso.Unit AS PartSizeUnit,
                pso.UnitUsage,
                m.MaterialID,
                m.MaterialCode,
                m.MaterialName,
                m.Unit AS MaterialUnit,
                pa.PartId,
                pa.PartName,
                pa.VietnameseName,
                pa.PartCode,
                ps.CreatedAt,
                d.Status,
                d.InventoryQty,
                do.CuttingDieQty, 
                do.PiecesPerPair, 
                do.MaterialLayer, 
                do.TotalPiecesPerPair
            FROM Product p
            JOIN ProductOrder po ON p.ProductId = po.ProductId
            JOIN PartSizeOrder pso ON po.OrderID = pso.OrderID
            JOIN Part pa ON pso.PartId = pa.PartId
            JOIN Material m ON pso.MaterialID = m.MaterialID
            JOIN Size s ON pso.SizeId = s.SizeID
            JOIN ProductionSchedule ps ON ps.OrderID = po.OrderID AND ps.PartID = pa.PartId AND ps.SizeID = s.SizeID
            LEFT JOIN DistributionData d ON pso.PartSizeOrderId = d.PartSizeOrderId
            LEFT JOIN DeviceOutput do ON do.SizeID = s.SizeId AND do.PartId = pa.PartId AND do.OrderID = po.OrderId
            WHERE po.SO IN ({inClause})
            ORDER BY TRY_CAST(s.Size AS DECIMAL(4,1));";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                for (int i = 0; i < soList.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@SO{i}", soList[i]);
                }

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ProductionSchedule
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            Factory = reader["Factory"].ToString(),
                            SO = reader["SO"].ToString(),
                            PO = reader["PO"].ToString(),
                            MasterWorkOrder = reader["MasterWorkOrder"].ToString(),
                            LastNo = reader["LastNo"].ToString(),
                            Process = reader["Process"].ToString(),
                            Size = reader["Size"].ToString(),
                            SizeID = Convert.ToInt32(reader["SizeID"]),
                            ART = reader["ART"].ToString(),
                            Model = reader["Model"].ToString(),
                            SizeQty = reader["SizeQty"] is int qty ? qty : 0,
                            PartSizeUnit = reader["PartSizeUnit"].ToString(),
                            MaterialID = Convert.ToInt32(reader["MaterialID"]),
                            MaterialCode = reader["MaterialCode"].ToString(),
                            MaterialName = reader["MaterialName"].ToString(),
                            MaterialUnit = reader["MaterialUnit"].ToString(),
                            PartId = Convert.ToInt32(reader["PartId"]),
                            PartName = reader["PartName"].ToString(),
                            VietnameseName = reader["VietnameseName"].ToString(),
                            PartCode = reader["PartCode"].ToString(),
                            CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.MinValue,
                            Status = reader["Status"]?.ToString(),
                            InventoryQty = reader["InventoryQty"] is int inv ? inv : 0,
                            CuttingDieQty = reader["CuttingDieQty"] is int die ? die : 0,
                            PeicesPerPair = reader["PiecesPerPair"] is int ppp ? ppp : 0,
                            MaterialLayer = reader["MaterialLayer"] is int ml ? ml : 0,
                            TotalPiecesPerPair = reader["TotalPiecesPerPair"] is int tpp ? tpp : 0
                        });
                    }
                }
            }

            return result;
        }

        public static List<Distribution> GetDistributionData(DateTime from, DateTime to, int deviceId, string so, string status)
        {
            List<Distribution> list = new List<Distribution>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    string query = @"
                       SELECT 
                             d.*, 
                             o.SO,
                             p.PartName, 
                             s.Size,
                             op.OperatorName,
                             u.Username,
                             dl.IpAddress,
                             dl.MachineName,
                             m.MaterialName,
                             pso.SizeQty,
                             pso.Unit
                         FROM DistributionData d
                         INNER JOIN PartSizeOrder pso ON d.PartSizeOrderId = pso.PartSizeOrderId
                         INNER JOIN ProductOrder o ON o.OrderID = pso.OrderId
                         INNER JOIN Part p ON p.PartId = pso.PartId
                         INNER JOIN Size s ON s.SizeID = pso.SizeId
                         LEFT JOIN SubDistribution sd ON d.DistributionID = sd.DistributionID
                         LEFT JOIN  DeviceList AS dl ON d.DeviceID = ISNULL(sd.DeviceID, dl.DeviceID)
                         LEFT JOIN Operator AS op ON op.OperatorID = ISNULL(sd.OperatorID, d.OperatorID)
                         LEFT JOIN Users u ON d.UserID = u.UserID
                         LEFT JOIN Material m ON m.MaterialID = pso.MaterialId
                         WHERE d.CreatedAt >= @From AND d.CreatedAt < @To
                        ";

                    if (!string.IsNullOrEmpty(status))
                    {
                        query += " AND d.Status = @Status";
                    }

                    if (!string.IsNullOrEmpty(so))
                    {
                        query += " AND o.SO = @SO";
                    }

                    if (deviceId != 0)
                    {
                        query += " AND d.DeviceID = @DeviceID";
                    }

                    query += " ORDER BY d.CreatedAt DESC";
                    cmd.CommandText = query;

                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);

                    if (!string.IsNullOrEmpty(status))
                        cmd.Parameters.AddWithValue("@Status", status);

                    if (!string.IsNullOrEmpty(so))
                        cmd.Parameters.AddWithValue("@SO", so);

                    if (deviceId != 0)
                        cmd.Parameters.AddWithValue("@DeviceID", deviceId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Distribution
                            {
                                DistributionID = Convert.ToInt32(reader["DistributionID"]),
                                DeviceID = SafeReader.GetNullableInt(reader, "DeviceID"),
                                IpAddress = reader["IpAddress"]?.ToString(),
                                MachineName = SafeReader.GetString(reader, "MachineName"),
                                Status = reader["Status"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                SO = reader["SO"].ToString(),
                                PartName = reader["PartName"].ToString(),
                                Size = reader["Size"].ToString(),
                                Unit = reader["Unit"]?.ToString(),
                                SizeQty = reader["SizeQty"] != DBNull.Value ? Convert.ToInt32(reader["SizeQty"]) : 0,
                                MaterialName = reader["MaterialName"]?.ToString(),
                                OperatorName = SafeReader.GetString(reader, "OperatorName"),
                                EmployeeName = reader["Username"]?.ToString(),
                                IsLeather = Convert.ToBoolean(reader["IsLeather"]),
                                Note = reader["Note"] != DBNull.Value ? Convert.ToInt32(reader["Note"]) : (int?)null,
                                UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : DateTime.MinValue,
                            });
                        }
                    }
                }
            }

            return list;
        }
        public static void DeleteDistributionById(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM DistributionData WHERE DistributionID = @ID";
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void DeleteDistributionByIds(List<int> distributionIds)
        {
            if (distributionIds == null || distributionIds.Count == 0) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string ids = string.Join(",", distributionIds);
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM DistributionData WHERE DistributionID IN ({ids})";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void DeleteDistributionAndDeviceOutput(List<int> distributionIds)
        {
            if (distributionIds == null || distributionIds.Count == 0) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                foreach (int distId in distributionIds)
                {
                    int? partId = 0, sizeId = 0, orderId = 0, operatorId = 0;

                    // 1. Get full info from joined DistributionData + PartSizeOrder
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT 
                                pso.PartID, 
                                pso.SizeID, 
                                pso.OrderID, 
                                ISNULL(
                                    (
                                        SELECT TOP 1 sd.OperatorID
                                        FROM SubDistribution sd
                                        WHERE sd.DistributionID = dd.DistributionID
                                        ORDER BY sd.SubDistributionID DESC
                                    ),
                                    dd.OperatorID
                                ) AS OperatorID
                            FROM DistributionData dd
                            JOIN PartSizeOrder pso ON dd.PartSizeOrderID = pso.PartSizeOrderID
                            WHERE dd.DistributionID = @DistID";
                        cmd.Parameters.AddWithValue("@DistID", distId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                partId = Convert.ToInt32(reader["PartID"]);
                                sizeId = Convert.ToInt32(reader["SizeID"]);
                                orderId = Convert.ToInt32(reader["OrderID"]);
                                operatorId = SafeReader.GetNullableInt(reader, "OperatorID");
                            }
                        }
                    }

                    // 2. Delete DeviceOutput if valid IDs
                    if (partId != 0 && sizeId != 0 && orderId != 0 && operatorId != null)
                    {
                        // get ID of operator
                        operatorId = GetEmployeeIdByOperatorId(conn, (int)operatorId);

                        using (SqlCommand deleteOutput = conn.CreateCommand())
                        {
                            deleteOutput.CommandText = @"
                                DELETE FROM DeviceOutput
                                WHERE PartID = @PartID AND SizeID = @SizeID AND OrderID = @OrderID AND OperatorID = @OperatorID";
                            deleteOutput.Parameters.AddWithValue("@PartID", partId);
                            deleteOutput.Parameters.AddWithValue("@SizeID", sizeId);
                            deleteOutput.Parameters.AddWithValue("@OrderID", orderId);
                            deleteOutput.Parameters.AddWithValue("@OperatorID", operatorId);
                            deleteOutput.ExecuteNonQuery();
                        }
                    }

                    // 3. Optionally delete SubDistribution first (if FK exists)
                    using (SqlCommand deleteSub = conn.CreateCommand())
                    {
                        deleteSub.CommandText = "DELETE FROM SubDistribution WHERE DistributionID = @DistID";
                        deleteSub.Parameters.AddWithValue("@DistID", distId);
                        deleteSub.ExecuteNonQuery();
                    }

                    // 4. Delete DistributionData
                    using (SqlCommand deleteDist = conn.CreateCommand())
                    {
                        deleteDist.CommandText = "DELETE FROM DistributionData WHERE DistributionID = @DistID";
                        deleteDist.Parameters.AddWithValue("@DistID", distId);
                        deleteDist.ExecuteNonQuery();
                    }
                }
            }
        }

        public static int GetEmployeeIdByOperatorId(SqlConnection conn, int operatorId)
        {
            int employeeId = 0;

            using (SqlCommand getEmployeeCmd = conn.CreateCommand())
            {
                getEmployeeCmd.CommandText = @"SELECT EmployeeID FROM Operator WHERE OperatorID = @OperatorID";
                getEmployeeCmd.Parameters.AddWithValue("@OperatorID", operatorId);

                var result = getEmployeeCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    employeeId = Convert.ToInt32(result);
                }
            }

            return employeeId;
        }

        public static async Task<bool> HasSubDistributions(int distributionID)
        {
            try
            {
                string query = @"
                    SELECT COUNT(1) AS SubCount
                    FROM SubDistribution
                    WHERE DistributionID = @DistributionID
                    AND IsDelete = 0";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@DistributionID", SqlDbType.Int).Value = distributionID;

                        var result = await command.ExecuteScalarAsync();

                        int subCount = Convert.ToInt32(result);
                        return subCount > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Error checking for subdistributions: {ex.Message}");
                return false;
            }
        }
        public static async Task<List<SubDistribution>> GetSubDistributions(int distributionID)
        {
            var result = new List<SubDistribution>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT 
                            sd.SubDistributionID,
                            sd.DistributionID,
                            sd.PartSizeOrderId,
                            sd.Status,
                            sd.CreatedAt,
                            o.OperatorName,
                            sd.SizeQty,
                            ISNULL(sd.InventoryQty, dd.InventoryQty) AS InventoryQty,
                            dl.MachineName
                        FROM SubDistribution sd
                        JOIN PartSizeOrder ps ON sd.PartSizeOrderId = ps.PartSizeOrderId
                        JOIN DistributionData dd ON sd.DistributionID = dd.DistributionID
                        LEFT JOIN Operator o ON o.OperatorID = sd.OperatorID
                        LEFT JOIN DeviceList dl ON sd.DeviceID = dl.DeviceID
                        WHERE sd.IsDelete = 0
                          AND sd.DistributionID = @DistributionID";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DistributionID", distributionID);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var sub = new SubDistribution
                                {
                                    SubDistributionID = reader.GetInt32(0),
                                    DistributionID = reader.GetInt32(1),
                                    PartSizeOrderId = reader.GetInt32(2),
                                    Status = reader.GetString(3),
                                    CreatedAt = reader.GetDateTime(4),

                                    // Extended info
                                    OperatorName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    SizeQty = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                    InventoryQty = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                    MachineName = reader.IsDBNull(8) ? null : reader.GetString(8)
                                };

                                result.Add(sub);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetSubDistributions: {ex.Message}");
                throw;
            }

            return result;
        }
        public static bool IsPendingNotSentForIp(string ipAddress)
        {
            string query = @"
                SELECT 1
                FROM DistributionData dd
                LEFT JOIN SubDistribution sd ON sd.DistributionID = dd.DistributionID
                LEFT JOIN DeviceList dl ON dl.DeviceID = ISNULL(sd.DeviceID, dd.DeviceID)
                WHERE 
                    dd.IsDelete = 0
                    AND ISNULL(sd.Status, dd.Status) = 'Pending'
                    AND dl.IpAddress = @IpAddress
                    AND NOT EXISTS (
                        SELECT 1 FROM DeviceOutput do
                        WHERE do.OrderID = dd.PartSizeOrderId
            )";

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IpAddress", ipAddress);

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Error checking pending for IP {ipAddress}: {ex.Message}");
                    return false;
                }
            }
        }
        public static List<CutActivityInfo> GetRecentDeviceCutActivity(int seconds)
        {
            var result = new List<CutActivityInfo>();
            DateTime threshold = DateTime.Now.AddSeconds(-seconds);

            string query = @"
                WITH RankedCuts AS (
                    SELECT 
                        dd.DeviceID,
                        se.Size,
                        do.ActualCut,
                        pso.SizeQty,
                        do.UpdatedAt,
                        ROW_NUMBER() OVER (PARTITION BY dd.DeviceID ORDER BY do.UpdatedAt DESC) AS RowNum
                    FROM DeviceOutput do
                    JOIN PartSizeOrder pso 
                        ON do.OrderID = pso.OrderId 
                        AND do.PartID = pso.PartId 
                        AND do.SizeID = pso.SizeID
                    JOIN DistributionData dd 
                        ON dd.PartSizeOrderId = pso.PartSizeOrderId
                    JOIN Size se 
                        ON se.SizeID = pso.SizeID
                    JOIN DeviceList dv 
                        ON dv.DeviceID = dd.DeviceID
                    WHERE  dd.DeviceID IS NOT NULL
                      AND dv.ConnectionStatus = 1
                )
                SELECT 
                    DeviceID,
                    Size,
                    ActualCut,
                    SizeQty,
                    UpdatedAt
                FROM RankedCuts
                WHERE RowNum = 1
                ORDER BY UpdatedAt DESC;";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CutActivityInfo
                        {
                            DeviceID = reader.GetInt32(0),
                            Size = reader.IsDBNull(1) ? null : reader.GetString(1),
                            ActualCut = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            SizeQty = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            UpdatedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                        });
                    }
                }
            }

            return result;
        }

    }
}

