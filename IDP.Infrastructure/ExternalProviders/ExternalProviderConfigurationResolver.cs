using IDP.Domain.AggregateRoots.Clients;

namespace IDP.Infrastructure.ExternalProviders;

public sealed class ExternalProviderConfigurationResolver
{
    private readonly IApplicationDbContext _dbContext;

    public ExternalProviderConfigurationResolver(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantExternalProvider?> ResolveAsync(int tenantId, 
        int clientId, 
        ExternalProviderTypes providerType)
    {
        var provider = await (
                        from tenantProvider in _dbContext.TenantExternalProviders
                        join clientProvider in _dbContext.ClientExternalProviders
                            on tenantProvider.Id equals clientProvider.ExternalProviderId
                        where tenantProvider.TenantId == tenantId
                            && clientProvider.ClientId == clientId
                            && tenantProvider.ProviderType == providerType
                            && tenantProvider.Enabled
                            && clientProvider.EnabledForClient
                        select tenantProvider
                    ).FirstOrDefaultAsync();

        if (provider == null)
            return null;

        return provider;
    }
}
