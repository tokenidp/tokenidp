using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class UserProvisioningService : IUserProvisioningService
{
    private readonly PasswordService _passwordService;

    public UserProvisioningService(PasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public async Task<User> CreateAsync(ApplicationDbContext db,
        int tenantId,
        UserDetail command,
        CancellationToken ct)
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

        user.GenerateUserCode(0);

        _passwordService.SetPassword(user!, command.Password!);

        db.Users.Add(user!);

        await db.SaveChangesAsync();

        return user!;
    }

    public async Task<User?> ExistsAsync(ApplicationDbContext db,
        int tenantId,
        string userName,
        CancellationToken ct)
    {
        var existingUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.UserName == userName, ct);

        return existingUser;
    }
}

