namespace TokenIDP.Core.OAuth.Abstractions;

public interface IAuthorizationRequestValidator
{
    Task<ClientShortInfo> ValidateAsync(AuthorizationRequest request, CancellationToken ct);
    Task<ClientShortInfo> ValidateAsync(DeviceAuthorizationRequest request, CancellationToken ct);
}

