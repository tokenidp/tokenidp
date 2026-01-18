using Admin.Core.Permissions;
using Admin.Core.Roles;

namespace Admin.Core.Users;

internal class UserUseCases
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<UserUseCases> _logger;

    public UserUseCases(ICurrentUserService currentUserService,
        UserManager<User> userManager,
        IApplicationDbContext applicationDbContext,
        IAppLogger<UserUseCases> logger)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _dbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateUser(CreateUpdateUser request)
    {
        _logger.LogDebug("Creating user {UserName} for tenant {TenantId}", request.UserName, request.TenantId);

        var user = CreateNewUser(request);

        var result = await _userManager.CreateAsync(user, request.Password);

        _logger.LogInfo("User created with Id {UserId}", user.Id);

        return result.ToApiResult(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUser(int id, CreateUpdateUser request)
    {
        _logger.LogDebug("Updating user {UserId}", id);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        MapUserUpdate(user, request);

        var result = await _userManager.UpdateAsync(user);

        _logger.LogInfo("User updated {UserId}", id);

        return result.ToApiResult(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUserStatus(int id, UpdateUserStatus request)
    {
        _logger.LogDebug("Updating user status for {UserId}", id);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("User not found for status update: {UserId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        user.UpdateStatus(request.Status);

        var result = await _userManager.UpdateAsync(user);

        _logger.LogInfo("User status updated {UserId}", id);

        return result.ToApiResult(user.Id);
    }

    public async Task<ApiResult<UserDto>> GetUserById(int userId)
    {
        _logger.LogDebug("Fetching user {UserId}", userId);

        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return ApiResult<UserDto>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(userId)));
        }

        return ApiResult<UserDto>.Success(user);
    }

    public async Task<ApiResult<UserLookups>> GetUserLookups()
    {
        _logger.LogDebug("Fetching user lookups for tenant {TenantId}", _currentUserService.TenantId);

        UserLookups userLookups = new();

        var roles = await _dbContext.Roles
            .Where(r => r.TenantId == _currentUserService.TenantId)
            .Select(RoleLookup.Projection)
           .ToListAsync();

        userLookups.RolesLookup = roles;

        _logger.LogDebug("User lookups fetched for tenant {TenantId}", _currentUserService.TenantId);
        return ApiResult<UserLookups>.Success(userLookups);
    }

    public async Task<ApiResult<PaginatedList<UserSearchDto>>> GetUsers(SearchData request)
    {
        _logger.LogDebug("Fetching users list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var users = await _dbContext.UsersSearch
           .AsNoTracking()
           .Select(UserSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} users", users.TotalCount);

        return ApiResult<PaginatedList<UserSearchDto>>.Success(users);
    }

    public async Task<ApiResult<UserPermission>> GetUserPermissions()
    {
        _logger.LogDebug("Fetching user info for: {UserId}", _currentUserService.UserId);

        var user = await _userManager.FindByIdAsync(_currentUserService.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "User not found."));
        }

        _logger.LogDebug("Found user {UserName} (Tenant: {TenantId})",
            user.UserName ?? string.Empty, user.TenantId);

        var permissions = await _dbContext.UserRolePermissions
            .Where(c => c.UserId == _currentUserService.UserId)
            .Select(c => new PermissionInfo(
                c.Id,
                c.ParentId,
                c.UserId,
                c.Sequence,
                c.PermissionName,
                c.IsAllowed ? "true" : "false",
                c.Permissionkey,
                c.Icon,
                c.AccessUrl,
                c.RoleName,
                c.ControlType))
            .ToListAsync();

        if (!permissions.IsSafe())
        {
            _logger.LogWarning("No claims found for user {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "Claims not found."));
        }

        _logger.LogDebug("Found {ClaimCount} claims for user {UserId}",
            permissions.Count, _currentUserService.UserId);

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for user {UserId}", _currentUserService.UserId);
        }

        var userInfo = UserPermission.Create(
            user.Id,
            user.TenantId,
            user.FullName,
            tenant?.HomePageUrl ?? string.Empty,
            permissions);

        _logger.LogInfo("Successfully compiled user info for {UserId}", _currentUserService.UserId);

        return ApiResult<UserPermission>.Success(userInfo);
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
