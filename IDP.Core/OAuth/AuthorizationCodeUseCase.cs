using IDP.Core.TokenServices;

namespace IDP.Core.OAuth;

internal class AuthorizationCodeUseCase
{
    private readonly IdentityService _identityService;
    private readonly MfaService _mfaService;
    private readonly AuthorizationCodeService _authorizationCodeService;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;

    public AuthorizationCodeUseCase(IdentityService identityService,
        IAppLogger<AuthorizationCodeUseCase> appLogger,
        MfaService mfaService,
        AuthorizationCodeService authorizationCodeService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaService = mfaService;
        _authorizationCodeService = authorizationCodeService;
    }

    public async Task<IResult> Authenticate(AuthRequest request)
    {
        var response = await _identityService.Authenticate(request);

        if (!response.IsSuccess)
        {
            var errorResult = ApiResult<ApiError>.Failure(
                            ApiError.Failure(response.Error));

            return Results.Json(errorResult, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (response.TwoFactorEnabled.HasValue && response.TwoFactorEnabled.Value)
        {
            response = await _mfaService.GenerateMfaCode(request, response.UserId.Value);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return Results.Ok(ApiResult<AuthResponse>.Success(response));
        }

        response = await _authorizationCodeService.GenerateAuthorizationCode(request, response.UserId.Value);

        return Results.Ok(response);
    }

    public async Task<IResult> VerifyCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaService.VerifyMfaRequest(request);

        if (authResponse != null && !authResponse.IsSuccess)
        {
            var errorResult = ApiResult<ApiError>.Failure(
                            ApiError.Failure(authResponse.Error));

            return Results.Json(errorResult, statusCode: StatusCodes.Status401Unauthorized);
        }

        authResponse = await _authorizationCodeService.GenerateAuthorizationCode(authRequest, request.UserId);

        return Results.Ok(ApiResult<AuthResponse>.Success(authResponse));
    }
}
