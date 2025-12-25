using IDP.Core.Admin.Clients;
using IDP.Core.OAuth.Model;

namespace IDP.Core.TokenServices.UseCases;

internal class AccessTokenUseCase
{
    private readonly TokenServiceFactory _tokenServiceFactory;
    private readonly ClientService _clientService;
    private readonly IAppLogger<AccessTokenUseCase> _logger;

    public AccessTokenUseCase(TokenServiceFactory tokenServiceFactory,
        ClientService clientService,
        IAppLogger<AccessTokenUseCase> appLogger)
    {
        _tokenServiceFactory = tokenServiceFactory;
        _clientService = clientService;
        _logger = appLogger;
    }

    public async Task<IResult> GetAccessToken(TokenRequest request, string ipAddress)
    {
        _logger.LogInfo("GetAccessToken called for ClientId: {ClientId} from IP: {IP}", request.ClientId, ipAddress);

        var tokenType = await _clientService.GetClientTokenType(request.ClientId);

        if (!Enum.IsDefined(typeof(TokenType), tokenType))
        {
            _logger.LogWarning("TokenType not found for ClientId: {ClientId}", request.ClientId);

            var errorResult = ApiResult<ApiError>.Failure(
                           ApiError.Failure("Invalid client."));

            return Results.BadRequest(errorResult);
        }

        ITokenService _tokenService = _tokenServiceFactory.GetService(tokenType);

        var response = await _tokenService.GenerateToken(request, ipAddress);

        _logger.LogInfo("Token generated for ClientId: {ClientId} with TokenType: {TokenType}", request.ClientId, tokenType);

        return Results.Ok(ApiResult<TokenResponse>.Success(response));
    }
}
