using Admin.Core.Configurations;

namespace Admin.Core.Bootstrap;

public interface IConfigurationSeeder
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string configKey, string scope, CancellationToken ct);
    Task CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateConfiguration command, CancellationToken ct);
}