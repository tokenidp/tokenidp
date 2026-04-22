using System.Text.RegularExpressions;
using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Server.Multitenancy;

internal static partial class TenantKeyValidator
{
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$", RegexOptions.Compiled)]
    private static partial Regex TenantKeyRegex();

    public static bool TryNormalize(
        string? rawValue,
        TenantResolutionOptions options,
        out string tenantKey)
    {
        tenantKey = string.Empty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = rawValue.Trim().ToLowerInvariant();
        if (normalized.Length == 0 || normalized.Length > Math.Max(1, options.MaxTenantKeyLength))
        {
            return false;
        }

        if (!TenantKeyRegex().IsMatch(normalized))
        {
            return false;
        }

        tenantKey = normalized;
        return true;
    }
}
