
namespace IDP.Infrastructure.Persistence;

internal sealed class RoleStore : IRoleStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleStore> _logger;

    public RoleStore(IApplicationDbContext dbContext,
        IAppLogger<RoleStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return userRoles;
    }
}
