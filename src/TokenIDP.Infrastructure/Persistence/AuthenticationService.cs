using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.AggregateRoots.Users.Enums;
using TokenIDP.Domain.DomainEvents.Users;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;
    private readonly IAppLogger<AuthenticationService> _logger;
    private readonly IUserRepository _userStore;
    private readonly PasswordService _passwordService;

    public AuthenticationService(IAppLogger<AuthenticationService> logger,
        ApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher,
        PasswordService passwordService,
        IUserRepository userStore)
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
            _logger.LogInfo("Authentication attempt for user: {UserName}, {TenantId}", userName, tenantId);

            var user = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId
                && !u.IsDeleted
                && (
                 u.UserName == userName
                || u.Email == userName
                || u.PhoneNumber == userName));

            if (user == null)
            {
                var message = $"User not found with username or email: {userName} in tenant: {tenantId}";

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
