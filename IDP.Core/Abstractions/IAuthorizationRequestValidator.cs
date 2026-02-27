namespace IDP.Core.Abstractions;

public interface IAuthorizationRequestValidator
{
    Task<ClientShortInfo> ValidateAsync(AuthorizationRequest request, CancellationToken ct);
}
