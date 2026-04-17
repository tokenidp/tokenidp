using TokenIDP.Domain.AggregateRoots.Configurations;
using TokenIDP.Core.Admin.Configurations;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class TenantConfigurationRepository : ITenantConfigurationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TenantConfigurationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Configuration> Query()
    {
        return _dbContext.Configurations;
    }

    public Task<Configuration?> GetByIdAsync(int tenantId, int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Configurations
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && !c.IsDeleted,
                cancellationToken);
    }

    public Task<Configuration?> GetByKeyAsync(
        int tenantId,
        string key,
        ConfigurationScopes? scope = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Configurations
            .Where(c => c.TenantId == tenantId && c.ConfigKey == key);

        if (scope.HasValue)
        {
            query = query.Where(c => c.Scope == scope.Value);
        }

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Configuration configuration, CancellationToken cancellationToken = default)
    {
        await _dbContext.Configurations.AddAsync(configuration, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Configuration> configurations, CancellationToken cancellationToken = default)
    {
        await _dbContext.Configurations.AddRangeAsync(configurations, cancellationToken);
    }

    public void Update(Configuration configuration)
    {
        _dbContext.Configurations.Update(configuration);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

