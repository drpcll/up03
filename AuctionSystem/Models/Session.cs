namespace AuctionSystem.Models;

public static class Session
{
    public static AuthenticatedUser? CurrentUser { get; private set; }

    public static bool IsSignedIn => CurrentUser is not null;

    public static bool IsAdministrator =>
        CurrentUser?.Role == UserRole.Administrator;

    public static bool IsAuctionAdministrator =>
        CurrentUser?.Role == UserRole.AuctionAdministrator;

    public static bool IsSeller =>
        CurrentUser is { AccountType: AccountType.Seller };

    public static bool IsBuyer =>
        CurrentUser is { AccountType: AccountType.Buyer };

    public static void SignIn(AuthenticatedUser user) => CurrentUser = user;

    public static void SignOut() => CurrentUser = null;

    public static readonly (string Title, AccountType[] Accounts, UserRole[] Roles)[] Sections =
    {
        ("Аукционы",          new[] { AccountType.Employee, AccountType.Seller, AccountType.Buyer }, new[] { UserRole.Administrator, UserRole.AuctionAdministrator, UserRole.Seller, UserRole.Buyer }),
        ("Лоты",              new[] { AccountType.Employee, AccountType.Seller, AccountType.Buyer }, new[] { UserRole.Administrator, UserRole.AuctionAdministrator, UserRole.Seller, UserRole.Buyer }),
        ("Участники",         new[] { AccountType.Employee, AccountType.Seller, AccountType.Buyer }, new[] { UserRole.Administrator, UserRole.AuctionAdministrator, UserRole.Seller, UserRole.Buyer }),
        ("Результаты торгов", new[] { AccountType.Employee, AccountType.Buyer },  new[] { UserRole.Administrator, UserRole.AuctionAdministrator, UserRole.Buyer }),
        ("Отчёты",            new[] { AccountType.Employee, AccountType.Seller },  new[] { UserRole.Administrator, UserRole.AuctionAdministrator, UserRole.Seller }),
        ("Справочники",       new[] { AccountType.Employee },                     new[] { UserRole.Administrator }),
        ("Пользователи",      new[] { AccountType.Employee },                     new[] { UserRole.Administrator }),
    };

    public static IEnumerable<string> AvailableSections()
    {
        if (CurrentUser is null)
        {
            return Sections.Select(s => s.Title);
        }

        return Sections
            .Where(s => s.Accounts.Contains(CurrentUser.AccountType))
            .Where(s => CurrentUser.Role is null || s.Roles.Contains(CurrentUser.Role.Value))
            .Select(s => s.Title);
    }
}
