using System.Text;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Core.OAuth.Security;

internal static class ClientSecretValidator
{
    internal static bool Matches(string? providedSecret, IEnumerable<string>? storedSecrets)
    {
        if (string.IsNullOrWhiteSpace(providedSecret) || storedSecrets is null)
            return false;

        var providedHash = SecretHasher.HashSecret(providedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedHash);

        foreach (var storedSecret in storedSecrets)
        {
            if (string.IsNullOrWhiteSpace(storedSecret))
                continue;

            var storedBytes = Encoding.UTF8.GetBytes(storedSecret);

            if (SecretHasher.FixedTimeEquals(providedBytes, storedBytes))
                return true;
        }

        return false;
    }
}
