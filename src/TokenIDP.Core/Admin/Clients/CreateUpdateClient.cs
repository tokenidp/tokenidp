namespace TokenIDP.Core.Admin.Clients;

public class CreateUpdateClient
{
    public int Id { get; set; }
    public string ClientName { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public ClientTypes AppType { get; set; }
    public TokenTypes AccessTokenType { get; set; }
    public string? LogoutRedirectUri { get; set; }
    public bool IsActive { get; set; }
    public int? ClientSecretExpiry { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int TwoFactorCodeExpiry { get; set; }
    public int AccessTokenLifetime { get; set; }
    public int AuthorizationCodeLifetime { get; set; }
    public int RefreshTokenExpiration { get; set; }
    public RefreshTokenDeliveryMode RefreshTokenDeliveryMode { get; set; } =
        RefreshTokenDeliveryMode.Response;
    public int? PermitLimit { get; set; }
    public TimeSpan? TimeWindow { get; set; }
    public int? QueueLimit { get; set; }
    public bool? EnableITracking { get; set; }
    public bool CibaEnabled { get; set; }
    public CibaTokenDeliveryModes BackchannelTokenDeliveryMode { get; set; } = CibaTokenDeliveryModes.Poll;
    public int CibaDefaultExpirySeconds { get; set; } = 300;
    public int CibaMinIntervalSeconds { get; set; } = 5;
    public bool RequireCibaUserCode { get; set; }
    public bool AllowCibaLoginHint { get; set; } = true;
    public bool AllowCibaLoginHintToken { get; set; }
    public bool AllowCibaIdTokenHint { get; set; }
    public List<string> Scopes { get; set; } = new();
    public List<string> ApiResources { get; set; } = new();
    public List<GrantTypes> GrantTypes { get; set; } = new();
    public string? ClientSecret { get; set; }
    public string? ClientSecretDescription { get; set; }
    public ClientAuthPolicyDetail AuthPolicy { get; set; } = new();
    public List<int> ExternalProviders { get; set; } = new();
}

public sealed class ClientAuthPolicyDetail
{
    public bool AllowLocalLoginOverride { get; set; }
    public bool AllowSelfRegistrationOverride { get; set; }
    public bool MfaPolicyOverride { get; set; }
    public bool ShowExternalProviders { get; set; }
    public bool ShowStaySignedIn { get; set; }
    public bool ShowCreateAccountLink { get; set; }
    public bool AutoCreateUsers { get; set; } = true;
    public int? DefaultRoleId { get; set; }
}
