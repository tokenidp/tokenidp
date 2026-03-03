using IDP.Core.Policies;
using IDP.Core.UseCases;

namespace IDP.Core.GrantHandlers;

internal class PasswordGrantHandler : ITokenGrantHandler
{
    private readonly IAuthenticationService _identityService;
    private readonly IMfaUseCase _mfaUseCase;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TenantUserMfaPolicy _mfaPolicy;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;

    public PasswordGrantHandler(IAuthenticationService identityService,
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
            GenerateMfaCommand mfaRequest = new()
            {
                UserId = context.UserId,
                ClientId = request.ClientId,
                RedirectUri = request.RedirectUri,
                Scopes = request.Scope
            };

            var authResponse = await _mfaUseCase.GenerateMfaCode(mfaRequest);

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
