using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

internal sealed class TenantConfigurationRepository : ITenantConfigurationRepository
{
    private readonly IApplicationDbContext _dbContext;

    public TenantConfigurationRepository(IApplicationDbContext dbContext)
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

    public Task<Configuration?> GetByKeyAsync(int tenantId, string key, CancellationToken cancellationToken = default)
    {
        return _dbContext.Configurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == key && !c.IsDeleted,
                cancellationToken);
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

