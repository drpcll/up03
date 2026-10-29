namespace AuctionSystem.Models;

public sealed class AuthenticatedUser
{
    public required int Id { get; init; }
    public required string Login { get; init; }
    public required string FullName { get; init; }
    public required AccountType AccountType { get; init; }

    public UserRole? Role { get; init; }

    public string RoleName => Role switch
    {
        UserRole.Administrator => "Администратор",
        UserRole.AuctionAdministrator => "Администратор аукциона",
        UserRole.Seller => "Продавец",
        UserRole.Buyer => "Покупатель",
        _ => AccountType switch
        {
            AccountType.Seller => "Продавец",
            AccountType.Buyer => "Покупатель",
            _ => "Сотрудник"
        }
    };

    public string ShortName => $"{FullName} ({RoleName})";
}
