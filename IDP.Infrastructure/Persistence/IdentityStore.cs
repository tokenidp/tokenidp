using IDP.Domain.DomainEvents.Users;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Core.OAuth;

internal sealed class IdentityStore : IIdentityStore
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;
    private readonly IAppLogger<IdentityStore> _logger;

    public IdentityStore(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAppLogger<IdentityStore> logger,
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
    }

    public async Task<AuthenticationContext> Authenticate(string userName, string password)
    {
        try
        {
            _logger.LogInfo("Authentication attempt for user: {UserName}", userName);

            var user = await _userManager.FindByNameAsync(userName)
             ?? await _userManager.FindByEmailAsync(userName)
             ?? await _userManager.Users.Where(u => u.PhoneNumber == userName)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                var message = $"User not found with username or email: {userName}";

                _logger.LogWarning(message);

                _applicationEventDispatcher.Raise(
                        new AuthenticationFlowEvent(
                            UserId: null,
                            TenantId: _currentUserService.TenantId,
                            Action: AuthenticationAction.Login,
                            Result: AuthenticationResult.Failed,
                            Description: message,
                            CorrelationId: _currentUserService.CorrelationId,
                            IpAddress: _currentUserService.IpAddress,
                            UserAgent: _currentUserService.UserAgent
                        ));

                await _applicationDbContext.SaveChangesAsync();

                return AuthenticationContext.Failure($"User with {userName} not found.");
            }

            _logger.LogDebug("Found user {UserId} for authentication", user.Id);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                string message = $"Failed authentication for user {userName}. Reason: {result.ToString()}";

                _logger.LogWarning(message);

                user.MarkLoginFailed(message,
                    _currentUserService.CorrelationId,
                    _currentUserService.IpAddress,
                    _currentUserService.UserAgent);

                await _userManager.UpdateAsync(user);

                return AuthenticationContext.Failure($"Credentials for '{userName} aren't valid.");
            }

            _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

            user.MarkLoginSuccess(_currentUserService.CorrelationId,
                _currentUserService.IpAddress,
                _currentUserService.UserAgent);

            await _userManager.UpdateAsync(user);

            return AuthenticationContext.Authenticated(user);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<User> GetUserById(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        return user!;
    }

    public async Task<UserShortInfo> GetUserShortInfo(int id)
    {
        var user = await _applicationDbContext.Users
            .Where(u => u.Id == id)
            .Select(UserProjection.Projection)
            .FirstOrDefaultAsync();

        return user!;
    }

    public async Task<int> SaveChangesAsync()
    {
        var rows = await _applicationDbContext.SaveChangesAsync();

        return rows;
    }
}