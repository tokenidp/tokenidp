namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface IAuthorizationStore
{
    Task<AuthorizationCode?> GetByAuthCode(string code, string clientId);
    Task<int> CreateAuthorization(AuthorizationCode authorizationCode);
    Task<int> UpdateAuthorization(AuthorizationCode authorizationCode);

    Task<PreAuthorization> GetPreAuthorization(string correlationId);
    Task<int> CreatePreAuthorization(PreAuthorization preAuthorization, CancellationToken ct);
    Task<int> UpdatePreAuthorization(PreAuthorization preAuthorization);

    Task<int> CreateDeviceAuthorization(DeviceAuthorization deviceAuthorization);
    Task<int> UpdateDeviceAuthorization(DeviceAuthorization deviceAuthorization);
    Task<DeviceAuthorization?> GetDeviceAuthorizationByUCode(string userCodeHash);
    Task<DeviceAuthorization?> GetDeviceAuthorizationByDCode(string deviceCodeHash);
}

