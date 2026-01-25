using Admin.Core.Common;

namespace Admin.Core.Users.UseCases;

internal class UserQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<UserQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UserQueryUseCase(IApplicationDbContext applicationDbContext,
        IAppLogger<UserQueryUseCase> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = applicationDbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<UserDetail>> GetUserById(
        int userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching user {UserId}", userId);

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u =>
                u.Id == userId &&
                u.TenantId == _currentUserService.TenantId)
            .Select(UserDetail.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return ApiResult<UserDetail>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(userId)));
        }

        return ApiResult<UserDetail>.Success(user);
    }

    public async Task<ApiResult<PaginatedList<UserSearchResult>>> GetUsers(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching users list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var query = _dbContext.UsersSearch
           .AsNoTracking()
           .Where(u => u.TenantId == _currentUserService.TenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();

        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();

            query = query.Where(user =>
                (user.FullName ?? string.Empty).ToLower().Contains(term) ||
                (user.UserName ?? string.Empty).ToLower().Contains(term) ||
                (user.Email ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var users = await query
           .Select(UserSearchResult.Projection)
           .ApplyFilter(criterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} users", users.TotalCount);

        return ApiResult<PaginatedList<UserSearchResult>>.Success(users);
    }
}