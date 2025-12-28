namespace IDP.Core.OAuth.TokenServices;

internal class TokenUseCase
{
    private readonly TokenGrantFactory _tokenGrantFactory;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly IAppLogger<TokenUseCase> _logger;

    public TokenUseCase(TokenGrantFactory tokenGrantFactory,
        TokenValidatorService tokenValidatorService,
        IAppLogger<TokenUseCase> logger)
    {
        _tokenGrantFactory = tokenGrantFactory;
        _tokenValidatorService = tokenValidatorService;
        _logger = logger;
    }

    public async Task<IResult> GetAccessToken(TokenRequest request)
    {
        _logger.LogInfo("GetAccessToken called for ClientId: {ClientId} from IP: {IP}", request.ClientId, request.IpAddress);

        if (!await _tokenValidatorService.ValidateGrantType(request.GrantType, request.ClientId))
        {
            var errorResult = ApiResult<ApiError>.Failure(
                           ApiError.Failure("Invalid grant type."));

            return Results.BadRequest(errorResult);
        }

        Enum.TryParse<GrantType>(request.GrantType, ignoreCase: true, out var parsedGrantType);

        ITokenGrantHandler tokenGrantHandler = _tokenGrantFactory.GetService(parsedGrantType);

        var response = await tokenGrantHandler.HandleAsync(request);

        _logger.LogInfo("Token generated for ClientId: {ClientId} for grant type: {GrantType}", request.ClientId, request.GrantType);

        return Results.Ok(ApiResult<TokenResponse>.Success(response));
    }
}
