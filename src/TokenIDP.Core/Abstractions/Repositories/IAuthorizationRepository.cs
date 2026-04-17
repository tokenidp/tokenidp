namespace TokenIDP.Core.Abstractions.Repositories;

public interface IAuthorizationRepository
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

    Task<int> CreateBackchannelAuthenticationRequest(BackchannelAuthenticationRequest request, CancellationToken ct);
    Task<int> UpdateBackchannelAuthenticationRequest(BackchannelAuthenticationRequest request, CancellationToken ct);
    Task<BackchannelAuthenticationRequest?> GetBackchannelAuthenticationRequestByHashAsync(string authReqIdHash, CancellationToken ct);
    Task<BackchannelAuthenticationRequest?> GetBackchannelAuthenticationRequestByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<BackchannelAuthenticationRequest>> GetPendingBackchannelRequestsForUserAsync(int tenantId, int userId, CancellationToken ct);
}

