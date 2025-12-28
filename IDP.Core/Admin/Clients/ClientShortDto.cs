using System.Linq.Expressions;

namespace IDP.Core.Admin.Clients;

internal class ClientShortDto
{
    internal static Expression<Func<Client, ClientShortDto>> Projection =>
    client => new ClientShortDto()
    {
        TenantId = client.TenantId,
        ClientId = client.ClientId,
        AccessTokenType = client.AccessTokenType,
        RedirectUri = client.RedirectUri,
        LogoutRedirectUri = client.LogoutRedirectUri,
        IsActive = client.IsActive,
        TwoFactorEnabled = client.TwoFactorEnabled,
        TwoFactorCodeExpiry = client.TwoFactorCodeExpiry,
        AccessTokenLifetime = client.AccessTokenLifetime,
        ClientSecretExpiry = client.ClientSecretExpiry,
        AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
        RefreshTokenExpiration = client.RefreshTokenExpiration,
        PermitLimit = client.PermitLimit,
        TimeWindow = client.TimeWindow,
        QueueLimit = client.QueueLimit,
        EnableITracking = client.EnableITracking,
        Scopes = client.ClientScopes.Select(s => s.Scope).ToArray(),
        GrantTypes = client.ClientGrantTypes.Select(s => s.AllowedGrantType.ToString()).ToArray(),
        Secrets = client.ClientSecrets.Where(s => s.ExpiresAt > DateTime.UtcNow || s.IsRevoked != true)
        .Select(s => s.SecretHash).ToArray(),
        Audiences = client.ClientAudiences.Where(s => s.IsActive != false)
        .Select(s => s.Name).ToArray()
    };

    public int Id { get; set; }
    public string ClientId { get; private set; } = string.Empty;
    public TokenType AccessTokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string? LogoutRedirectUri { get; private set; }
    public bool IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public int TwoFactorCodeExpiry { get; private set; }
    public int ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }
    public string[] Scopes { get; private set; }
    public string[] GrantTypes { get; private set; }
    public string[] Secrets { get; private set; }
    public string[] Audiences { get; private set; }
}
