using IDP.Core.Policies;
using IDP.Core.UseCases;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.GrantHandlers;

internal class PasswordGrantHandler : ITokenGrantHandler
{
    private readonly IIdentityStore _identityService;
    private readonly IMfaUseCase _mfaUseCase;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;
    private readonly TenantUserMfaPolicy _mfaPolicy;
    private readonly TokenContextUseCase _tokenContextUseCase;

    public PasswordGrantHandler(IIdentityStore identityService,
        IAppLogger<AuthorizationCodeUseCase> appLogger,
        IMfaUseCase mfaUseCase,
        TokenContextUseCase tokenContextUseCase,
        TenantUserMfaPolicy mfaPolicy,
        TokenIssuerUseCase tokenService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaUseCase = mfaUseCase;
        _tokenContextUseCase = tokenContextUseCase;
        _mfaPolicy = mfaPolicy;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        var context = await _identityService
            .Authenticate(request.TenantId, request.UserName, request.Password);

        if (!context.IsSuccess)
        {
            return TokenResponse.Failure(context.Error);
        }

        var checkTwoFactorEnabled = await _mfaPolicy.IsMfaRequiredAsync(context);

        if (checkTwoFactorEnabled)
        {
            var authRequest = AuthorizationRequest.Create(request.ClientId,
                request.RedirectUri,
                string.Empty,
                string.Empty,
                request.Scope);

            var authResponse = await _mfaUseCase.GenerateMfaCode(authRequest, context.UserId);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return TokenResponse.Success(authResponse.TwoFactorEnabled ?? false);
        }

        var tokenInfo = await _tokenContextUseCase
            .BuildTokenContextAsync(request.ClientId, context.UserId);

        var token = await _tokenService.IssueTokenAsync(tokenInfo);

        return token;
    }

    public async Task<TokenResponse> VerifyMfaCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaUseCase.VerifyMfaRequest(request);

        if (authResponse != null && !authResponse.IsSuccess)
        {
            return TokenResponse.Failure(authResponse.Error);
        }

        var tokenInfo = await _tokenContextUseCase
            .BuildTokenContextAsync(authRequest?.ClientId!, request.UserId);

        var token = await _tokenService.IssueTokenAsync(tokenInfo);

        return token!;
    }
}
