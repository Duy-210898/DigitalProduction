using System;
using System.Data;

namespace DigitalProduction.Extensions
{
    public static class SafeReader
    {
        public static string GetString(IDataReader reader, string column) =>
            reader[column] == DBNull.Value ? null : reader[column].ToString();

        public static int? GetNullableInt(IDataReader reader, string column) =>
            reader[column] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[column]);
    }
}
