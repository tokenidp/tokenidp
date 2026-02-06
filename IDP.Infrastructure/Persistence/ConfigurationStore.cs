using IDP.Domain.AggregateRoots.Clients;
using IDP.Domain.AggregateRoots.Configurations;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Infrastructure.Persistence;

internal class ConfigurationStore : IConfigurationStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationStore> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ConfigurationStore(IApplicationDbContext dbContext,
        IAppLogger<ConfigurationStore> logger,
        ICache cache,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ConfigurationShortInfo>> GetTenantConfigurations(int tenantId, ConfigurationScopes type)
    {
        _logger.LogDebug("GetTenantConfigurations for scope: {ConfigScope}", type.ToString());

        var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey(tenantId, type.ToString());

        var configurations = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var configurations = await _dbContext.Configurations
            .Where(x => x.TenantId == tenantId && x.Scope == type && !x.IsDeleted)
            .Select(ConfigurationProjection.ProjectionShort)
            .ToListAsync();

            _logger.LogDebug("Cached Configurations for {CacheKey}", cacheKey);

            return configurations;
        }, new TimeSpan(0, 45, 0));

        _logger.LogDebug("Retrieved Configurations for scope: {ConfigScope}", type.ToString());

        return configurations;
    }
}
