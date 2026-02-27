namespace IDP.Core.Abstractions;

public interface IAuthorizationPageUiUseCase
{
    Task<AuthorizationPageUi> BuildAsync(int tenantId, int clientId, CancellationToken ct);
}
