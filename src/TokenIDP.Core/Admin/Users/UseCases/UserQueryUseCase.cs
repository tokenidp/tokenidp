using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal class UserQueryUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IAppLogger<UserQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UserQueryUseCase(IUserRepository userRepository,
        IAppLogger<UserQueryUseCase> logger,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<UserDetail>> GetUserById(
        int userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching user {UserId}", userId);

        var user = await _userRepository.GetUserDetailAsync(
            _currentUserService.TenantId,
            userId,
            cancellationToken);

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

        var users = await _userRepository.SearchUsersAsync(
            _currentUserService.TenantId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} users", users.TotalCount);

        return ApiResult<PaginatedList<UserSearchResult>>.Success(users);
    }
}
