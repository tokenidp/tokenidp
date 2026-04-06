using System.Security.Cryptography;
using System.Text;

namespace TokenIDP.Core.OAuth.Security;

internal static class PkceHelper
{
    public static (string CodeVerifier, string CodeChallenge) GeneratePkce()
    {
        // Generate a random 43-128 character code_verifier
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        string codeVerifier = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        // Hash the code_verifier using SHA-256 to get the code_challenge
        var codeChallenge = CalculateCodeChallenge(codeVerifier);
        return (codeVerifier, codeChallenge);
    }

    internal static string CalculateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(codeVerifier);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash)
                         .TrimEnd('=')
                         .Replace('+', '-')
                         .Replace('/', '_');
        }
    }
}

