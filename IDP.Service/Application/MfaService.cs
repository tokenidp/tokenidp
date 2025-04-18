using IDP.Service.Security;

namespace IDP.Service.Application;

public class MfaService
{
    private readonly UserManager<User> _userManager;
    private readonly IAppLogger<MfaService> _logger;
    private readonly PreAuthorizationRepo _preAuthorizationRepo;
    private readonly IEmailSetting _emailSetting;
    private readonly EmailProviderFactory _emailProviderFactory;

    public MfaService(IAppLogger<MfaService> logger,
        IEmailSetting emailSetting,
        PreAuthorizationRepo preAuthorizationRepo,
        AuthorizationRepo authorizationRepo,
        UserManager<User> userManager,
        EmailProviderFactory emailProviderFactory)
    {
        _logger = logger;
        _emailSetting = emailSetting;
        _preAuthorizationRepo = preAuthorizationRepo;
        _userManager = userManager;
        _emailProviderFactory = emailProviderFactory;
    }

    public async Task<AuthResponse> GenerateMfaCode(AuthRequest request, int userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", correlationId);

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        PreAuthorization preAuthorization = new(userId,
            mfaCode,
            correlationId,
            request.ClientId,
            request.RedirectUri,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            DateTime.UtcNow.AddMinutes(5),
            request.Scopes);

        await _preAuthorizationRepo.SavePreAuthorization(preAuthorization);

        _logger.LogInfo("Saved authorization code for user {UserId} (Client: {ClientId})",
            userId, request.ClientId);

        //await SendNotification(userId, mfaCode);

        return AuthResponse.Success(userId, correlationId, true);
    }

    public async Task<(AuthRequest, AuthResponse)> VerifyMfaRequest(MfaRequest request)
    {
        var preAuthoriztion = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthoriztion == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId} and Code:{request.Code}";
            _logger.LogWarning(message);
            return (default, AuthResponse.Failure(message));
        }

        var authRequest = AuthRequest.Create(preAuthoriztion.ClientId,
            preAuthoriztion.RedirectUri,
            preAuthoriztion.CodeChallenge,
            preAuthoriztion.CodeChallengeMethod,
            preAuthoriztion.Scopes);

        return (authRequest, default);
    }

    private async Task SendNotification(int userId, string mfaCode)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        await _emailSetting.PopulateEmailSettings(user.TenantId);

        var data = new Dictionary<string, string> { { "MfaCode", mfaCode } };

        var notificationRequest = NotificationRequest.Create(user.Email, user.FullName, data);

        var emailNotification = _emailProviderFactory.GetService(_emailSetting.EmailProviderType);

        await emailNotification.SendNotificationAsync(notificationRequest);
    }
}
