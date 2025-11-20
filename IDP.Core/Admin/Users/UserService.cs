using IDP.Core.Admin.Roles;

namespace IDP.Core.Admin.Users;

internal class UserService
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAppLogger<UserService> _logger;

    public UserService(ICurrentUserService currentUserService,
        UserManager<User> userManager,
        ApplicationDbContext applicationDbContext,
        IAppLogger<UserService> logger)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _dbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<Result> CreateUser(CreateUpdateUser request)
    {

        var user = CreateNewUser(request);

        var result = await _userManager.CreateAsync(user, request.Password);

        return result.ToApplicationResult(user.Id);
    }

    public async Task<Result> UpdateUser(CreateUpdateUser request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null)
        {
            return Result.Failure("NotFound", "User not found for the Id {0}".FormatString(request.Id));
        }

        MapUserUpdate(user, request);

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult(user.Id);
    }

    public async Task<Result> UpdateUserStatus(UpdateUserStatus request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null)
        {
            return Result.Failure("NotFound", "User not found for the Id {0}".FormatString(request.Id));
        }

        user.UpdateStatus(request.Status);

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult(user.Id);
    }

    public async Task<UserDto?> GetUserById(int userId)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<UserLookups?> GetUserLookups()
    {
        UserLookups userLookups = new();

        var roles = await _dbContext.Roles
            .Where(r => r.TenantId == _currentUserService.TenantId)
            .Select(RoleLookup.Projection)
           .ToListAsync();

        userLookups.RolesLookup = roles;
        return userLookups;
    }

    public async Task<PaginatedList<UserSearchDto>> GetUsers(SearchData request)
    {
        var users = await _dbContext.UsersSearch
           .AsNoTracking()
           .Select(UserSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }

    public async Task<UserClaim> GetUserClaims(int userId)
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

        var claims = await _dbContext.UserRolePermissions
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

        var userInfo = UserClaim.Create(
            user.Id,
            user.TenantId,
            user.FullName,
            tenant.HomePageUrl,
            claims);

        _logger.LogInfo("Successfully compiled user info for {UserId}", userId);

        return userInfo;
    }

    private User CreateNewUser(CreateUpdateUser request)
    {
        var tenantId = request.TenantId == 0
            ? _currentUserService.TenantId
            : request.TenantId;

        return new User(
            tenantId,
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Phone,
            _currentUserService.UserId,
            request.Roles
        );
    }

    private void MapUserUpdate(User user, CreateUpdateUser request)
    {
        user.UpdateUser(
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Phone,
            _currentUserService.UserId,
            request.Roles
        );
    }
}
