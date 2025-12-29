using IDP.Common.Notifications;
using IDP.Core.Model;
using IDP.Core.OAuth;

namespace IDP.Core.TokenHandlers;

internal class MfaService
{
    private readonly UserManager<User> _userManager;
    private readonly IAppLogger<MfaService> _logger;
    private readonly PreAuthorizationService _preAuthorizationRepo;
    private readonly IEmailSetting _emailSetting;
    private readonly EmailProviderFactory _emailProviderFactory;

    public MfaService(IAppLogger<MfaService> logger,
        IEmailSetting emailSetting,
        PreAuthorizationService preAuthorizationRepo,
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

        UserPreAuthorization preAuthorization = new(userId,
            mfaCode,
            correlationId,
            request.ClientId,
            request.RedirectUri,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            DateTime.UtcNow.AddMinutes(5),
            request.Scopes);

        await _preAuthorizationRepo.AddPreAuthorization(preAuthorization);

        _logger.LogInfo("Saved authorization code for user {UserId} (Client: {ClientId})",
            userId, request.ClientId);

        await SendNotification(userId, mfaCode);

        return AuthResponse.Success(userId, correlationId, true);
    }

    public async Task<(AuthRequest, AuthResponse)> VerifyMfaRequest(MfaRequest request)
    {
        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId} and Code:{request.Code}";
            _logger.LogWarning(message);
            return (default, AuthResponse.Failure(message));
        }

        var authRequest = AuthRequest.Create(preAuthorization.ClientId,
            preAuthorization.RedirectUri,
            preAuthorization.CodeChallenge,
            preAuthorization.CodeChallengeMethod,
            preAuthorization.Scopes);

        return (authRequest, default);
    }

    public async Task<IResult> ResendMfaCode(MfaRequest request)
    {
        if (string.IsNullOrEmpty(request.CorrelationId))
        {
            var errorResult = ApiResult<ApiError>.Failure(
                            ApiError.Failure("Correlation Id cannot be empty."));

            return Results.Json(errorResult, statusCode: StatusCodes.Status400BadRequest);
        }

        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", request.CorrelationId);

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        preAuthorization.UpdateMfaCode(request.UserId, mfaCode, DateTime.UtcNow.AddMinutes(5));

        await _preAuthorizationRepo.UpdatePreAuthorization(preAuthorization);

        _logger.LogInfo("Resend mfa code for user {UserId}", request.UserId);

        await SendNotification(request.UserId, mfaCode);

        return Results.Ok(ApiResult<AuthResponse>
            .Success(AuthResponse.Success(request.UserId, request.CorrelationId, true)));
    }

    private async Task SendNotification(int userId, string mfaCode)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        await _emailSetting.PopulateEmailSettings(user.TenantId);

        var tokens = new Dictionary<string, string>
        {
            { "<%NAME%>", user.FullName},
            { "<%MFA_CODE%>",  mfaCode}
        };

        var notificationRequest = NotificationRequest.Create(user.Email,
            user.FullName,
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
