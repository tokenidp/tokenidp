using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Server.Multitenancy;

internal sealed class TenantResolver : ITenantResolver
{
    private readonly ITenantRepository _tenantRepository;

    public TenantResolver(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantContext?> ResolveAsync(string tenantKey, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.ResolveTenantAsync(tenantKey, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return null;
        }

        return tenant.Context;
    }
}
