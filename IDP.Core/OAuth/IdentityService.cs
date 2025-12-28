using IDP.Core.Admin.Tenants;
using IDP.Core.OAuth;

namespace IDP.Core.TokenServices;

internal class IdentityService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAppLogger<IdentityService> _logger;
    private readonly AuthorizationCodeService _authorizationService;
    private readonly TenantService _tenantService;

    public IdentityService(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAppLogger<IdentityService> logger,
        TenantService tenantService,
        AuthorizationCodeService authorizationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tenantService = tenantService;
        _authorizationService = authorizationService;
    }

    public async Task<AuthResponse> Authenticate(AuthRequest request)
    {
        _logger.LogInfo("Authentication attempt for user: {UserName}", request.UserName);

        var user = await _userManager.FindByNameAsync(request.UserName)
         ?? await _userManager.FindByEmailAsync(request.UserName);

        if (user == null)
        {
            _logger.LogWarning("User not found with username or email: {UserName}", request.UserName);
            return AuthResponse.Failure($"User with {request.UserName} not found.");
        }

        _logger.LogDebug("Found user {UserId} for authentication", user.Id);

        var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password,
            false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed authentication for user {UserName}. Reason: {FailureReason}",
                request.UserName, result.ToString());
            return AuthResponse.Failure($"Credentials for '{request.UserName} aren't valid.");
        }

        _logger.LogInfo("Successful authentication for user {UserId}", user.Id);

        var twoFactorEnabledOnTenant = await _tenantService.CheckTwoFactorEnabled(user.TenantId);
        var twoFactorEnabled = twoFactorEnabledOnTenant && user.TwoFactorEnabled;

        return AuthResponse.Success(user.Id, twoFactorEnabled);
    }
}