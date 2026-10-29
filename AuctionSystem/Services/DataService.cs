using System.Collections.Generic;
using System.Data;
using System.Linq;
using AuctionSystem.Data;
using AuctionSystem.Models;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Services;

public static class DataService
{
    public static DataTable LoadSection(SectionDescriptor section, string search)
    {
        var sql = $"SELECT * FROM {section.Table} t";
        var where = new List<string>();
        var parameters = new List<SqlParameter>
        {
            new("@uid", SqlDbType.Int) { Value = Session.CurrentUser?.Id ?? 0 }
        };

        if (!string.IsNullOrWhiteSpace(section.RowFilter))
        {
            where.Add("(" + section.RowFilter + ")");
        }

        if (!string.IsNullOrWhiteSpace(search) && section.SearchColumns.Length > 0)
        {
            var parts = section.SearchColumns.Select(c => $"t.{c} LIKE @search");
            where.Add("(" + string.Join(" OR ", parts) + ")");
            parameters.Add(new SqlParameter("@search", SqlDbType.NVarChar) { Value = "%" + search.Trim() + "%" });
        }

        if (where.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", where);
        }

        if (!string.IsNullOrWhiteSpace(section.OrderBy))
        {
            sql += " ORDER BY " + section.OrderBy;
        }

        return DbHelper.Query(AppConnection.String, sql, parameters.ToArray());
    }

    public static List<ComboItem> LoadCombo(string sql)
    {
        var table = DbHelper.Query(AppConnection.String, sql);
        var items = new List<ComboItem>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new ComboItem
            {
                Value = row["Value"],
                Display = row["Display"]?.ToString() ?? string.Empty
            });
        }

        return items;
    }

    public static Dictionary<string, object?> GetRow(SectionDescriptor section, object id)
    {
        var sql = $"SELECT * FROM {section.Table} WHERE {section.IdColumn} = @id";
        var table = DbHelper.Query(AppConnection.String, sql, new SqlParameter("@id", id));
        var result = new Dictionary<string, object?>();
        if (table.Rows.Count == 0)
        {
            return result;
        }

        var row = table.Rows[0];
        foreach (DataColumn column in table.Columns)
        {
            result[column.ColumnName] = row[column] is DBNull ? null : row[column];
        }

        return result;
    }

    public static int Insert(SectionDescriptor section, IDictionary<string, object?> values)
    {
        var prepared = PrepareValues(section, values);
        var fields = GetPersistedFields(section, prepared);

        var columns = string.Join(", ", fields.Select(f => f.Column));
        var names = string.Join(", ", fields.Select(f => "@" + f.Column));
        var sql = $"INSERT INTO {section.Table} ({columns}) VALUES ({names})";

        var parameters = fields.Select(f => BuildParameter(f, prepared[f.Column])).ToArray();
        return DbHelper.ExecuteInsert(AppConnection.String, sql, parameters);
    }

    public static void Update(SectionDescriptor section, object id, IDictionary<string, object?> values)
    {
        var prepared = PrepareValues(section, values);
        var fields = GetPersistedFields(section, prepared);

        var assignments = string.Join(", ", fields.Select(f => $"{f.Column} = @{f.Column}"));
        var sql = $"UPDATE {section.Table} SET {assignments} WHERE {section.IdColumn} = @__id";

        var parameters = fields.Select(f => BuildParameter(f, prepared[f.Column])).ToList();
        parameters.Add(new SqlParameter("@__id", id));
        DbHelper.Execute(AppConnection.String, sql, parameters.ToArray());
    }

    private static List<FieldDescriptor> GetPersistedFields(SectionDescriptor section, IDictionary<string, object?> values) =>
        section.Fields
            .Where(f => f.Editable && !f.Identity && values.ContainsKey(f.Column))
            .ToList();

    private static Dictionary<string, object?> PrepareValues(SectionDescriptor section, IDictionary<string, object?> values)
    {
        var prepared = new Dictionary<string, object?>(values);
        var passwordField = section.Fields.FirstOrDefault(f => f.IsPassword);
        if (passwordField is not null && prepared.ContainsKey(passwordField.Column))
        {
            var password = prepared[passwordField.Column] as string;
            prepared.Remove(passwordField.Column);

            if (!string.IsNullOrEmpty(password))
            {
                var salt = Security.PasswordHasher.CreateSalt();
                prepared["Parol"] = Security.PasswordHasher.Hash(password, salt);
                prepared["Sol"] = salt;
            }
        }

        return prepared;
    }

    public static void Delete(SectionDescriptor section, object id)
    {
        var sql = $"DELETE FROM {section.Table} WHERE {section.IdColumn} = @id";
        DbHelper.Execute(AppConnection.String, sql, new SqlParameter("@id", id));
    }

    public static object? Scalar(string sql, params SqlParameter[] parameters) =>
        DbHelper.Scalar(AppConnection.String, sql, parameters);

    public static DataTable Query(string sql, params SqlParameter[] parameters) =>
        DbHelper.Query(AppConnection.String, sql, parameters);

    private static SqlParameter BuildParameter(FieldDescriptor field, object? value)
    {
        var parameter = new SqlParameter("@" + field.Column, value ?? DBNull.Value);
        parameter.SqlDbType = field.Kind switch
        {
            FieldKind.Int => SqlDbType.Int,
            FieldKind.Decimal => SqlDbType.Decimal,
            FieldKind.Date => SqlDbType.Date,
            FieldKind.Time => SqlDbType.Time,
            FieldKind.Bool => SqlDbType.Bit,
            FieldKind.Combo => SqlDbType.Int,
            _ => SqlDbType.NVarChar
        };
        if (field.Kind is FieldKind.Decimal && value is decimal)
        {
            parameter.Precision = 12;
            parameter.Scale = 2;
        }

        return parameter;
    }
}
