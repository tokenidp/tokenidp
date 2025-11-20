namespace IDP.Core.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class UserAccessToken
{
    [Key]
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string TokenId { get; private set; }
    public string ClientId { get; private set; }
    public string Scopes { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public string Roles { get; private set; }
    public bool? IsRevoked { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private UserAccessToken() { }

    public UserAccessToken(int userId,
        int tenantId,
        string clientId,
        string tokenId,
        string scopes,
        DateTime expiresAt,
        DateTime issuedAt,
        string roles,
        int createdBy)
    {
        UserId = userId;
        TenantId = tenantId;
        ClientId = clientId;
        TokenId = tokenId;
        Scopes = scopes;
        ExpiresAt = expiresAt;
        IssuedAt = issuedAt;
        Roles = roles;
        CreatedBy = createdBy;
        CreatedOn = DateTime.UtcNow;
    }

    public void RevokeAccessToken(int userId)
    {
        IsRevoked = true;
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
