namespace IDP.Service.Infrastructure;

public class RoleRepo
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IAppLogger<RoleService> _logger;
    private readonly ICache _cache;

    public RoleRepo(ApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<RoleService> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _applicationDbContext.UserRoles
                               join r in _applicationDbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return userRoles;
    }

    public async Task<bool> HasUserPermission(int userId, string claim)
    {
        _logger.LogDebug("Get User {UserId} authorization for claim {Claim}", userId, claim);

        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, claim);

        var userRole = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var claimValue = await _applicationDbContext.UsersClaims
              .Where(c => c.UserId == userId
                           && c.ClaimType == claim
                           && c.ClaimValue == "true")
              .Select(c => c.ClaimValue)
              .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(claimValue);

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Cache hit for claim authorization {CacheKey}", cacheKey);

        return userRole;
    }

    public async Task<bool> IsUserInRole(int userId, string role)
    {
        var cacheKey = CacheKeys.USER_ROLE.FormatCacheKey(userId, role);

        var hasAssignedRole = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            var assignedRole = await (from ur in _applicationDbContext.UserRoles
                                      join r in _applicationDbContext.Roles on ur.RoleId equals r.Id
                                      where ur.UserId == userId
                                        && r.Name == role
                                        && r.IsDeleted != true
                                        && r.IsActive != false
                                      select r.Name).FirstOrDefaultAsync();

            _logger.LogDebug("Cached role membership for {CacheKey}", cacheKey);

            return !string.IsNullOrEmpty(assignedRole);

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Cache hit for role membership {CacheKey}", cacheKey);

        return hasAssignedRole;
    }
}
