using IDP.Core.UseCases;
using IDP.Domain.AggregateRoots.Clients;

namespace IDP.Core.GrantHandlers;

internal sealed class TokenGrantUseCase : ITokenGrantUseCase
{
    private readonly GrantTypeValidatorUseCase _grantTypeValidator;
    private readonly TokenGrantFactory _tokenGrantFactory;
    private readonly IAppLogger<TokenGrantUseCase> _logger;

    public TokenGrantUseCase(TokenGrantFactory tokenGrantFactory,
        IAppLogger<TokenGrantUseCase> logger,
        GrantTypeValidatorUseCase grantTypeValidator)
    {
        _tokenGrantFactory = tokenGrantFactory;
        _logger = logger;
        _grantTypeValidator = grantTypeValidator;
    }

    public async Task<IResult> GetAccessToken(TokenRequest request)
    {
        _logger.LogInfo("GetAccessToken called for ClientId: {ClientId} from IP: {IP}", request.ClientId, request.IpAddress);

        if (!await _grantTypeValidator.ValidateGrantType(request.GrantType, request.ClientId))
        {
            var errorResult = ApiResult<ApiError>.Failure(
                           ApiError.Failure("Invalid grant type."));

            return Results.BadRequest(errorResult);
        }

        Enum.TryParse<GrantTypes>(request.GrantType, ignoreCase: true, out var parsedGrantType);

        ITokenGrantHandler tokenGrantHandler = _tokenGrantFactory.GetService(parsedGrantType);

        var response = await tokenGrantHandler.HandleAsync(request);

        _logger.LogInfo("Token generated for ClientId: {ClientId} for grant type: {GrantType}", request.ClientId, request.GrantType);

        return Results.Ok(ApiResult<TokenResponse>.Success(response));
    }
}
