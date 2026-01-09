namespace IDP.Core.OAuth;

internal sealed class AuthorizationCodeUseCase : IAuthorizationCodeUseCase
{
    private readonly IIdentityStore _identityService;
    private readonly IMfaService _mfaService;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;
    private readonly ClientService _clientService;
    private readonly AuthorizationCodeService _authorizationCodeService;

    internal AuthorizationCodeUseCase(IIdentityStore identityService,
        IAppLogger<AuthorizationCodeUseCase> appLogger,
        IMfaService mfaService,
        AuthorizationCodeService authorizationCodeService,
        ClientService clientService)
    {
        _identityService = identityService;
        _logger = appLogger;
        _mfaService = mfaService;
        _authorizationCodeService = authorizationCodeService;
        _clientService = clientService;
    }

    public async Task<AuthResponse> Authenticate(AuthRequest request)
    {
        var response = await _identityService.Authenticate(request.UserName, request.Password);

        if (!response.IsSuccess)
        {
            return AuthResponse.Failure(response.Error);
        }

        AuthResponse authResponse = default;

        if (response.TwoFactorEnabled.HasValue && response.TwoFactorEnabled.Value)
        {
            authResponse = await _mfaService.GenerateMfaCode(request, response.UserId ?? 0);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return authResponse;
        }

        authResponse = await _authorizationCodeService.GenerateAuthorizationCode(request, response.UserId.Value);

        return authResponse;
    }

    public async Task<AuthResponse> VerifyCode(MfaRequest request)
    {
        var (authRequest, authResponse) = await _mfaService.VerifyMfaRequest(request);

        if (authResponse != null && !authResponse.IsSuccess)
        {
            return authResponse;
        }

        authResponse = await _authorizationCodeService.GenerateAuthorizationCode(authRequest, request.UserId);

        return authResponse;
    }

    public async Task<ClientValidationResult> ValidateClient(string clientId)
    {
        _logger.LogDebug("IsValidClient: Checking is valid client for client: {ClientId}", clientId);

        return await _clientService.ValidateClient(clientId);
    }
}
