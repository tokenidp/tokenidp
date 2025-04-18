using Identity.Application.Identity;

namespace Identity.Infrastructure.Identity;

public class AuthorizationService : IAuthorization
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public AuthorizationService(IApplicationDbContext dbContext,
        ICache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<bool> IsAuthorized(int userId, string claim)
    {
        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, claim);

        var userRole = _cache.GetValue<string>(cacheKey);

        if (!string.IsNullOrEmpty(userRole))
        {
            return true;
        }

        var userClaim = await (from ar in _dbContext.AppRoleClaims
                               join ur in _dbContext.AppUserRoles on ar.RoleId equals ur.RoleId
                               join r in _dbContext.AppRoles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               && ar.ClaimType == claim && ar.ClaimValue == "true"
                               select ar.ClaimValue).FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(userClaim))
        {
            _cache.Add(cacheKey, "Yes");
            return true;
        }

        return default;
    }

    public async Task<bool> IsInRole(int userId, string role)
    {
        var cacheKey = CacheKeys.USER_ROLE.FormatCacheKey(userId, role);

        var userRole = _cache.GetValue<string>(cacheKey);

        if (!string.IsNullOrEmpty(userRole))
        {
            return true;
        }

        var assignedRole = await (from ur in _dbContext.AppUserRoles
                                  join r in _dbContext.AppRoles on ur.RoleId equals r.Id
                                  where ur.UserId == userId && r.Name == role
                                  && r.IsDeleted != true && r.IsActive != false
                                  select r.Name).FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(assignedRole))
        {
            _cache.Add(cacheKey, "Yes");
            return true;
        }

        return default;
    }
}