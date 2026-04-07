using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal sealed class TokenGrantPipeline : ITokenGrantUseCase
{
    private readonly GrantTypeValidatorUseCase _grantTypeValidator;
    private readonly TokenGrantFactory _tokenGrantFactory;
    private readonly IAppLogger<TokenGrantPipeline> _logger;

    public TokenGrantPipeline(TokenGrantFactory tokenGrantFactory,
        IAppLogger<TokenGrantPipeline> logger,
        GrantTypeValidatorUseCase grantTypeValidator)
    {
        _tokenGrantFactory = tokenGrantFactory;
        _logger = logger;
        _grantTypeValidator = grantTypeValidator;
    }

    public async Task<IResult> GetAccessToken(TokenRequest request)
    {
        _logger.LogInfo("GetAccessToken called for ClientId: {ClientId} from IP: {IP}"
            , request.ClientId, request.IpAddress ?? string.Empty);

        try
        {
            var (grantType, tenantId) = await _grantTypeValidator
                .ValidateGrantType(request.GrantType, request.ClientId);

            ITokenGrantHandler tokenGrantHandler = _tokenGrantFactory.GetService(grantType);

            request.SetTenantId(tenantId);

            TokenResponse response = await tokenGrantHandler.HandleAsync(request);

            _logger.LogInfo("Token generated for ClientId: {ClientId} for grant type: {GrantType}",
                request.ClientId, request.GrantType);

            return Results.Ok(ApiResult<TokenResponse>.Success(response));
        }
        catch (TokenRequestValidationException ex)
        {
            return TokenRequestValidationResultFactory.Create(ex);
        }
    }
}

