using TokenIDP.Core.Admin.Clients;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface IClientProvisioningService
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string clientId, CancellationToken ct);
    Task CreateAsync(IApplicationDbContext db, int tenantId, string clientId, CreateUpdateClient command, CancellationToken ct);
}

