using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Server.Multitenancy;

internal sealed class HostTenantResolver : ITenantResolver
{
    public const string TenantKeyItemName = "__tenant_key";

    private readonly ITenantRepository _tenantRepository;
    private readonly TenantResolutionOptions _options;

    public HostTenantResolver(
        ITenantRepository tenantRepository,
        IOptions<TenantResolutionOptions> options)
    {
        _tenantRepository = tenantRepository;
        _options = options.Value;
    }

    public async Task<TenantContext?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var tenantKey = httpContext.Items.TryGetValue(TenantKeyItemName, out var itemValue)
            ? itemValue?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            var parsed = TenantHostParser.TryResolveTenantKey(
                httpContext.Request.Host.Host,
                _options,
                out var resolvedTenantKey);

            if (!parsed)
            {
                return null;
            }

            tenantKey = resolvedTenantKey;
        }

        var tenant = await _tenantRepository.ResolveTenantAsync(tenantKey, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return null;
        }

        return tenant.Context;
    }
}
