namespace TokenIDP.Core.Admin.Clients;

public class ClientDetail
{
    public static Expression<Func<Client, ClientDetail>> Projection =>
        client => new ClientDetail()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            Description = client.Description,
            IconUrl = client.IconUrl,
            AppType = client.ClientType,
            TokenType = client.TokenType,
            RedirectUri = client.RedirectUri,
            LogoutRedirectUri = client.LogoutRedirectUri,
            IsActive = client.IsActive,
            ClientSecretExpiry = client.ClientSecretExpiry,
            AccessTokenLifetime = client.AccessTokenLifetime,
            AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
            RefreshTokenExpiration = client.RefreshTokenExpiration,
            RefreshTokenDeliveryMode = client.RefreshTokenDeliveryMode,
            PermitLimit = client.PermitLimit,
            TimeWindow = client.TimeWindow,
            QueueLimit = client.QueueLimit,
            EnableITracking = client.EnableITracking,
            CibaEnabled = client.CibaEnabled,
            BackchannelTokenDeliveryMode = client.BackchannelTokenDeliveryMode,
            CibaDefaultExpirySeconds = client.CibaDefaultExpirySeconds,
            CibaMinIntervalSeconds = client.CibaMinIntervalSeconds,
            RequireCibaUserCode = client.RequireCibaUserCode,
            AllowCibaLoginHint = client.AllowCibaLoginHint,
            AllowCibaLoginHintToken = client.AllowCibaLoginHintToken,
            AllowCibaIdTokenHint = client.AllowCibaIdTokenHint,
            Scopes = client.ClientScopes.Select(scope => scope.Scope).ToList(),
            ApiResources = client.ClientApiResources
                .Where(apiResource => apiResource.IsActive)
                .Select(apiResource => apiResource.Name)
                .ToList(),
            GrantTypes = client.ClientGrantTypes.Select(grant => grant.AllowedGrantType).ToList(),
            AuthPolicy = client.ClientAuthPolicy == null
                ? new ClientAuthPolicyDetail()
                : new ClientAuthPolicyDetail
                {
                    AllowLocalLoginOverride = client.ClientAuthPolicy.AllowLocalLoginOverride,
                    AllowSelfRegistrationOverride = client.ClientAuthPolicy.AllowSelfRegistrationOverride,
                    MfaPolicyOverride = client.ClientAuthPolicy.MfaPolicyOverride,
                    ShowExternalProviders = client.ClientAuthPolicy.ShowExternalProviders,
                    ShowStaySignedIn = client.ClientAuthPolicy.ShowStaySignedIn,
                    ShowCreateAccountLink = client.ClientAuthPolicy.ShowCreateAccountLink,
                    AutoCreateUsers = client.ClientAuthPolicy.AutoCreateUsers,
                    DefaultRoleId = client.ClientAuthPolicy.DefaultRoleId
                },
            ExternalProviders = client.ClientExternalProviders
                .Where(provider => provider.EnabledForClient)
                .Select(provider => provider.ExternalProviderId)
                .ToList()
        };

    public int Id { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public ClientTypes AppType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string? LogoutRedirectUri { get; private set; }
    public bool IsActive { get; private set; }
    public int? ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public RefreshTokenDeliveryMode RefreshTokenDeliveryMode { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }
    public bool CibaEnabled { get; private set; }
    public CibaTokenDeliveryModes BackchannelTokenDeliveryMode { get; private set; }
    public int CibaDefaultExpirySeconds { get; private set; }
    public int CibaMinIntervalSeconds { get; private set; }
    public bool RequireCibaUserCode { get; private set; }
    public bool AllowCibaLoginHint { get; private set; }
    public bool AllowCibaLoginHintToken { get; private set; }
    public bool AllowCibaIdTokenHint { get; private set; }
    public List<string> Scopes { get; private set; } = new();
    public List<string> ApiResources { get; private set; } = new();
    public List<GrantTypes> GrantTypes { get; private set; } = new();
    public ClientAuthPolicyDetail AuthPolicy { get; private set; } = new();
    public List<int> ExternalProviders { get; private set; } = new();
    public List<string> Audiences => ApiResources;
}
