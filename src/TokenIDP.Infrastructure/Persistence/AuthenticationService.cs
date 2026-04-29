using System.Text.Json;
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
    private readonly ILookupNormalizer _normalizer;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public AuthenticationService(IAppLogger<AuthenticationService> logger,
        ApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher,
        PasswordService passwordService,
        IUserRepository userStore,
        ILookupNormalizer normalizer,
        ITenantContextAccessor tenantContextAccessor)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
        _passwordService = passwordService;
        _userStore = userStore;
        _normalizer = normalizer;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<AuthenticationContext> Authenticate(int tenantId, string userName, string password)
    {
        try
        {
            _logger.LogInfo("Authentication attempt for user: {UserName}, {TenantId}", userName, tenantId);

            var loginHint = userName.Trim();
            var normalizedLoginHint = _normalizer.NormalizeName(loginHint);
            var normalizedEmailHint = _normalizer.NormalizeEmail(loginHint);

            var user = await _applicationDbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.TenantId == tenantId
                && !u.IsDeleted
                && (
                 u.NormalizedUserName == normalizedLoginHint
                || u.NormalizedEmail == normalizedEmailHint
                || u.UserName == loginHint
                || u.Email == loginHint
                || u.PhoneNumber == loginHint));

            if (user == null)
            {
                var message = $"User not found with username or email: {userName} in tenant: {tenantId}";

                await LogAuthenticationLookupMissAsync(
                    tenantId,
                    loginHint,
                    normalizedLoginHint,
                    normalizedEmailHint);

                _logger.LogWarning(message);

                _applicationEventDispatcher.Raise(
                        new AuthenticationFlowEvent(
                            UserId: null,
                            TenantId: tenantId,
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

    private async Task LogAuthenticationLookupMissAsync(
        int tenantId,
        string loginHint,
        string normalizedLoginHint,
        string normalizedEmailHint)
    {
        var candidates = await _applicationDbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u =>
                u.UserName == loginHint ||
                u.Email == loginHint ||
                u.PhoneNumber == loginHint ||
                u.NormalizedUserName == normalizedLoginHint ||
                u.NormalizedEmail == normalizedEmailHint)
            .Select(u => new
            {
                u.Id,
                u.TenantId,
                u.UserName,
                u.Email,
                u.NormalizedUserName,
                u.NormalizedEmail,
                u.IsDeleted,
                u.StatusId,
                u.EmailConfirmed
            })
            .Take(10)
            .ToListAsync();

        _logger.LogWarning(
            "Authentication lookup miss details. RequestedTenantId={RequestedTenantId}, AmbientTenantId={AmbientTenantId}, LoginHint='{LoginHint}', NormalizedName='{NormalizedName}', NormalizedEmail='{NormalizedEmail}', CandidateCount={CandidateCount}, Candidates={Candidates}",
            tenantId,
            _tenantContextAccessor.CurrentTenantId?.ToString() ?? "none",
            loginHint,
            normalizedLoginHint ?? string.Empty,
            normalizedEmailHint ?? string.Empty,
            candidates.Count,
            JsonSerializer.Serialize(candidates));
    }
}
