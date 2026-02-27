using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

public sealed class AuthorizationPageUiUseCase : IAuthorizationPageUiUseCase
{
    private readonly ITenantStore _tenantStore;
    private readonly IClientStore _clientStore;

    public AuthorizationPageUiUseCase(ITenantStore tenantStore,
        IClientStore clientStore)
    {
        _tenantStore = tenantStore;
        _clientStore = clientStore;
    }

    public async Task<AuthorizationPageUi> BuildAsync(int tenantId, int clientId, CancellationToken ct)
    {
        var tenantUISetting = await _tenantStore.GetTenantUISettings(tenantId);
        var clientPolicy = await _clientStore.GetClientAuthPolicy(clientId);
        var providers = await _clientStore.GetExternalProviders(clientId);

        AuthorizationPageUi authorizationPageUi = new();

        if (tenantUISetting != null)
        {
            authorizationPageUi.LogoUrl = tenantUISetting?.LogoUrl;
            authorizationPageUi.Theme = tenantUISetting?.Theme;
            authorizationPageUi.AccentColor = tenantUISetting?.PrimaryColor;
        }

        if (clientPolicy != null)
        {
            authorizationPageUi.AllowLocalLogin = clientPolicy.AllowLocalLoginOverride;
            authorizationPageUi.AllowSignup = clientPolicy.ShowCreateAccountLink;
        }

        if (providers != null)
        {
            authorizationPageUi.ExternalProviders = providers.Select(p => new ExternalProviderUi
            {
                DisplayName = p.ProviderType,
                Enabled = p.EnabledForClient
            }).ToList();
        }

        return authorizationPageUi;
    }
}
