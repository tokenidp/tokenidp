using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class UserProvisioningService : IUserProvisioningService
{
    private readonly PasswordService _passwordService;
    private readonly UserNormalizationService _userNormalizationService;
    private readonly ILookupNormalizer _normalizer;

    public UserProvisioningService(
        PasswordService passwordService,
        UserNormalizationService userNormalizationService,
        ILookupNormalizer normalizer)
    {
        _passwordService = passwordService;
        _userNormalizationService = userNormalizationService;
        _normalizer = normalizer;
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

        if (!createResult.IsSuccess || user is null)
        {
            throw new InvalidOperationException(
                string.Join("; ", createResult.Errors.Select(x => x.Message)));
        }

        if (!string.IsNullOrWhiteSpace(command.Status) &&
           Enum.TryParse<UserStatus>(command.Status, true, out var parsedStatus))
        {
            user.UpdateStatus(parsedStatus);
        }

        user.GenerateUserCode(0);

        _userNormalizationService.Normalize(user);

        _passwordService.SetPassword(user, command.Password!);

        db.Users.Add(user);

        await db.SaveChangesAsync();

        return user;
    }

    public async Task<User?> ExistsAsync(ApplicationDbContext db,
        int tenantId,
        string userName,
        CancellationToken ct)
    {
        var normalizedUserName = _normalizer.NormalizeName(userName.Trim());

        var existingUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.TenantId == tenantId &&
                        (t.NormalizedUserName == normalizedUserName || t.UserName == userName),
                        ct);

        return existingUser;
    }
}

