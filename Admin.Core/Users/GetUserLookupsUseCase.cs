using Admin.Core.Permissions;

namespace Admin.Core.Users;

internal sealed class GetUserLookupsUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<GetUserLookupsUseCase> _logger;

    public GetUserLookupsUseCase(
        IApplicationDbContext dbContext,
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<GetUserLookupsUseCase> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<UserLookups>> GetUserLookups(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching user lookups for tenant {TenantId}", _currentUserService.TenantId);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == _currentUserService.TenantId)
            .Select(r => new LookupItem()
            {
                Key = r.Id.ToString(),
                Value = r.Name ?? string.Empty
            })
           .ToListAsync(cancellationToken);

        var userLookups = new UserLookups
        {
            Roles = roles,
            UserStatuses = UserLookupMapper.MapUserStatuses(),
            AddressTypes = UserLookupMapper.MapAddressTypes()
        };

        _logger.LogDebug("User lookups fetched for tenant {TenantId}", _currentUserService.TenantId);
        return ApiResult<UserLookups>.Success(userLookups);
    }
}