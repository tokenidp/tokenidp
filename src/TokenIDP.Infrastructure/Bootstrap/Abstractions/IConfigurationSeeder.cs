using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface IConfigurationSeeder
{
    Task<bool> ExistsAsync(ApplicationDbContext db, int tenantId, string configKey, string scope, CancellationToken ct);
    Task CreateAsync(ApplicationDbContext db, int tenantId, CreateUpdateConfiguration command, CancellationToken ct);
}
