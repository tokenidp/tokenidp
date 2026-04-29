using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

public sealed class AuthorizationPageUiUseCase : IAuthorizationPageUiUseCase
{
    private readonly ITenantRepository _tenantStore;
    private readonly IClientRepository _clientStore;

    public AuthorizationPageUiUseCase(ITenantRepository tenantStore,
        IClientRepository clientStore)
    {
        _tenantStore = tenantStore;
        _clientStore = clientStore;
    }

    public async Task<AuthorizationPageUi> BuildAsync(IReadOnlySet<string> scopes,
        int tenantId,
        int clientId,
        CancellationToken ct)
    {
        var tenant = await _tenantStore.GetSummaryAsync(tenantId, ct);
        var tenantUISetting = await _tenantStore.GetTenantUISettings(tenantId);
        var clientPolicy = await _clientStore.GetClientAuthPolicy(clientId);
        var providers = await _clientStore.GetExternalProviders(clientId);

        AuthorizationPageUi authorizationPageUi = new();
        if (tenant is not null)
        {
            authorizationPageUi.ProductName = string.IsNullOrWhiteSpace(tenant.TenantDisplayName)
                ? tenant.TenantName
                : tenant.TenantDisplayName;
        }

        if (tenantUISetting != null)
        {
            authorizationPageUi.LogoUrl = tenantUISetting?.LogoUrl;
            authorizationPageUi.Theme = tenantUISetting?.Theme;
            authorizationPageUi.AccentColor = tenantUISetting?.PrimaryColor;
            authorizationPageUi.LoginText = tenantUISetting?.LoginText;
        }

        if (clientPolicy != null)
        {
            authorizationPageUi.AllowLocalLogin = clientPolicy.AllowLocalLoginOverride;
            authorizationPageUi.AllowSignup = clientPolicy.ShowCreateAccountLink;
            authorizationPageUi.AllowStaySignedIn = clientPolicy.ShowStaySignedIn;
        }

        if (providers != null)
        {
            authorizationPageUi.ExternalProviders = providers.Select(p => new ExternalProviderUi
            {
                DisplayName = p.ProviderType,
                Enabled = p.EnabledForClient
            }).ToList();
        }

        if (!scopes.Contains(StandardScopes.OfflineAccess))
        {
            authorizationPageUi.AllowStaySignedIn = false;
        }

        return authorizationPageUi;
    }
}


