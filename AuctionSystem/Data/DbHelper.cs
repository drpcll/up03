using System.Data;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Data;

public static class DbHelper
{
    public static bool TestConnection(string connStr, out string error)
    {
        try
        {
            using var connection = new SqlConnection(connStr);
            connection.Open();
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static DataTable Query(string connStr, string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(connStr);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        var table = new DataTable();
        connection.Open();
        using (var reader = command.ExecuteReader())
        {
            table.Load(reader);
        }

        return table;
    }

    public static int Execute(string connStr, string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(connStr);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        connection.Open();
        return command.ExecuteNonQuery();
    }

    public static object? Scalar(string connStr, string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(connStr);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        connection.Open();
        return command.ExecuteScalar();
    }

    public static int ExecuteInsert(string connStr, string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(connStr);
        using var command = new SqlCommand(sql + "; SELECT CAST(SCOPE_IDENTITY() AS int);", connection);
        command.Parameters.AddRange(parameters);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static SqlParameter Str(string name, string? value) =>
        new(name, SqlDbType.NVarChar) { Value = (object?)value ?? DBNull.Value };

    public static SqlParameter Num(string name, int value) =>
        new(name, SqlDbType.Int) { Value = value };

    public static SqlParameter Money(string name, decimal value) =>
        new(name, SqlDbType.Decimal) { Value = value };

    public static SqlParameter Bit(string name, bool value) =>
        new(name, SqlDbType.Bit) { Value = value };

    public static SqlParameter Date(string name, DateTime value) =>
        new(name, SqlDbType.Date) { Value = value.Date };

    public static SqlParameter NullableNum(string name, int? value) =>
        new(name, SqlDbType.Int) { Value = (object?)value ?? DBNull.Value };
}
