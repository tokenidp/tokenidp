using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

internal interface ITenantConfigurationRepository
{
    IQueryable<Configuration> Query();
    Task<Configuration?> GetByIdAsync(int tenantId, int id, CancellationToken cancellationToken = default);
    Task<Configuration?> GetByKeyAsync(int tenantId, string key, CancellationToken cancellationToken = default);
    Task AddAsync(Configuration configuration, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Configuration> configurations, CancellationToken cancellationToken = default);
    void Update(Configuration configuration);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

