using Admin.Core.Clients;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultClients
{
    public static CreateUpdateClient GetDefaultClient(
        string redirectUri,
        string logoutRedirectUri) => new()
        {
            ClientId = "idp-admin",
            ClientName = "IDP Admin Portal",
            Description = "Administrative client for managing the Identity Platform",
            AppType = ClientTypes.SPA,
            AccessTokenType = TokenTypes.JWT,
            RedirectUri = redirectUri,
            LogoutRedirectUri = logoutRedirectUri,
            IsActive = true,

            TwoFactorEnabled = true,
            TwoFactorCodeExpiry = 5,              // 5 minutes
            AccessTokenLifetime = 30,              // 30 minutes
            AuthorizationCodeLifetime = 5,        // 5 minutes
            RefreshTokenExpiration = 24,       // 24 hours

            PermitLimit = null,
            TimeWindow = null,
            QueueLimit = null,
            EnableITracking = true,

            GrantTypes = new List<GrantTypes>
            {
                GrantTypes.authorization_code,
                GrantTypes.refresh_token
            },

            Scopes = new List<string>
            {
                "openid",
                "profile",
                "email",
                "offline_access"
            },

            Audiences = new List<string>
            {
                "tresor.admin.api"
            },

            // Public client (SPA) → no secret
            ClientSecret = null,
            ClientSecretDescription = null
        };
}

