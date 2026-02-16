using Admin.Core.Bootstrap;
using Admin.Core.Users;

namespace IDP.Infrastructure.Bootstrap;

internal class UserProvisioningService : IUserProvisioningService
{
    private readonly UserManager<User> _userManager;

    public UserProvisioningService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User> CreateAsync(IApplicationDbContext db, int tenantId, UserDetail command, CancellationToken ct)
    {
        var createResult = User.Create(
            tenantId,
            command.FirstName,
            command.LastName,
            command.UserName!,
            command.Email!,
            command.Phone!,
            0,
            command.Roles,
            out var user);

        if (!string.IsNullOrWhiteSpace(command.Status) &&
           Enum.TryParse<UserStatus>(command.Status, true, out var parsedStatus))
        {
            user!.UpdateStatus(parsedStatus);
        }

        await _userManager.CreateAsync(user!, command.Password!);

        return user!;
    }

    public async Task<User?> ExistsAsync(IApplicationDbContext db, int tenantId, string userName, CancellationToken ct)
    {
        var existingUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.UserName == userName, ct);

        return existingUser;
    }
}