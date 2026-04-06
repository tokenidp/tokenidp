using TokenIDP.Core.Admin.Configurations;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface IConfigurationSeeder
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string configKey, string scope, CancellationToken ct);
    Task CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateConfiguration command, CancellationToken ct);
}
