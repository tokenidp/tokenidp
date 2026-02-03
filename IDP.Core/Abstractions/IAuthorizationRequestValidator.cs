namespace IDP.Core.Abstractions;

public interface IAuthorizationRequestValidator
{
    Task ValidateAsync(AuthorizationRequest request, CancellationToken ct);
}
