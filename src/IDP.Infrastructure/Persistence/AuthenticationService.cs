using Admin.Core.Common;
using IDP.Domain.DomainEvents.Users;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.OAuth;

internal sealed class AuthenticationService : IAuthenticationService
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;
    private readonly IAppLogger<AuthenticationService> _logger;
    private readonly IUserStore _userStore;
    private readonly PasswordService _passwordService;

    public AuthenticationService(IAppLogger<AuthenticationService> logger,
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher,
        PasswordService passwordService,
        IUserStore userStore)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
        _passwordService = passwordService;
        _userStore = userStore;
    }

    public async Task<AuthenticationContext> Authenticate(int tenantId, string userName, string password)
    {
        try
        {
            _logger.LogInfo("Authentication attempt for user: {UserName}", userName);

            var user = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId
                && (
                 u.UserName == userName
                || u.Email == userName
                || u.PhoneNumber == userName));

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

                return AuthenticationContext.Failure("Invalid username or password");
            }

            _logger.LogDebug("Found user {UserId} for authentication", user.Id);

            if (user.IsLockedOut())
            {
                return AuthenticationContext.Failure("User is locked out.");
            }

            if (!user.EmailConfirmed)
            {
                return AuthenticationContext.Failure("Please confirm your email before signing in.");
            }

            var result = _passwordService.Verify(user, password);

            if (result == PasswordVerificationResult.Failed)
            {
                string message = $"Failed authentication for user {userName}. Reason: {result.ToString()}";

                _logger.LogWarning(message);

                user.MarkLoginFailed(message,
                    _currentUserService.CorrelationId,
                    _currentUserService.IpAddress,
                    _currentUserService.UserAgent);

                user.RegisterFailedAttempt(3, TimeSpan.FromMinutes(5));

                await _userStore.UpdateUser(user);

                return AuthenticationContext.Failure($"Invalid username or password");
            }

            _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

            user.MarkLoginSuccess(_currentUserService.CorrelationId,
                _currentUserService.IpAddress,
                _currentUserService.UserAgent);

            user.ResetAccessFailed();
            await _userStore.UpdateUser(user);

            return AuthenticationContext.Authenticated(user);
        }
        catch (Exception)
        {
            throw;
        }
    }
}