using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Clients;

internal class CreateUpdateClient
{
    public int Id { get; set; }
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string ClientName { get; set; }
    [Required]
    public required string RedirectUri { get; set; }
    public string? Description { get; set; }
    public ClientTypes ClientType { get; set; }
    public AppTypes AppType { get; set; }
    public TokenTypes AccessTokenType { get; set; }
    public string? LogoutRedirectUri { get; set; }
    public bool IsActive { get; set; }
    public int? ClientSecretExpiry { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int TwoFactorCodeExpiry { get; set; }
    public int AccessTokenLifetime { get; set; }
    public int AuthorizationCodeLifetime { get; set; }
    public int RefreshTokenExpiration { get; set; }
    public int? PermitLimit { get; set; }
    public TimeSpan? TimeWindow { get; set; }
    public int? QueueLimit { get; set; }
    public bool? EnableITracking { get; set; }
    public List<string> Scopes { get; set; } = new();
    public List<GrantTypes> GrantTypes { get; set; } = new();
    public List<string> Audiences { get; set; } = new();
    public string? ClientSecret { get; set; }
    public string? ClientSecretDescription { get; set; }
}