using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Clients;

public class Client : BaseEntity, IAggregateRoot, ITenant
{
    public string ClientId { get; private set; }
    public string ClientName { get; private set; }
    public string? Description { get; private set; }
    public ClientTypes ClientType { get; private set; }
    public AppTypes AppType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; }
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

    public virtual Tenant Tenant { get; private set; }
    public virtual ICollection<ClientScope> ClientScopes { get; private set; }
    public virtual ICollection<ClientGrantType> ClientGrantTypes { get; private set; }
    public virtual ICollection<ClientSecret> ClientSecrets { get; private set; }
    public virtual ICollection<ClientAudience> ClientAudiences { get; private set; }

    private Client()
    {

    }

    public Client(int tenantId,
        string clientId,
        string clientName,
        string? description,
        ClientTypes clientType,
        AppTypes appType,
        TokenTypes tokenType,
        string redirectUri,
        string? logoutRedirectUri,
        bool isActive,
        int? clientSecretExpiry,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientName = clientName;
        Description = description;
        ClientType = clientType;
        AppType = appType;
        TokenType = tokenType;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        IsActive = isActive;
        ClientSecretExpiry = clientSecretExpiry;
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;

        ClientScopes = new List<ClientScope>();
        ClientGrantTypes = new List<ClientGrantType>();
        ClientSecrets = new List<ClientSecret>();
        ClientAudiences = new List<ClientAudience>();
    }

    public void UpdateClient(
        string clientId,
        string clientName,
        string? description,
        ClientTypes clientType,
        AppTypes appType,
        TokenTypes tokenType,
        string redirectUri,
        string? logoutRedirectUri,
        bool isActive,
        int? clientSecretExpiry,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking)
    {
        ClientId = clientId;
        ClientName = clientName;
        Description = description;
        ClientType = clientType;
        AppType = appType;
        TokenType = tokenType;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        IsActive = isActive;
        ClientSecretExpiry = clientSecretExpiry;
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;
    }
}