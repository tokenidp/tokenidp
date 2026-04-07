using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal sealed class UserLookupsUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<UserLookupsUseCase> _logger;

    public UserLookupsUseCase(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAppLogger<UserLookupsUseCase> logger)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<UserLookups>> GetUserLookups(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching user lookups for tenant {TenantId}", _currentUserService.TenantId);

        var userLookups = await _userRepository.GetUserLookupsAsync(
            _currentUserService.TenantId,
            cancellationToken);

        _logger.LogDebug("User lookups fetched for tenant {TenantId}", _currentUserService.TenantId);
        return ApiResult<UserLookups>.Success(userLookups);
    }
}
