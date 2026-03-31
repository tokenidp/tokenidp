namespace IDP.Domain.AggregateRoots.Tokens;

public class TokenContext
{
    public int? UserId { get; private set; }
    public int TenantId { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public GrantTypes GrantType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public int ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public bool RememberMe { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime RefreshExpiresAt { get; private set; }
    public string[] Scopes { get; private set; } = Array.Empty<string>();
    public string[] Audiences { get; private set; } = Array.Empty<string>();
    public string[] Roles { get; private set; } = Array.Empty<string>();
    public IReadOnlySet<string> ActiveSecretHashes { get; private set; } = default!;

    public static TokenContext Create(
        int userId,
        int tenantId,
        string clientName,
        string userName,
        string clientId,
        GrantTypes grantType,
        TokenTypes tokenType,
        int clientSecretExpiry,
        int accessTokenLifetime,
        int refreshTokenExpiration,
        bool rememberMe,
        string[] roles,
        string[] scopes,
        string[] audiences,
        string ipAddress = "")
    {
        return new TokenContext()
        {
            UserId = userId,
            TenantId = tenantId,
            ClientName = clientName,
            UserName = userName,
            ClientId = clientId,
            GrantType = grantType,
            TokenType = tokenType,
            Roles = roles,
            ClientSecretExpiry = clientSecretExpiry,
            AccessTokenLifetime = accessTokenLifetime * 60,
            RefreshTokenExpiration = (refreshTokenExpiration * 24) * 60,
            Scopes = scopes,
            Audiences = audiences,
            IpAddress = ipAddress,
            RememberMe = rememberMe
        };
    }

    public static TokenContext Create(
        int tenantId,
        string clientName,
        string clientId,
        GrantTypes grantType,
        TokenTypes tokenType,
        int clientSecretExpiry,
        int accessTokenLifetime,
        int refreshTokenExpiration,
        string[] scopes,
        string[] audiences,
        IReadOnlySet<string> secrets,
        string ipAddress = "")
    {
        return new TokenContext()
        {
            TenantId = tenantId,
            ClientName = clientName,
            ClientId = clientId,
            GrantType = grantType,
            TokenType = tokenType,
            ClientSecretExpiry = clientSecretExpiry,
            AccessTokenLifetime = accessTokenLifetime * 60,
            RefreshTokenExpiration = (refreshTokenExpiration * 24) * 60,
            Scopes = scopes,
            Audiences = audiences,
            IpAddress = ipAddress,
            ActiveSecretHashes = secrets,
        };
    }

    public void SetTokenDates()
    {
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddSeconds(AccessTokenLifetime);
    }

    public void SetRefreshTokenExpiry()
    {
        if (RememberMe)
        {
            RefreshExpiresAt = DateTime.UtcNow.AddMinutes(RefreshTokenExpiration);
        }
        else
        {
            RefreshExpiresAt = DateTime.UtcNow.AddMinutes(240);
        }
    }

    public void AddAuthorizedScopes(string scope)
    {
        if (scope == null)
        {
            return;
        }

        Scopes = scope
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    public void SetAuthorizedScopes(IEnumerable<string> scopes)
    {
        Scopes = scopes?
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
    }

    public void SetAudiences(IEnumerable<string> audiences)
    {
        Audiences = audiences?
            .Where(audience => !string.IsNullOrWhiteSpace(audience))
            .Select(audience => audience.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
    }
}