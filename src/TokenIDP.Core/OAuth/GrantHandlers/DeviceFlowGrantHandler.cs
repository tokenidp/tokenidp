using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Core.Foundation.Abstractions.Stores;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal class DeviceFlowGrantHandler : ITokenGrantHandler
{
    private readonly IAuthorizationStore _authorizationStore;
    private readonly IAuthenticationService _authenticateService;
    private readonly IUserStore _userStore;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenIssuerUseCase _tokenService;

    public DeviceFlowGrantHandler(IUserStore userStore,
        IAuthorizationStore authorizationStore,
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
