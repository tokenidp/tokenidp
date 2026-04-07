using System.Security.Cryptography;

namespace TokenIDP.Core.OAuth.ExternalProviders.Security;

public static class NonceGenerator
{
    public static string Generate(int bytes = 32)
    {
        var nonceBytes = RandomNumberGenerator.GetBytes(Math.Max(16, bytes));
        return Convert.ToHexString(nonceBytes).ToLowerInvariant();
    }
}

