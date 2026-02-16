using Admin.Core.Tenants;
using Admin.Core.Users;

namespace Admin.Core.Bootstrap;

public interface IUserProvisioningService
{
    Task<User> CreateAsync(IApplicationDbContext db, int tenantId, UserDetail command, CancellationToken ct);
    Task<User?> ExistsAsync(IApplicationDbContext db, int tenantId, string userName, CancellationToken ct);
}
