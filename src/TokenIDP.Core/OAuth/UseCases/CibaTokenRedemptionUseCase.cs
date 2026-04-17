using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Domain;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaTokenRedemptionUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenIssuerUseCase _tokenIssuerUseCase;

    public CibaTokenRedemptionUseCase(
        IAuthorizationRepository authorizationRepository,
        IClientRepository clientRepository,
        TokenContextUseCase tokenContextUseCase,
        TokenIssuerUseCase tokenIssuerUseCase)
    {
        _authorizationRepository = authorizationRepository;
        _clientRepository = clientRepository;
        _tokenContextUseCase = tokenContextUseCase;
        _tokenIssuerUseCase = tokenIssuerUseCase;
    }

    public async Task<TokenResponse> RedeemAsync(TokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AuthReqId))
        {
            throw new TokenRequestValidationException("invalid_request", "auth_req_id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            throw new TokenRequestValidationException("invalid_client", "Client authentication is required.");
        }

        var authReqIdHash = SecretHasher.HashSecret(request.AuthReqId);
        var cibaRequest = await _authorizationRepository.GetBackchannelAuthenticationRequestByHashAsync(authReqIdHash, ct);

        if (cibaRequest == null ||
            !string.Equals(cibaRequest.ClientId, request.ClientId, StringComparison.Ordinal))
        {
            throw new TokenRequestValidationException("invalid_grant", "Invalid auth_req_id.");
        }

        var client = await _clientRepository.GetActiveByClientId(request.ClientId);
        if (!client.CibaEnabled || client.BackchannelTokenDeliveryMode != CibaTokenDeliveryModes.Poll)
        {
            throw new TokenRequestValidationException("unauthorized_client", "The client is not authorized to use poll mode.");
        }

        try
        {
            cibaRequest.RegisterPoll();
        }
        catch (DomainException ex)
        {
            await _authorizationRepository.UpdateBackchannelAuthenticationRequest(cibaRequest, ct);
            throw MapDomainException(ex);
        }

        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(cibaRequest, ct);

        var userId = cibaRequest.UserId.GetValueOrDefault();
        if (userId <= 0)
        {
            throw new TokenRequestValidationException("invalid_grant", "Invalid auth_req_id.");
        }

        var tokenContext = await _tokenContextUseCase.BuildTokenContextAsync(
            request.ClientId,
            userId,
            GrantTypes.ciba,
            cibaRequest.RequestedScopes);

        var token = await _tokenIssuerUseCase.IssueTokenAsync(tokenContext);

        cibaRequest.MarkTokenIssued();
        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(cibaRequest, ct);

        return token;
    }

    private static TokenRequestValidationException MapDomainException(DomainException exception)
    {
        return exception.Message switch
        {
            "authorization_pending" => new TokenRequestValidationException("authorization_pending", "Authorization is still pending."),
            "slow_down" => new TokenRequestValidationException("slow_down", "Polling too quickly."),
            "expired_token" => new TokenRequestValidationException("expired_token", "The auth_req_id has expired."),
            "access_denied" => new TokenRequestValidationException("access_denied", "The end-user denied the request."),
            _ => new TokenRequestValidationException("invalid_grant", "Invalid auth_req_id.")
        };
    }
}
