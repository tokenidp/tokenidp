namespace IDP.Service.Infrastructure;

public class UserRepo
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAppLogger<UserRepo> _logger;

    public UserRepo(UserManager<User> userManager,
        ApplicationDbContext dbContext,
        IAppLogger<UserRepo> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserInfo> GetUserInfo(int userId)
    {
        _logger.LogDebug("Fetching user info for: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            throw new NotFoundException("User not found.");
        }

        _logger.LogDebug("Found user {UserName} (Tenant: {TenantId})",
            user.UserName, user.TenantId);

        var claims = await _dbContext.UsersClaims
            .Where(c => c.UserId == userId)
            .Select(c => new ClaimDto(
                c.Id,
                c.ParentId,
                c.UserId,
                c.Sequence,
                c.ClaimType,
                c.ClaimName,
                c.ClaimValue,
                c.Icon,
                c.AccessUrl,
                c.RoleName,
                c.ControlType))
            .ToListAsync();

        if (!claims.IsSafe())
        {
            _logger.LogWarning("No claims found for user {UserId}", userId);
            throw new NotFoundException("Claims not found.");
        }

        _logger.LogDebug("Found {ClaimCount} claims for user {UserId}",
            claims.Count, userId);

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for user {UserId}", userId);
        }

        var userInfo = UserInfo.Create(
            user.Id,
            user.TenantId,
            user.FullName,
            tenant?.HomePageUrl,
            claims);

        _logger.LogInfo("Successfully compiled user info for {UserId}", userId);

        return userInfo;
    }
}
