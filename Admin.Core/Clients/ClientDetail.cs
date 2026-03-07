namespace Admin.Core.Clients;

internal class ClientDetail
{
    internal static Expression<Func<Client, ClientDetail>> Projection =>
        client => new ClientDetail()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            Description = client.Description,
            AppType = client.ClientType,
            TokenType = client.TokenType,
            RedirectUri = client.RedirectUri,
            LogoutRedirectUri = client.LogoutRedirectUri,
            IsActive = client.IsActive,
            ClientSecretExpiry = client.ClientSecretExpiry,
            AccessTokenLifetime = client.AccessTokenLifetime,
            AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
            RefreshTokenExpiration = client.RefreshTokenExpiration,
            PermitLimit = client.PermitLimit,
            TimeWindow = client.TimeWindow,
            QueueLimit = client.QueueLimit,
            EnableITracking = client.EnableITracking,
            Scopes = client.ClientScopes.Select(scope => scope.Scope).ToList(),
            GrantTypes = client.ClientGrantTypes.Select(grant => grant.AllowedGrantType).ToList(),
            Audiences = client.ClientAudiences.Select(audience => audience.Name).ToList(),
            AuthPolicy = client.ClientAuthPolicy == null
                ? new ClientAuthPolicyDetail()
                : new ClientAuthPolicyDetail
                {
                    AllowLocalLoginOverride = client.ClientAuthPolicy.AllowLocalLoginOverride,
                    AllowSelfRegistrationOverride = client.ClientAuthPolicy.AllowSelfRegistrationOverride,
                    MfaPolicyOverride = client.ClientAuthPolicy.MfaPolicyOverride,
                    ShowExternalProviders = client.ClientAuthPolicy.ShowExternalProviders,
                    ShowStaySignedIn = client.ClientAuthPolicy.ShowStaySignedIn,
                    ShowCreateAccountLink = client.ClientAuthPolicy.ShowCreateAccountLink
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
    public ClientTypes AppType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string? LogoutRedirectUri { get; private set; }
    public bool IsActive { get; private set; }
    public int? ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }
    public List<string> Scopes { get; private set; } = new();
    public List<GrantTypes> GrantTypes { get; private set; } = new();
    public List<string> Audiences { get; private set; } = new();
    public ClientAuthPolicyDetail AuthPolicy { get; private set; } = new();
    public List<int> ExternalProviders { get; private set; } = new();
    public bool AutoCreateUsers { get; set; } = true;
    public int? DefaultRoleId { get; set; }
}