using System.Linq.Expressions;

namespace IDP.Core.Admin.Clients;

internal class ClientDto
{
    internal static Expression<Func<Client, ClientDto>> Projection =>
    client => new ClientDto()
    {
        Id = client.Id,
        TenantId = client.TenantId,
        ClientId = client.ClientId,
        ClientName = client.ClientName,
        Description = client.Description,
        AccessTokenType = client.AccessTokenType,
        RedirectUri = client.RedirectUri,
        LogoutRedirectUri = client.LogoutRedirectUri,
        IsActive = client.IsActive,
        TwoFactorEnabled = client.TwoFactorEnabled,
        TwoFactorCodeExpiry = client.TwoFactorCodeExpiry,
        AccessTokenLifetime = client.AccessTokenLifetime,
        AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
        RefreshTokenExpiration = client.RefreshTokenExpiration,
        PermitLimit = client.PermitLimit,
        TimeWindow = client.TimeWindow,
        QueueLimit = client.QueueLimit,
        EnableITracking = client.EnableITracking
    };


    public int Id { get; set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TokenType AccessTokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string? LogoutRedirectUri { get; private set; }
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



