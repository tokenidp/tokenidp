namespace IDP.Infrastructure.ExternalProviders;

public sealed class ExternalProviderConfigurationResolver
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ExternalProviderConfigurationResolver> _logger;

    public ExternalProviderConfigurationResolver(
        IApplicationDbContext dbContext,
        ICache cache,
        IAppLogger<ExternalProviderConfigurationResolver> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TenantExternalProvider?> ResolveAsync(
        int tenantId,
        int clientId,
        ExternalProviderTypes providerType)
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
                from tenantProvider in _dbContext.TenantExternalProviders.AsNoTracking()
                join clientProvider in _dbContext.ClientExternalProviders.AsNoTracking()
                    on tenantProvider.Id equals clientProvider.ExternalProviderId
                where tenantProvider.TenantId == tenantId
                    && clientProvider.ClientId == clientId
                    && tenantProvider.ProviderType == providerType
                    && tenantProvider.Enabled
                    && clientProvider.EnabledForClient
                select tenantProvider
            ).FirstOrDefaultAsync();

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