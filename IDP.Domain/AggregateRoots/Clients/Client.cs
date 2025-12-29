namespace IDP.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class Client
{
    public enum TokenType
    {
        JWT,
        ReferenceToken
    }

    [Key]
    public int Id { get; private set; }
    public string ClientId { get; private set; }
    public string ClientName { get; private set; }
    public string? Description { get; private set; }
    public string ClientType { get; private set; }
    public TokenType AccessTokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; }
    public string? LogoutRedirectUri { get; private set; }
    public bool IsActive { get; private set; }
    public int? ClientSecretExpiry { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public int TwoFactorCodeExpiry { get; private set; }
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
}
