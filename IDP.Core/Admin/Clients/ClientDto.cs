namespace IDP.Core.Admin.Clients;

internal class ClientDto
{
    public int Id { get; set; }
    public string ClientId { get; private set; }
    public string ClientName { get; private set; }
    public string? Description { get; private set; }
    public TokenType AccessTokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; }
    public string LogoutRedirectUri { get; private set; }
    public bool IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public int TwoFactorCodeExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }
}



