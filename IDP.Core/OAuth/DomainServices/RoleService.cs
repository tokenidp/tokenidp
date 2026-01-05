namespace IDP.Core.OAuth.DomainServices;

internal sealed class RoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleService> _logger;

    public RoleService(IAppLogger<RoleService> logger,
        ApplicationDbContext dbContext,
        ICache cache)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cache = cache;
    }

    internal async Task<IEnumerable<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return userRoles;
    }
}