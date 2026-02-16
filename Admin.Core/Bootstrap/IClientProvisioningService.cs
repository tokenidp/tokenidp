using Admin.Core.Clients;

namespace Admin.Core.Bootstrap;

public interface IClientProvisioningService
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string clientId, CancellationToken ct);
    Task CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateClient command, CancellationToken ct);
}
