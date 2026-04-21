using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Server.Multitenancy;

public static class TenantHostParser
{
    public static bool TryResolveTenantKey(
        string host,
        TenantResolutionOptions options,
        out string tenantKey)
    {
        tenantKey = string.Empty;

        var normalizedHost = NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return false;
        }

        if (TryResolveFromDomains(normalizedHost, options.AllowedDevelopmentHosts, options.FallbackTenantKey, out tenantKey))
        {
            return true;
        }

        if (TryResolveFromDomains(normalizedHost, options.AllowedRootDomains, options.FallbackTenantKey, out tenantKey))
        {
            return true;
        }

        return false;
    }

    private static bool TryResolveFromDomains(
        string host,
        IEnumerable<string> allowedDomains,
        string? fallbackTenantKey,
        out string tenantKey)
    {
        tenantKey = string.Empty;

        foreach (var domain in allowedDomains
                     .Select(NormalizeHost)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (string.Equals(host, domain, StringComparison.OrdinalIgnoreCase))
            {
                tenantKey = NormalizeTenantKey(fallbackTenantKey);
                return !string.IsNullOrWhiteSpace(tenantKey);
            }

            var suffix = $".{domain}";
            if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var subdomain = host[..^suffix.Length];
            if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Contains('.', StringComparison.Ordinal))
            {
                return false;
            }

            tenantKey = NormalizeTenantKey(subdomain);
            return !string.IsNullOrWhiteSpace(tenantKey);
        }

        return false;
    }

    private static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var value = host.Trim().ToLowerInvariant();
        var portSeparatorIndex = value.LastIndexOf(':');
        if (portSeparatorIndex > 0 && value.Count(c => c == ':') == 1)
        {
            value = value[..portSeparatorIndex];
        }

        return value.Trim('.');
    }

    private static string NormalizeTenantKey(string? tenantKey)
        => string.IsNullOrWhiteSpace(tenantKey)
            ? string.Empty
            : tenantKey.Trim().ToLowerInvariant();
}
