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

    public async Task<List<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _applicationDbContext.UserRoles
                      join r in _applicationDbContext.Roles on ur.RoleId equals r.Id
                      where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                      select r.Name).ToListAsync();

        return userRoles;
    }

    public async Task<bool> HasUserPermission(int userId, string claim)
    {
        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, claim);
        var userRole = _cache.GetValue<string>(cacheKey);

        if (!string.IsNullOrEmpty(userRole))
        {
            _logger.LogDebug("Cache hit for claim authorization {CacheKey}", cacheKey);
            return true;
        }

        _logger.LogDebug("Cache miss, querying database for claim {Claim}", claim);

        var claimValue = await _applicationDbContext.UsersClaims
          .Where(c => c.UserId == userId
                       && c.ClaimType == claim
                       && c.ClaimValue == "true")
          .Select(c => c.ClaimValue)
          .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(claimValue))
        {
            _logger.LogInfo("User {UserId} authorized for claim {Claim}", userId, claim);
            _cache.Add(cacheKey, "Yes");
            _logger.LogDebug("Cached claim authorization for {CacheKey}", cacheKey);
            return true;
        }

        _logger.LogWarning("User {UserId} not authorized for claim {Claim}", userId, claim);
        return false;
    }

    public async Task<bool> IsUserInRole(int userId, string role)
    {
        var cacheKey = CacheKeys.USER_ROLE.FormatCacheKey(userId, role);
        var userRole = _cache.GetValue<string>(cacheKey);

        if (!string.IsNullOrEmpty(userRole))
        {
            _logger.LogDebug("Cache hit for role membership {CacheKey}", cacheKey);
            return true;
        }

        _logger.LogDebug("Cache miss, querying database for role {Role}", role);

        var assignedRole = await (from ur in _applicationDbContext.UserRoles
                                  join r in _applicationDbContext.Roles on ur.RoleId equals r.Id
                                  where ur.UserId == userId
                                    && r.Name == role
                                    && r.IsDeleted != true
                                    && r.IsActive != false
                                  select r.Name).FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(assignedRole))
        {
            _logger.LogInfo("User {UserId} has role {Role}", userId, role);

            _cache.Add(cacheKey, "Yes");

            _logger.LogDebug("Cached role membership for {CacheKey}", cacheKey);

            return true;
        }

        _logger.LogWarning("User {UserId} does not have role {Role}", userId, role);
        return false;
    }
}
