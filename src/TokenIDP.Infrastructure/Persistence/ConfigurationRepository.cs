using TokenIDP.Domain.AggregateRoots.Configurations;
using TokenIDP.Infrastructure.Projections;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class ConfigurationRepository : IConfigurationRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationRepository> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ConfigurationRepository(ApplicationDbContext dbContext,
        IAppLogger<ConfigurationRepository> logger,
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


