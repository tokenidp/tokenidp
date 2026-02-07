using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Clients;

public class ClientValidationSnapshot
{
    public string ClientId { get; }
    public string ClientName { get; }
    public int TenantId { get; private set; }
    public bool IsActive { get; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string? LogoutRedirectUri { get; private set; }
    public TokenTypes TokenType { get; }
    public IReadOnlySet<GrantTypes> GrantTypes { get; }
    public IReadOnlySet<string> Scopes { get; }
    public IReadOnlySet<string> Audiences { get; }
    public IReadOnlySet<string> ActiveSecretHashes { get; }
    public int? ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; }
    public int AuthorizationCodeLifetime { get; }
    public int RefreshTokenExpiration { get; }

    public ClientValidationSnapshot(
        string clientId,
        string clientName,
        int tenantId,
        bool isActive,
        string redirectUri,
        string? logoutRedirectUri,
        TokenTypes tokenType,
        IEnumerable<GrantTypes> grantTypes,
        IEnumerable<string> scopes,
        IEnumerable<string> audiences,
        IEnumerable<string> activeSecretHashes,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        int? clientSecretExpiry)
    {
        ClientId = clientId;
        ClientName = clientName;
        TenantId = tenantId;
        IsActive = isActive;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        TokenType = tokenType;
        GrantTypes = grantTypes.ToHashSet();
        Scopes = scopes.ToHashSet();
        Audiences = audiences.ToHashSet();
        ActiveSecretHashes = activeSecretHashes.ToHashSet();
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        ClientSecretExpiry = clientSecretExpiry;
    }
}

