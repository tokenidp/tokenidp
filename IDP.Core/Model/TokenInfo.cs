namespace IDP.Core.Model;

internal class TokenInfo
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public TokenType AccessTokenType { get; private set; }
    public int ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public string[] Scopes { get; private set; }
    public string[] Audiences { get; private set; }
    public string[] Roles { get; private set; } = new string[0];

    public static TokenInfo Create(int userId,
        int tenantId,
        string userName,
        string clientId,
        TokenType accessTokenType,
        string[] scope,
        string[] audience,
        int clientSecretExpiry,
        int accessTokenLifetime,
        int refreshTokenExpiration,
        string[] roles)
    {
        return new TokenInfo()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = userName,
            ClientId = clientId,
            AccessTokenType = accessTokenType,
            Roles = roles,
            ClientSecretExpiry = clientSecretExpiry,
            AccessTokenLifetime = accessTokenLifetime,
            RefreshTokenExpiration = refreshTokenExpiration,
            Scopes = scope,
            Audiences = audience
        };
    }

    public void AddAuthorizedScopes(string scope)
    {
        if(scope == null)
        {
            return;
        }

        Scopes = scope.Split(' ');
    }
}
