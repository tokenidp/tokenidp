using IDP.Core.Policies;
using IDP.Domain.AggregateRoots.Authorization;
using IDP.Foundation.Abstractions.Stores;
using IDP.Foundation.Security;

namespace IDP.Core.UseCases;

internal class DeviceAuthenticationUseCase : IDeviceAuthenticationUseCase
{
    private readonly IAuthenticationService _identityService;
    private readonly IMfaUseCase _mfaUseCase;
    private readonly IAuthorizationStore _authorizationStore;
    private readonly TenantUserMfaPolicy _mfaPolicy;
    private readonly IAppLogger<DeviceAuthenticationUseCase> _logger;

    internal DeviceAuthenticationUseCase(IAuthenticationService identityService,
        IMfaUseCase mfaUseCase,
        TenantUserMfaPolicy mfaPolicy,
        IAppLogger<DeviceAuthenticationUseCase> logger,
        IAuthorizationStore authorizationStore)
    {
        _identityService = identityService;
        _mfaUseCase = mfaUseCase;
        _mfaPolicy = mfaPolicy;
        _logger = logger;
        _authorizationStore = authorizationStore;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string userCode, string userName, string password)
    {
        var userCodeHash = SecretHasher.HashSecret(userCode);

        var deviceRequest = await _authorizationStore
            .GetDeviceAuthorizationByUCode(userCodeHash);

        var result = ValidateDeviceAuthorization(deviceRequest);

        if (result != null)
        {
            return result;
        }

        var context = await _identityService
                    .Authenticate(deviceRequest!.TenantId, userName, password);

        if (!context.IsSuccess)
        {
            return AuthenticationResult.Failure(context.Error);
        }

        var checkTwoFactorEnabled = await _mfaPolicy.IsMfaRequiredAsync(context);

        if (checkTwoFactorEnabled)
        {
            GenerateMfaCommand mfaRequest = new()
            {
                UserId = context.UserId,
                ClientId = deviceRequest.ClientId,
                Scopes = deviceRequest.Scopes
            };

            var authResponse = await _mfaUseCase.GenerateMfaCode(mfaRequest);

            _logger.LogInfo("Authenticate completed for user: {UserId}", context.UserId);

            return AuthenticationResult.Success(context.UserId, checkTwoFactorEnabled);
        }

        return AuthenticationResult.Success(context.UserId, false);
    }

    public async Task<AuthenticationResult> ApproveAsync(string userCode, int userId)
    {
        var userCodeHash = SecretHasher.HashSecret(userCode);

        var deviceRequest = await _authorizationStore
            .GetDeviceAuthorizationByUCode(userCodeHash);

        var result = ValidateDeviceAuthorization(deviceRequest);

        if(result!= null)
        {
            return result;
        }

        deviceRequest!.Approve(userId.ToString());

        await _authorizationStore.UpdateDeviceAuthorization(deviceRequest!);

        return AuthenticationResult.Success(userId, false);
    }

    private AuthenticationResult? ValidateDeviceAuthorization(DeviceAuthorization? deviceRequest)
    {
        if (deviceRequest == null)
            return AuthenticationResult.Failure("Invalid or expired device code.");

        if (deviceRequest.IsExpired()
            || deviceRequest.Status == DeviceAuthorizationStatus.Consumed
            || deviceRequest.Status == DeviceAuthorizationStatus.Denied
            || deviceRequest.Status != DeviceAuthorizationStatus.Pending)
            return AuthenticationResult.Failure("Invalid or expired device code.");

        return default;
    }
}
