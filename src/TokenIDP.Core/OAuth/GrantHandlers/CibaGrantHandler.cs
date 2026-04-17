using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal class CibaGrantHandler : ITokenGrantHandler
{
    private readonly CibaTokenRedemptionUseCase _redemptionUseCase;

    public CibaGrantHandler(CibaTokenRedemptionUseCase redemptionUseCase)
    {
        _redemptionUseCase = redemptionUseCase;
    }

    public Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        return _redemptionUseCase.RedeemAsync(request, CancellationToken.None);
    }
}

