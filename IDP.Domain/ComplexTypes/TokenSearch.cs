using IDP.Domain.Specifications;

namespace IDP.Domain.ComplexTypes;

public class TokenSearch
{
    public Guid Id { get; private set; }
    public Guid TokenId { get; private set; } = default!;
    public int TenantId { get; private set; }
    public string SourceType { get; private set; } = default!;
    public string TokenType { get; private set; } = default!;
    public string ClientId { get; private set; } = default!;
    public string ClientName { get; private set; } = default!;
    public int? UserId { get; private set; }
    public string? UserName { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public TokenStatus Status { get; private set; }
    public string? Scopes { get; private set; }
    public string Audience { get; private set; } = default!;
    public string? IssuedByIp { get; private set; } = default!;
    public string? IssuedUserAgent { get; private set; }
    public string IssuedBy { get; private set; } = default!;
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private TokenSearch() { }
}