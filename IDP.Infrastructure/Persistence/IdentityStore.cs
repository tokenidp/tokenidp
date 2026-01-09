namespace IDP.Core.OAuth;

internal sealed class IdentityStore : IIdentityStore
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAppLogger<IdentityStore> _logger;
    private readonly ITenantStore _tenantService;

    public IdentityStore(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAppLogger<IdentityStore> logger,
        ITenantStore tenantService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<AuthenticationResult> Authenticate(string userName, string password)
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
                return AuthenticationResult.Failure($"User with {userName} not found.");
            }

            _logger.LogDebug("Found user {UserId} for authentication", user.Id);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed authentication for user {UserName}. Reason: {FailureReason}",
                    userName, result.ToString());
                return AuthenticationResult.Failure($"Credentials for '{userName} aren't valid.");
            }

            _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

            var twoFactorEnabledOnTenant = await _tenantService.CheckTwoFactorEnabled(user.TenantId);
            var twoFactorEnabled = twoFactorEnabledOnTenant && user.TwoFactorEnabled;

            return AuthenticationResult.Success(user.Id, twoFactorEnabled);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<User> FindByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        return user;
    }
}