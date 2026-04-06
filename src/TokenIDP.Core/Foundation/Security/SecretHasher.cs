using System.Security.Cryptography;
using System.Text;

namespace TokenIDP.Core.Foundation.Security;

public class SecretHasher
{
    /// <summary>
    /// Hashes a secret (e.g., client secret, API key) using SHA-256 and Base64 encoding.
    /// Store only the hash in persistence.
    /// </summary>
    public static string HashSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret must not be null or empty.", nameof(secret));

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(secret);
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Constant-time comparison for secrets/tokens.
    /// </summary>
    public static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return left.Length == right.Length &&
               CryptographicOperations.FixedTimeEquals(left, right);
    }
}
