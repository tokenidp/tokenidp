using System.Security.Cryptography;

namespace TokenIDP.Core.OAuth.ExternalProviders.Security;

public static class OAuthStateGenerator
{
    public static string Generate(int bytes = 32)
    {
        var stateBytes = RandomNumberGenerator.GetBytes(Math.Max(16, bytes));
        return Base64UrlEncode(stateBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

