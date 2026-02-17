using Admin.Core.Common;
using Azure.Core;
using IDP.Domain.DomainEvents.Users;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Core.OAuth;

internal sealed class IdentityStore : IIdentityStore
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;
    private readonly IAppLogger<IdentityStore> _logger;
    private readonly PasswordService _passwordService;

    public IdentityStore(IAppLogger<IdentityStore> logger,
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher,
        PasswordService passwordService)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
        _passwordService = passwordService;
    }

    public async Task<AuthenticationContext> Authenticate(string userName, string password)
    {
        try
        {
            _logger.LogInfo("Authentication attempt for user: {UserName}", userName);

            var user = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == userName
                || u.Email == userName
                || u.PhoneNumber == userName);

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

            var result = _passwordService.Verify(user, password);

            if (result == PasswordVerificationResult.Failed)
            {
                string message = $"Failed authentication for user {userName}. Reason: {result.ToString()}";

                _logger.LogWarning(message);

                user.MarkLoginFailed(message,
                    _currentUserService.CorrelationId,
                    _currentUserService.IpAddress,
                    _currentUserService.UserAgent);

                await UpdateUser(user);

                return AuthenticationContext.Failure($"Credentials for '{userName} aren't valid.");
            }

            _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

            user.MarkLoginSuccess(_currentUserService.CorrelationId,
                _currentUserService.IpAddress,
                _currentUserService.UserAgent);

            await UpdateUser(user);

            return AuthenticationContext.Authenticated(user);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<User> GetUserById(int id)
    {
        var user = await _applicationDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return user!;
    }

    public async Task<UserShortInfo> GetUserShortInfo(int id)
    {
        var user = await _applicationDbContext.Users
            .Where(u => u.Id == id)
            .AsNoTracking()
            .Select(UserProjection.Projection)
            .FirstOrDefaultAsync();

        return user!;
    }

    public async Task<User?> GetUserAggregateAsync(int id, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserAddresses)
            .Include(u => u.UserContacts)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<bool> EmailExistsAsync(int excludeUserId, string normalizedEmail, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != excludeUserId &&
                u.NormalizedEmail == normalizedEmail,
                ct);
    }

    public async Task<bool> UserNameExistsAsync(int excludeUserId, string normalizedUserName, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != excludeUserId &&
                u.NormalizedUserName == normalizedUserName,
                ct);
    }

    public async Task<int> CreateUser(User user, string password)
    {
        _passwordService.SetPassword(user, password);

        _applicationDbContext.Users.Add(user);

        var rows = await _applicationDbContext.SaveChangesAsync();

        return rows;
    }

    public async Task<int> UpdateUser(User user)
    {
        _applicationDbContext.Users.Update(user);

        var rows = await _applicationDbContext.SaveChangesAsync();

        return rows;
    }
}