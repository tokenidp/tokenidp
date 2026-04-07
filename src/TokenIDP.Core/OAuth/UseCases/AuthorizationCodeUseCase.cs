using TokenIDP.Core.OAuth.Policies;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class AuthorizationCodeUseCase : IAuthorizationCodeUseCase
{
    private readonly IAuthenticationService _identityService;
    private readonly IAuthorizationRepository _authorizationStore;
    private readonly IMfaUseCase _mfaUseCase;
    private readonly IClientRepository _clientStore;
    private readonly TenantUserMfaPolicy _mfaPolicy;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;
    private readonly IUserSignInService _userSignInService;

    internal AuthorizationCodeUseCase(IAuthenticationService identityService,
        IAppLogger<AuthorizationCodeUseCase> appLogger,
        IMfaUseCase mfaUseCase,
        IAuthorizationRepository authorizationStore,
        TokenContextUseCase tokenContextUseCase,
        TenantUserMfaPolicy mfaPolicy,
        IClientRepository clientStore,
        IUserSignInService userSignInService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaUseCase = mfaUseCase;
        _authorizationStore = authorizationStore;
        _tokenContextUseCase = tokenContextUseCase;
        _mfaPolicy = mfaPolicy;
        _clientStore = clientStore;
        _userSignInService = userSignInService;
    }

    public async Task<AuthorizationResponse> Authenticate(AuthorizationRequest request)
    {
        var context = await _identityService
            .Authenticate(request.TenantId, request.UserName, request.Password);

        if (!context.IsSuccess)
        {
            return AuthorizationResponse.Failure(context.Error);
        }

        AuthorizationResponse authResponse = default!;

        var checkTwoFactorEnabled = await _mfaPolicy.IsMfaRequiredAsync(context);

        if (checkTwoFactorEnabled)
        {
            authResponse = await _mfaUseCase.GenerateMfaForAuthorizeAsync(request, context.UserId);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return authResponse;
        }

        await _userSignInService.SignInAsync(context.UserId,
                context.User.UserName,
                context.User.Email,
                context.User.TenantId,
                request.RememberMe,
                CancellationToken.None);

        return AuthorizationResponse.Success(context.UserId, false);
    }

    public async Task<TokenContext> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest)
    {
        _logger.LogInfo("Token request received for ClientId: {ClientId} with Code: {Code}",
            tokenRequest.ClientId, tokenRequest.Code ?? string.Empty);

        var authorizationCode = await ValidateAuthorizationCode(tokenRequest.Code!, tokenRequest.ClientId);

        var calculatedCodeChallenge = PkceHelper.CalculateCodeChallenge(tokenRequest.CodeVerifier!);

        if (calculatedCodeChallenge != authorizationCode.CodeChallenge)
        {
            _logger.LogWarning("Invalid code verifier for ClientId: {ClientId}, UserId: {UserId}",
                tokenRequest.ClientId, authorizationCode.UserId);

            throw new UnauthorizedAccessException("Invalid code verifier.");
        }

        if (!string.Equals(
                tokenRequest.RedirectUri?.Trim(),
                authorizationCode.RedirectUri,
                StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Invalid redirect_uri for ClientId: {ClientId}, UserId: {UserId}",
                tokenRequest.ClientId,
                authorizationCode.UserId);

            throw new TokenRequestValidationException("invalid_grant", "Invalid redirect_uri.");
        }

        var validationResult = await _clientStore.GetClientShortInfo(tokenRequest.ClientId);

        if (validationResult != null && !validationResult.IsValidClient)
        {
            _logger.LogWarning("ClientId: {ClientId} is invalid", tokenRequest.ClientId);

            throw new NotFoundException("Client not found.");
        }

        var tokenInfo = await _tokenContextUseCase
            .BuildTokenContextAsync(
                tokenRequest.ClientId,
                authorizationCode.UserId,
                GrantTypes.authorization_code,
                authorizationCode.Scopes,
                authorizationCode.RememberMe);

        return tokenInfo;
    }

    public async Task<AuthorizationResponse> GenerateAuthorizationCode(
        AuthorizationRequest request,
        int userId)
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
            request.RememberMe,
            request.Scopes);

        var id = await _authorizationStore.CreateAuthorization(authorizationCode);

        _logger.LogInfo("Saved authorization code {Id} for user {UserId} - Client: {ClientId}.",
            id, userId, request.ClientId);

        return AuthorizationResponse.Success(code);
    }

    private async Task<AuthorizationCode> ValidateAuthorizationCode(string code, string clientId)
    {
        var authorizationCode = await _authorizationStore.GetByAuthCode(code, clientId);

        if (authorizationCode == null || authorizationCode.Expiry <= DateTime.UtcNow
            || authorizationCode.IsUsed || authorizationCode.Code != code)
        {
            _logger.LogWarning("Authorization code {code} not found or expired.", code);

            throw new UnauthorizedAccessException("Authorization code {code} not found or expired.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        var id = _authorizationStore.UpdateAuthorization(authorizationCode);

        return authorizationCode;
    }
}

