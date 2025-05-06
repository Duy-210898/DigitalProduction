using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using DigitalProduction.Models;
using Newtonsoft.Json.Linq;

public class SqlDataImporter
{
    private const string ConnectionString = "Server=10.30.0.116;Database=CuttingProjectData;UserId=sa;Password=12345;TrustServerCertificate=True;Encrypt=False;";
    private const string ApiUrl = "http://10.30.0.36:3100/getDataCuttingBySO?so=1000236604,1000236579";

    public async Task RunAsync()
    {
        var data = await FetchDataAsync();
        if (data == null || data.Count == 0)
        {
            Console.WriteLine("No data to insert.");
            return;
        }

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();

            foreach (var item in data)
            {
                var productId = await GetOrInsertAsync(connection, "Product", "ProductID",
                    "SELECT ProductID FROM Product WHERE ART = @ART",
                    "INSERT INTO Product (ART, Model) OUTPUT INSERTED.ProductID VALUES (@ART, @Model)",
                    new SqlParameter("@ART", item.ART),
                    new SqlParameter("@Model", item.Model)
                );

                var materialId = await GetOrInsertAsync(connection, "Material", "MaterialID",
                    "SELECT MaterialID FROM Material WHERE MaterialCode = @MaterialCode",
                    "INSERT INTO Material (MaterialCode, MaterialName) OUTPUT INSERTED.MaterialID VALUES (@MaterialCode, @MaterialName)",
                    new SqlParameter("@MaterialCode", item.MaterialsID),
                    new SqlParameter("@MaterialName", item.MaterialsName)
                );

                var orderId = await GetOrInsertAsync(connection, "ProductOrder", "OrderID",
                    "SELECT OrderID FROM ProductOrder WHERE Factory = @Factory AND SO = @SO AND PO = @PO",
                    "INSERT INTO ProductOrder (ProductId, Factory, SO, PO, MasterWorkOrder, LastNo, Process) OUTPUT INSERTED.OrderID VALUES (@ProductId, @Factory, @SO, @PO, @MasterWorkOrder, @LastNo, @Process)",
                    new SqlParameter("@ProductId", productId),
                    new SqlParameter("@Factory", item.Factory),
                    new SqlParameter("@SO", item.SO),
                    new SqlParameter("@PO", item.PO),
                    new SqlParameter("@MasterWorkOrder", item.MasterWorkOrder),
                    new SqlParameter("@LastNo", item.LastNo),
                    new SqlParameter("@Process", item.ProductionProcess)
                );

                var sizeId = await GetOrInsertAsync(connection, "Size", "SizeID",
                    "SELECT SizeID FROM Size WHERE Size = @Size",
                    "INSERT INTO Size (Size) OUTPUT INSERTED.SizeID VALUES (@Size)",
                    new SqlParameter("@Size", item.Size)
                );

                var partId = await GetOrInsertAsync(connection, "Part", "PartID",
                    "SELECT PartID FROM Part WHERE PartCode = @PartCode AND PartName = @PartName",
                    "INSERT INTO Part (PartName, PartCode) OUTPUT INSERTED.PartID VALUES (@PartName, @PartCode)",
                    new SqlParameter("@PartName", item.PartName),
                    new SqlParameter("@PartCode", item.PartId)
                );

                // Check if PartSizeOrder already exists
                using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM PartSizeOrder WHERE PartId = @PartId AND OrderId = @OrderId AND SizeId = @SizeId", connection))
                {
                    checkCmd.Parameters.AddWithValue("@PartId", partId);
                    checkCmd.Parameters.AddWithValue("@OrderId", orderId);
                    checkCmd.Parameters.AddWithValue("@SizeId", sizeId);

                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        using (var insertCmd = new SqlCommand(
                            "INSERT INTO PartSizeOrder (MaterialId, PartId, OrderId, SizeId, SizeQty, Unit, UnitUsage) VALUES (@MaterialId, @PartId, @OrderId, @SizeId, @SizeQty, @Unit, @UnitUsage)", connection))
                        {
                            insertCmd.Parameters.AddWithValue("@MaterialId", materialId);
                            insertCmd.Parameters.AddWithValue("@PartId", partId);
                            insertCmd.Parameters.AddWithValue("@OrderId", orderId);
                            insertCmd.Parameters.AddWithValue("@SizeId", sizeId);
                            insertCmd.Parameters.AddWithValue("@SizeQty", item.SizeQty);
                            insertCmd.Parameters.AddWithValue("@Unit", item.Unit);
                            insertCmd.Parameters.AddWithValue("@UnitUsage", item.UnitUsage);
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            Console.WriteLine("Data successfully inserted.");
        }
    }

    private async Task<int> GetOrInsertAsync(SqlConnection connection, string table, string idField, string selectQuery, string insertQuery, params SqlParameter[] parameters)
    {
        using (var cmd = new SqlCommand(selectQuery, connection))
        {
            cmd.Parameters.AddRange(parameters);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                return Convert.ToInt32(result);
        }

        using (var cmd = new SqlCommand(insertQuery, connection))
        {
            cmd.Parameters.AddRange(parameters);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
    }

    private async Task<List<PartItem>> FetchDataAsync()
    {
        try
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                var response = await client.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var root = JObject.Parse(json);
                var dataArray = root["data"] as JArray;

                var list = new List<PartItem>();
                foreach (var item in dataArray)
                {
                    list.Add(new PartItem
                    {
                        ART = item["ART"]?.ToString(),
                        Model = item["Model"]?.ToString(),
                        MaterialsID = item["Materials ID"]?.ToString(),
                        MaterialsName = item["Materials Name"]?.ToString(),
                        Factory = item["Factory"]?.ToString(),
                        SO = item["SO"]?.ToString(),
                        PO = item["PO"]?.ToString(),
                        MasterWorkOrder = item["Master Work Order"]?.ToString(),
                        LastNo = item["Last No"]?.ToString(),
                        ProductionProcess = item["Production Process"]?.ToString(),
                        Size = item["Size"]?.ToString(),
                        PartId = item["Part Id"]?.ToString(),
                        PartName = item["Part Name"]?.ToString(),
                        SizeQty = item["Size Qty"]?.ToObject<int>() ?? 0,
                        Unit = item["UNIT"]?.ToString(),
                        UnitUsage = item["Unit usage"]?.ToObject<double>() ?? 0
                    });
                }

                return list;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
            return null;
        }
    }
}
