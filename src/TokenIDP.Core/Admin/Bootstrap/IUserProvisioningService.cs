using TokenIDP.Core.Admin.Users;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface IUserProvisioningService
{
    Task<User> CreateAsync(IApplicationDbContext db, int tenantId, UserDetail command, CancellationToken ct);
    Task<User?> ExistsAsync(IApplicationDbContext db, int tenantId, string userName, CancellationToken ct);
}

