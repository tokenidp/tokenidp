using IDP.Domain.AggregateRoots.Authorization;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class MfaUseCase : IMfaUseCase
{
    private readonly IEmailSetting _emailSetting;
    private readonly IIdentityStore _identityStore;
    private readonly IPreAuthorizationStore _preAuthorizationRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly EmailProviderFactory _emailProviderFactory;
    private readonly IAppLogger<MfaUseCase> _logger;

    public MfaUseCase(IIdentityStore identityStore,
        IEmailSetting emailSetting,
        IPreAuthorizationStore preAuthorizationRepo,
        EmailProviderFactory emailProviderFactory,
        IAppLogger<MfaUseCase> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _emailSetting = emailSetting;
        _preAuthorizationRepo = preAuthorizationRepo;
        _emailProviderFactory = emailProviderFactory;
        _identityStore = identityStore;
        _currentUserService = currentUserService;
    }

    public async Task<AuthorizationResponse> GenerateMfaCode(AuthorizationRequest request, int userId)
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

        await _preAuthorizationRepo.Create(preAuthorization);

        _logger.LogInfo("Saved authorization code for user {UserId} (Client: {ClientId})",
            userId, request.ClientId);

        var user = await _identityStore.GetUserById(userId);

        await SendNotification(user.TenantId, user.FullName, user.Email ?? string.Empty, mfaCode);

        user.MarkMfaChallengeSent(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.SaveChangesAsync();

        return AuthorizationResponse.Success(userId, correlationId, true);
    }

    public async Task<(AuthorizationRequest?, AuthorizationResponse)> VerifyMfaRequest(MfaRequest request)
    {
        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId} and Code:{request.Code}";
            _logger.LogWarning(message);
            return (default, AuthorizationResponse.Failure(message));
        }

        var authResponse = AuthorizationResponse.Success(preAuthorization.UserId, preAuthorization.CorrelationId, true);

        var authRequest = AuthorizationRequest.Create(preAuthorization.ClientId,
            preAuthorization.RedirectUri,
            preAuthorization.CodeChallenge,
            preAuthorization.CodeChallengeMethod,
            preAuthorization.Scopes);

        var user = await _identityStore.GetUserById(request.UserId);

        user.MarkMfaValidated(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.SaveChangesAsync();

        return (authRequest, authResponse);
    }

    public async Task<AuthorizationResponse> ResendMfaCode(MfaRequest request)
    {
        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", request.CorrelationId);

        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId}";

            _logger.LogWarning(message);

            return AuthorizationResponse.Failure(message);
        }

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        preAuthorization.UpdateMfaCode(request.UserId, mfaCode, DateTime.UtcNow.AddMinutes(5));

        await _preAuthorizationRepo.Update(preAuthorization);

        _logger.LogInfo("Resend mfa code for user {UserId}", request.UserId);

        var user = await _identityStore.GetUserById(request.UserId);

        await SendNotification(user.TenantId, user.FullName, user.Email ?? string.Empty, mfaCode);

        user.MarkMfaChallengeSent(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.SaveChangesAsync();

        return AuthorizationResponse.Success(request.UserId, request.CorrelationId, true);
    }

    private async Task SendNotification(int tenantId,
        string fullName,
        string email,
        string mfaCode)
    {
        await _emailSetting.PopulateEmailSettings(tenantId);

        var tokens = new Dictionary<string, string>
        {
            { "<%NAME%>", fullName},
            { "<%MFA_CODE%>",  mfaCode},
            { "<%YEAR_REGISTERED%>",  DateTime.Now.Year.ToString()}
        };

        var notificationRequest = NotificationRequest.Create(email,
            fullName,
            tokens,
            "Your two-factor Verification Code!",
            string.Empty,
            emailHtml);

        var emailNotification = _emailProviderFactory.GetService(_emailSetting.EmailProviderType);

        await emailNotification.SendNotificationAsync(notificationRequest);
    }

    private readonly string emailHtml = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='UTF-8'>
                            <title>Your Verification Code</title>
                        </head>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; color: #333;'>
                            <div style='max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #2563eb; margin-bottom: 16px;'>Your Verification Code</h2>
                                <p>Hi <%NAME%>,</p>
                                <p>Use the following code to verify your identity:</p>
                                <div style='
                                    font-size: 24px;
                                    font-weight: bold;
                                    color: #2563eb;
                                    margin: 20px 0;
                                    padding: 10px 0;
                                    letter-spacing: 2px;
                                '><%MFA_CODE%></div>
                                <p>This code expires in <strong style='color: #dc2626;'>10 minutes</strong>. Do not share it with anyone.</p>
                                <div style='margin-top: 30px; font-size: 12px; color: #6b7280;'>
                                    <p>If you didn't request this, please ignore this email.</p>
                                    <p>© {DateTime.Now.Year} SmartDevCon. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";
}
