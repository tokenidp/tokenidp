using System.Security.Cryptography;
using System.Text;

namespace TokenIDP.Core.OAuth.ExternalProviders.Security;

public static class PkceGenerator
{
    public static PkcePair Generate(int verifierBytes = 64)
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(Math.Max(32, verifierBytes)));

        var challenge = CreateCodeChallenge(verifier);

        return new PkcePair(verifier, challenge, "S256");
    }

    public static string CreateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed record PkcePair(
    string CodeVerifier,
    string CodeChallenge,
    string CodeChallengeMethod
);

