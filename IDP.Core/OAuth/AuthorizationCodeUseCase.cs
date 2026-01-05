using IDP.Core.Model;
using IDP.Core.OAuth.DomainServices;
using IDP.Core.OAuth.Interfaces;

namespace IDP.Core.OAuth;

internal sealed class AuthorizationCodeUseCase : IAuthorizationCodeUseCase
{
    private readonly IdentityService _identityService;
    private readonly ClientService _clientService;
    private readonly AuthorizationCodeService _authorizationCodeService;
    private readonly IMfaService _mfaService;
    private readonly IAppLogger<AuthorizationCodeUseCase> _logger;

    internal AuthorizationCodeUseCase(IdentityService identityService,
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
        var response = await _identityService.Authenticate(request);

        if (!response.IsSuccess)
        {
            return response;
        }

        if (response.TwoFactorEnabled.HasValue && response.TwoFactorEnabled.Value)
        {
            response = await _mfaService.GenerateMfaCode(request, response.UserId.Value);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return response;
        }

        response = await _authorizationCodeService.GenerateAuthorizationCode(request, response.UserId.Value);

        return response;
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

        var clientDto = await _clientService.GetClient(clientId);

        return ClientValidationResult.Create(clientDto != null, clientDto?.Scopes ?? Array.Empty<string>());
    }
}
