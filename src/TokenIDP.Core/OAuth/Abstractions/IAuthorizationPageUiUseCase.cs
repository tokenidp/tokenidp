namespace TokenIDP.Core.OAuth.Abstractions;

public interface IAuthorizationPageUiUseCase
{
    Task<AuthorizationPageUi> BuildAsync(IReadOnlySet<string> scopes,
        int tenantId,
        int clientId,
        CancellationToken ct);
}

