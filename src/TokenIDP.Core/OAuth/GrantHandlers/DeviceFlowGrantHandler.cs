using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal class DeviceFlowGrantHandler : ITokenGrantHandler
{
    private readonly IAuthorizationRepository _authorizationStore;
    private readonly IAuthenticationService _authenticateService;
    private readonly IUserRepository _userStore;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenIssuerUseCase _tokenService;

    public DeviceFlowGrantHandler(IUserRepository userStore,
        IAuthorizationRepository authorizationStore,
        TokenContextUseCase tokenContextUseCase,
        IAuthenticationService authenticateService,
        TokenIssuerUseCase tokenService)
    {
        _userStore = userStore;
        _authorizationStore = authorizationStore;
        _tokenContextUseCase = tokenContextUseCase;
        _authenticateService = authenticateService;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        var deviceCodeHash = SecretHasher.HashSecret(request.DeviceCode!);

        var deviceRequest = await _authorizationStore
            .GetDeviceAuthorizationByDCode(deviceCodeHash);

        if (deviceRequest == null)
            return TokenResponse.Failure("invalid_grant");

        deviceRequest.RegisterPoll();
        await _authorizationStore.UpdateDeviceAuthorization(deviceRequest);

        var tokenContext = await _tokenContextUseCase
            .BuildTokenContextAsync(
                request.ClientId,
                Convert.ToInt32(deviceRequest.SubjectUserId),
                GrantTypes.device_code,
                deviceRequest.Scopes);

        var token = await _tokenService.IssueTokenAsync(tokenContext);

        deviceRequest.MarkConsumed();
        await _authorizationStore.UpdateDeviceAuthorization(deviceRequest);

        return token;
    }
}

