using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.OAuth;

internal sealed class IdentityStore : IIdentityStore
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAppLogger<IdentityStore> _logger;

    public IdentityStore(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAppLogger<IdentityStore> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
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
                _logger.LogWarning("User not found with username or email: {UserName}", userName);
                return AuthenticationContext.Failure($"User with {userName} not found.");
            }

            _logger.LogDebug("Found user {UserId} for authentication", user.Id);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed authentication for user {UserName}. Reason: {FailureReason}",
                    userName, result.ToString());
                return AuthenticationContext.Failure($"Credentials for '{userName} aren't valid.");
            }

            _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

            return AuthenticationContext.Authenticated(user);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<User?> FindByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        return user;
    }
}