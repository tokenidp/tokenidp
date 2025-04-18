namespace IDP.Service.Controllers;

[Route("[controller]")]
[ApiController]
[ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.Unauthorized)]
public class AuthenticateController : ControllerBase
{
    private readonly IdentityService _identityService;
    private readonly MfaService _mfaService;
    private readonly IAppLogger<AuthenticateController> _logger;

    public AuthenticateController(IdentityService identityService,
        IAppLogger<AuthenticateController> appLogger,
        MfaService mfaService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaService = mfaService;
    }

    [HttpPost]
    public async Task<IActionResult> Authenticate(AuthRequest request)
    {
        _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

        var response = await _identityService.Authenticate(request);

        if (!response.IsSuccess)
            return Unauthorized(Result<AuthResponse>.Failure(response.Error));

        if (response.TwoFactorEnabled.HasValue && response.TwoFactorEnabled.Value)
        {
            response = await _mfaService.GenerateMfaCode(request, response.UserId.Value);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return Ok(Result<AuthResponse>.Success(response));
        }

        response = await _identityService.GenerateAuthorizationCode(request, response.UserId.Value);

        _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

        return Ok(Result<AuthResponse>.Success(response));
    }

    [HttpPost("mfa")]
    public async Task<IActionResult> VerifyCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaService.VerifyMfaRequest(request);

        if (!authResponse.IsSuccess)
            return Unauthorized(Result<AuthResponse>.Failure(authResponse.Error));

        authResponse = await _identityService.GenerateAuthorizationCode(authRequest, request.UserId);

        _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

        return Ok(Result<AuthResponse>.Success(authResponse));
    }
}