using System.Security.Cryptography;
using System.Text;

namespace AuctionSystem.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string CreateSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    public static string Hash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return Convert.ToBase64String(key);
    }

    public static bool Verify(string password, string salt, string hash)
    {
        if (string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            var computed = Hash(password, salt);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(hash));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
