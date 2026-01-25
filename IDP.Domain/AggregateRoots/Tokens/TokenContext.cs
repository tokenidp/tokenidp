using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Tokens;

public class TokenContext
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public GrantTypes GrantType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public int ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime RefreshExpiresAt { get; private set; }
    public string[] Scopes { get; private set; } = default!;
    public string[] Audiences { get; private set; } = default!;
    public string[] Roles { get; private set; } = Array.Empty<string>();

    public static TokenContext Create(int userId,
        int tenantId,
        string userName,
        string clientId,
        TokenTypes tokenType,
        int clientSecretExpiry,
        int accessTokenLifetime,
        int refreshTokenExpiration,
        string[] roles,
        string[] scope,
        string[] audience,
        string ipAddress = "")
    {
        return new TokenContext()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = userName,
            ClientId = clientId,
            TokenType = tokenType,
            Roles = roles,
            ClientSecretExpiry = clientSecretExpiry,
            AccessTokenLifetime = accessTokenLifetime,
            RefreshTokenExpiration = refreshTokenExpiration,
            Scopes = scope,
            Audiences = audience,
            IpAddress = ipAddress
        };
    }

    public void SetTokenDates()
    {
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenLifetime);
    }

    public void SetRefreshTokenExpiry()
    {
        RefreshExpiresAt = DateTime.UtcNow.AddHours(RefreshTokenExpiration);
    }

    public void AddAuthorizedScopes(string scope)
    {
        if (scope == null)
        {
            return;
        }

        Scopes = scope.Split(' ');
    }
}