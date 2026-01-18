namespace IDP.Domain.AggregateRoots;

public class ReferenceToken
{
    [Key]
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string Token { get; private set; }
    public string AccessToken { get; private set; }
    public string ClientId { get; private set; }
    public string Scopes { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public string Roles { get; private set; }
    public bool IsRevoked { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private ReferenceToken() { }

    public ReferenceToken(int userId,
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
        Token = tokenId;
        Scopes = scopes;
        ExpiresAt = expiresAt;
        IssuedAt = issuedAt;
        Roles = roles;
        CreatedBy = createdBy;
        CreatedOn = DateTime.UtcNow;
    }

    public void RevokeToken(int userId)
    {
        IsRevoked = true;
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
