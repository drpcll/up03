using System.Data;
using AuctionSystem.Data;
using AuctionSystem.Models;
using AuctionSystem.Security;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Services;

public static class AuthService
{




    public static bool TryLogin(string connStr, string login, string password,
        out AuthenticatedUser? user, out string error)
    {
        user = null;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            error = "Введите логин и пароль.";
            return false;
        }

        try
        {
            var employee = FindEmployee(connStr, login);
            if (employee is not null)
            {
                return CheckPassword(employee, password, out user, out error);
            }

            var seller = FindParty(connStr, login, "dbo.UP03_Prodavtsy", "IDProdavca");
            if (seller is not null)
            {
                return CheckPassword(seller, password, out user, out error, AccountType.Seller);
            }

            var buyer = FindParty(connStr, login, "dbo.UP03_Pokupateli", "IDPokupatelya");
            if (buyer is not null)
            {
                return CheckPassword(buyer, password, out user, out error, AccountType.Buyer);
            }

            error = "Пользователь с таким логином не найден.";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Ошибка при входе: {ex.Message}";
            return false;
        }
    }

    private static bool CheckPassword(PersonRecord record, string password,
        out AuthenticatedUser? user, out string error, AccountType type = AccountType.Employee)
    {
        if (!PasswordHasher.Verify(password, record.Salt, record.PasswordHash))
        {
            user = null;
            error = "Неверный пароль.";
            return false;
        }

        user = new AuthenticatedUser
        {
            Id = record.Id,
            Login = record.Login,
            FullName = record.FullName,
            AccountType = type,
            Role = type == AccountType.Employee && record.RoleId is int roleId
                ? (UserRole)roleId
                : null
        };

        error = string.Empty;
        return true;
    }

    private static PersonRecord? FindEmployee(string connStr, string login)
    {
        const string sql = """
            SELECT TOP 1
                   IDSotrudnika AS Id,
                   Familiya     AS Familiya,
                   Imya         AS Imya,
                   Otchestvo    AS Otchestvo,
                   Login        AS Login,
                   Parol        AS PasswordHash,
                   Sol          AS Salt,
                   IDRole       AS RoleId
            FROM dbo.UP03_Sotrudniki
            WHERE Login = @login
            """;

        return ReadPerson(DbHelper.Query(connStr, sql, DbHelper.Str("@login", login)));
    }

    private static PersonRecord? FindParty(string connStr, string login, string table, string idColumn)
    {
        var sql = $"""
            SELECT TOP 1
                   {idColumn} AS Id,
                   Familiya   AS Familiya,
                   Imya       AS Imya,
                   Otchestvo  AS Otchestvo,
                   Login      AS Login,
                   Parol      AS PasswordHash,
                   Sol        AS Salt
            FROM {table}
            WHERE Login = @login
            """;

        return ReadPerson(DbHelper.Query(connStr, sql, DbHelper.Str("@login", login)));
    }

    private static PersonRecord? ReadPerson(DataTable table)
    {
        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new PersonRecord
        {
            Id = Convert.ToInt32(row["Id"]),
            Login = row["Login"].ToString() ?? string.Empty,
            FullName = ComposeName(row),
            PasswordHash = row["PasswordHash"].ToString() ?? string.Empty,
            Salt = row["Salt"].ToString() ?? string.Empty,
            RoleId = row.Table.Columns.Contains("RoleId") && row["RoleId"] is not DBNull
                ? Convert.ToInt32(row["RoleId"])
                : null
        };
    }

    private static string ComposeName(DataRow row)
    {
        var parts = new[] { "Familiya", "Imya", "Otchestvo" }
            .Where(c => row.Table.Columns.Contains(c) && row[c] is not DBNull)
            .Select(c => row[c].ToString()!.Trim())
            .Where(s => s.Length > 0);

        return string.Join(' ', parts);
    }

    public static bool IsLoginFree(string connStr, string login)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM dbo.UP03_Sotrudniki WHERE Login = @login)
              + (SELECT COUNT(*) FROM dbo.UP03_Prodavtsy  WHERE Login = @login)
              + (SELECT COUNT(*) FROM dbo.UP03_Pokupateli WHERE Login = @login)
            """;

        return Convert.ToInt32(DbHelper.Scalar(connStr, sql, DbHelper.Str("@login", login))) == 0;
    }

    public static void RegisterEmployee(string connStr, string familiya, string imya, string? otchestvo,
        string login, string password, int roleId, string? telefon, string? email)
    {
        const string sql = """
            INSERT INTO dbo.UP03_Sotrudniki
                (Familiya, Imya, Otchestvo, Login, Parol, Sol, IDRole, Telefon, Email, DataPrioma)
            VALUES
                (@familiya, @imya, @otchestvo, @login, @parol, @sol, @role, @telefon, @email, CAST(GETDATE() AS date))
            """;

        var (hash, salt) = BuildPassword(password);
        DbHelper.Execute(connStr, sql,
            DbHelper.Str("@familiya", familiya),
            DbHelper.Str("@imya", imya),
            DbHelper.Str("@otchestvo", otchestvo),
            DbHelper.Str("@login", login),
            DbHelper.Str("@parol", hash),
            DbHelper.Str("@sol", salt),
            DbHelper.Num("@role", roleId),
            DbHelper.Str("@telefon", telefon),
            DbHelper.Str("@email", email));
    }

    public static void RegisterSeller(string connStr, string familiya, string imya, string? otchestvo,
        string login, string password, string? telefon, string? email, string? adres)
    {
        const string sql = """
            INSERT INTO dbo.UP03_Prodavtsy
                (Familiya, Imya, Otchestvo, Telefon, Email, Adres, Login, Parol, Sol, DataRegistracii)
            VALUES
                (@familiya, @imya, @otchestvo, @telefon, @email, @adres, @login, @parol, @sol, CAST(GETDATE() AS date))
            """;

        var (hash, salt) = BuildPassword(password);
        DbHelper.Execute(connStr, sql,
            DbHelper.Str("@familiya", familiya),
            DbHelper.Str("@imya", imya),
            DbHelper.Str("@otchestvo", otchestvo),
            DbHelper.Str("@telefon", telefon),
            DbHelper.Str("@email", email),
            DbHelper.Str("@adres", adres),
            DbHelper.Str("@login", login),
            DbHelper.Str("@parol", hash),
            DbHelper.Str("@sol", salt));
    }

    public static void RegisterBuyer(string connStr, string familiya, string imya, string? otchestvo,
        string login, string password, string? telefon, string? email, string? adres)
    {
        const string sql = """
            INSERT INTO dbo.UP03_Pokupateli
                (Familiya, Imya, Otchestvo, Telefon, Email, Adres, Login, Parol, Sol, DataRegistracii)
            VALUES
                (@familiya, @imya, @otchestvo, @telefon, @email, @adres, @login, @parol, @sol, CAST(GETDATE() AS date))
            """;

        var (hash, salt) = BuildPassword(password);
        DbHelper.Execute(connStr, sql,
            DbHelper.Str("@familiya", familiya),
            DbHelper.Str("@imya", imya),
            DbHelper.Str("@otchestvo", otchestvo),
            DbHelper.Str("@telefon", telefon),
            DbHelper.Str("@email", email),
            DbHelper.Str("@adres", adres),
            DbHelper.Str("@login", login),
            DbHelper.Str("@parol", hash),
            DbHelper.Str("@sol", salt));
    }

    private static (string Hash, string Salt) BuildPassword(string password)
    {
        var salt = PasswordHasher.CreateSalt();
        return (PasswordHasher.Hash(password, salt), salt);
    }

    private sealed class PersonRecord
    {
        public int Id { get; init; }
        public string Login { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string Salt { get; init; } = string.Empty;
        public int? RoleId { get; init; }
    }
}
