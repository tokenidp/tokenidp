using System.Security.Cryptography;
using System.Text;

namespace TokenIDP.Core.OAuth.Security;

internal class TokenSecretGenerator
{
    /// <summary>
    /// Generates a secure 512-bit refresh token using non-allocating RNG APIs.
    /// </summary>
    public string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public byte[] HashToken(string rawToken)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
    }
}

