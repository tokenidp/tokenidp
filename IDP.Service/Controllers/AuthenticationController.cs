namespace IDP.Service.Controllers;

[Route("authenticate")]
public class AuthenticationController : ApiControllerBase
{
    private readonly IdentityService _identityService;
    private readonly MfaService _mfaService;
    private readonly IAppLogger<AuthenticationController> _logger;

    public AuthenticationController(IdentityService identityService,
        IAppLogger<AuthenticationController> appLogger,
        MfaService mfaService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaService = mfaService;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Authenticate(AuthRequest request)
    {
        _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

        var response = await _identityService.Authenticate(request);

        if (!response.IsSuccess)
            return UnauthorizedResult(ApiError.Failure(response.Error));

        if (response.TwoFactorEnabled.HasValue && response.TwoFactorEnabled.Value)
        {
            response = await _mfaService.GenerateMfaCode(request, response.UserId.Value);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return OkResult(response);
        }

        response = await _identityService.GenerateAuthorizationCode(request, response.UserId.Value);

        _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

        return OkResult(response);
    }

    [HttpPost("verify-mfa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> VerifyCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaService.VerifyMfaRequest(request);

        if (authResponse != null && !authResponse.IsSuccess)
            return UnauthorizedResult(ApiError.Failure(authResponse.Error));

        authResponse = await _identityService.GenerateAuthorizationCode(authRequest, request.UserId);

        _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

        return OkResult(authResponse);
    }

    [HttpPost("resend-mfa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> ResendMfaCode(MfaRequest request)
    {
        _logger.LogInfo("Resend Mfa Code process started for user: {Username}", request.UserName);

        if (string.IsNullOrEmpty(request.CorrelationId))
            return BadRequest(ApiError.Failure("Correlation Id cannot be empty."));

        var response = await _mfaService.ResendMfaCode(request);

        if (!response.IsSuccess)
            return UnauthorizedResult(ApiError.Failure(response.Error));

        _logger.LogInfo("Resend Mfa Code process completed for user: {Username}", request.UserName);

        return OkResult(response);
    }
}