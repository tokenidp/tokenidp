using IDP.Core.OAuth.Model;

namespace IDP.Core.TokenServices.UseCases;

internal class AuthenticationUseCase
{
    private readonly IdentityService _identityService;
    private readonly MfaService _mfaService;
    private readonly IAppLogger<AuthenticationUseCase> _logger;

    public AuthenticationUseCase(IdentityService identityService,
        IAppLogger<AuthenticationUseCase> appLogger,
        MfaService mfaService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaService = mfaService;
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

            return Results.Ok(response);
        }

        response = await _identityService.GenerateAuthorizationCode(request, response.UserId.Value);

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

        authResponse = await _identityService.GenerateAuthorizationCode(authRequest, request.UserId);

        return Results.Ok(authResponse);
    }

    public async Task<IResult> ResendMfaCode(MfaRequest request)
    {
        if (string.IsNullOrEmpty(request.CorrelationId))
        {
            var errorResult = ApiResult<ApiError>.Failure(
                            ApiError.Failure("Correlation Id cannot be empty."));

            return Results.Json(errorResult, statusCode: StatusCodes.Status400BadRequest);
        }

        var response = await _mfaService.ResendMfaCode(request);

        if (!response.IsSuccess)
        {
            var errorResult = ApiResult<ApiError>.Failure(
                            ApiError.Failure(response.Error));

            return Results.Json(errorResult, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(response);
    }
}
