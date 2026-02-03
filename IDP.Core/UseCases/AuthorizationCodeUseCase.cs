using IDP.Core.Policies;
using IDP.Domain.AggregateRoots.Authorization;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class AuthorizationCodeUseCase : IAuthorizationCodeUseCase
{
    private readonly IIdentityStore _identityService;
    private readonly IAuthorizationCodeStore _authorizationCodeStore;
    private readonly IMfaUseCase _mfaUseCase;
    private readonly IClientStore _clientStore;
    private readonly TenantUserMfaPolicy _mfaPolicy;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;

    internal AuthorizationCodeUseCase(IIdentityStore identityService,
        IAppLogger<AuthorizationCodeUseCase> appLogger,
        IMfaUseCase mfaUseCase,
        IAuthorizationCodeStore authorizationCodeStore,
        TokenContextUseCase tokenContextUseCase,
        TenantUserMfaPolicy mfaPolicy,
        IClientStore clientStore)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaUseCase = mfaUseCase;
        _authorizationCodeStore = authorizationCodeStore;
        _tokenContextUseCase = tokenContextUseCase;
        _mfaPolicy = mfaPolicy;
        _clientStore = clientStore;
    }

    public async Task<AuthorizationResponse> Authenticate(AuthorizationRequest request)
    {
        var context = await _identityService.Authenticate(request.UserName, request.Password);

        if (!context.IsSuccess)
        {
            return AuthorizationResponse.Failure(context.Error);
        }

        AuthorizationResponse authResponse = default!;

        var checkTwoFactorEnabled = await _mfaPolicy.IsMfaRequiredAsync(context);

        if (checkTwoFactorEnabled)
        {
            authResponse = await _mfaUseCase.GenerateMfaCode(request, context.UserId);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return authResponse;
        }

        authResponse = await GenerateAuthorizationCode(request, context.UserId);

        return authResponse;
    }

    public async Task<AuthorizationResponse> VerifyMfaCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaUseCase.VerifyMfaRequest(request);

        if (authResponse != null && !authResponse.IsSuccess)
        {
            return authResponse;
        }

        authResponse = await GenerateAuthorizationCode(authRequest!, request.UserId);

        return authResponse;
    }

    public async Task<TokenContext> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest)
    {
        _logger.LogInfo("Token request received for ClientId: {ClientId} with Code: {Code}",
            tokenRequest.ClientId, tokenRequest.Code ?? string.Empty);

        var authorizationCode = await ValidateAuthorizationCode(tokenRequest.Code!);

        var calculatedCodeChallenge = PkceHelper.CalculateCodeChallenge(tokenRequest.CodeVerifier!);

        if (calculatedCodeChallenge != authorizationCode.CodeChallenge)
        {
            _logger.LogWarning("Invalid code verifier for ClientId: {ClientId}, UserId: {UserId}",
                tokenRequest.ClientId, authorizationCode.UserId);

            throw new UnauthorizedAccessException("Invalid code verifier.");
        }

        var validationResult = await _clientStore.GetClientValidation(tokenRequest.ClientId);

        if (validationResult != null && !validationResult.IsValidClient)
        {
            _logger.LogWarning("ClientId: {ClientId} is invalid", tokenRequest.ClientId);

            throw new NotFoundException("Client not found.");
        }

        var tokenInfo = await _tokenContextUseCase.BuildTokenContextAsync(tokenRequest.ClientId, authorizationCode.UserId);

        tokenInfo.AddAuthorizedScopes(authorizationCode.Scopes ?? string.Empty);

        return tokenInfo;
    }

    private async Task<AuthorizationResponse> GenerateAuthorizationCode(AuthorizationRequest request, int userId)
    {
        var code = Guid.NewGuid().ToString();
        _logger.LogDebug("Generated authorization code: {Code}", code);

        AuthorizationCode authorizationCode = new(
            code,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            request.ClientId,
            userId,
            DateTime.UtcNow.AddMinutes(5),
            request.RedirectUri,
            request.Scopes);

        var id = await _authorizationCodeStore.Create(authorizationCode);

        _logger.LogInfo("Saved authorization code {Id} for user {UserId} - Client: {ClientId}.",
            id, userId, request.ClientId);

        return AuthorizationResponse.Success(code);
    }

    private async Task<AuthorizationCode> ValidateAuthorizationCode(string code)
    {
        var authorizationCode = await _authorizationCodeStore.GetByCode(code);

        if (authorizationCode == null || authorizationCode.Expiry <= DateTime.UtcNow
            || authorizationCode.IsUsed || authorizationCode.Code != code)
        {
            _logger.LogWarning("Authorization code {code} not found or expired.", code);

            throw new UnauthorizedAccessException("Authorization code {code} not found or expired.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        var id = _authorizationCodeStore.Update(authorizationCode);

        return authorizationCode;
    }
}
