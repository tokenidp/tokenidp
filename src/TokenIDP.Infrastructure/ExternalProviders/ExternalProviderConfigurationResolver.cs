using TokenIDP.Core.Abstractions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.ExternalProviders;

public sealed class ExternalProviderConfigurationResolver
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ExternalProviderConfigurationResolver> _logger;

    public ExternalProviderConfigurationResolver(
        ApplicationDbContext dbContext,
        ICache cache,
        IAppLogger<ExternalProviderConfigurationResolver> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClientExternalProviderSnapshot?> ResolveAsync(
        int tenantId,
        int clientId,
        ExternalProviderTypes providerType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Resolve external provider config for tenant: {TenantId}, client: {ClientId}, provider: {ProviderType}",
            tenantId,
            clientId,
            providerType);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("EPRV", tenantId, clientId, providerType);

        var provider = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var resolvedProvider = await (
                from tp in _dbContext.TenantExternalProviders.AsNoTracking()
                join cp in _dbContext.ClientExternalProviders.AsNoTracking()
                    on tp.Id equals cp.ExternalProviderId
                where tp.TenantId == tenantId
                    && cp.ClientId == clientId
                    && tp.ProviderType == providerType
                    && tp.Enabled
                    && cp.EnabledForClient
                    && tp.OidcConfig != null
                select new ClientExternalProviderSnapshot(
                    tp.ProviderType.ToString(),
                    cp.EnabledForClient,
                    tp.Enabled,
                    tp.OidcConfig!.ClientId,
                    tp.OidcConfig.ClientSecret)
            ).FirstOrDefaultAsync(cancellationToken);

            _logger.LogDebug("Cached external provider config for {CacheKey}", cacheKey);

            return resolvedProvider;
        }, expiration: TimeSpan.FromMinutes(30));

        _logger.LogDebug(
            "Retrieved external provider config for tenant: {TenantId}, client: {ClientId}, provider: {ProviderType}",
            tenantId,
            clientId,
            providerType);

        return provider;
    }
}

