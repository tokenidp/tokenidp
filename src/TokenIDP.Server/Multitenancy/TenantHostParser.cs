using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Server.Multitenancy;

public static class TenantHostParser
{
    public static TenantHostResolutionResult Resolve(
        string host,
        TenantResolutionOptions options)
    {
        var normalizedHost = NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return new TenantHostResolutionResult(TenantHostResolutionKind.Invalid);
        }

        if (IsSystemHostAlias(normalizedHost, options.SystemHostAliases))
        {
            return new TenantHostResolutionResult(TenantHostResolutionKind.Root);
        }

        var developmentResolution = ResolveFromDomains(normalizedHost, options.AllowedDevelopmentHosts);
        if (developmentResolution.Kind != TenantHostResolutionKind.None)
        {
            return developmentResolution;
        }

        var rootDomainResolution = ResolveFromDomains(normalizedHost, options.AllowedRootDomains);
        if (rootDomainResolution.Kind != TenantHostResolutionKind.None)
        {
            return rootDomainResolution;
        }

        return new TenantHostResolutionResult(TenantHostResolutionKind.None);
    }

    public static bool TryResolveTenantKey(
        string host,
        TenantResolutionOptions options,
        out string tenantKey)
    {
        var result = Resolve(host, options);
        tenantKey = result.TenantKey ?? string.Empty;
        return result.Kind == TenantHostResolutionKind.Tenant;
    }

    private static TenantHostResolutionResult ResolveFromDomains(
        string host,
        IEnumerable<string> allowedDomains)
    {
        foreach (var domain in allowedDomains
                     .Select(NormalizeHost)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (string.Equals(host, domain, StringComparison.OrdinalIgnoreCase))
            {
                return new TenantHostResolutionResult(TenantHostResolutionKind.Root);
            }

            var suffix = $".{domain}";
            if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var subdomain = host[..^suffix.Length];
            if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Contains('.', StringComparison.Ordinal))
            {
                return new TenantHostResolutionResult(TenantHostResolutionKind.Invalid);
            }

            var normalizedTenantKey = NormalizeTenantKey(subdomain);
            return string.IsNullOrWhiteSpace(normalizedTenantKey)
                ? new TenantHostResolutionResult(TenantHostResolutionKind.Invalid)
                : new TenantHostResolutionResult(TenantHostResolutionKind.Tenant, normalizedTenantKey);
        }

        return new TenantHostResolutionResult(TenantHostResolutionKind.None);
    }

    private static bool IsSystemHostAlias(string host, IEnumerable<string> systemHostAliases)
    {
        return systemHostAliases
            .Select(NormalizeHost)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Any(alias => string.Equals(host, alias, StringComparison.OrdinalIgnoreCase));
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
