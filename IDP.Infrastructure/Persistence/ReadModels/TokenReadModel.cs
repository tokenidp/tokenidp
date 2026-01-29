namespace IDP.Infrastructure.Persistence.ReadModels;

internal sealed class TokenReadModel
{
    public Guid Id { get; private set; }

    public int TenantId { get; private set; }
    public Guid SourceTokenId { get; private set; }
    public string SourceType { get; private set; } = default!;
    public byte[]? TokenIdHash { get; private set; }

    public string TokenType { get; private set; } = default!;
    public string ClientId { get; private set; } = default!;

    public int? UserId { get; private set; }
    public string? Subject { get; private set; }

    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public string Status { get; private set; } = default!;
    public string? Scopes { get; private set; }
    public string Audience { get; private set; } = default!;

    public string? IssuedByIp { get; private set; }
    public string? IssuedUserAgent { get; private set; }
    public string? IssuedBy { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }

    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private TokenReadModel() { }

    public TokenReadModel(
        int tenantId,
        Guid sourceTokenId,
        string sourceType,
        byte[]? tokenIdHash,
        string tokenType,
        string clientId,
        int? userId,
        string? subject,
        DateTime issuedAt,
        DateTime expiresAt,
        string status,
        string? scopes,
        string audience,
        string? issuedByIp,
        string? issuedUserAgent,
        string? issuedBy)
    {
        TenantId = tenantId;
        SourceTokenId = sourceTokenId;
        SourceType = sourceType;
        TokenIdHash = tokenIdHash;
        TokenType = tokenType;
        ClientId = clientId;
        UserId = userId;
        Subject = subject;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Status = status;
        Scopes = scopes;
        Audience = audience;
        IssuedByIp = issuedByIp;
        IssuedUserAgent = issuedUserAgent;
        IssuedBy = issuedBy;
        CreatedOn = DateTime.UtcNow;
    }

    public void Revoke(string? revokedBy, string? reason, string? revokedByIp)
    {
        Status = "Revoked";
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
        RevokedByIp = revokedByIp;
        UpdatedOn = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = "Expired";
        UpdatedOn = DateTime.UtcNow;
    }

    public void UpdateExpiry(DateTime newExpiry)
    {
        ExpiresAt = newExpiry;
        UpdatedOn = DateTime.UtcNow;
    }
}