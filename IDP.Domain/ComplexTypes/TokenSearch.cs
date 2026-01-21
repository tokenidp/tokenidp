using IDP.Domain.Specifications;

namespace IDP.Domain.ComplexTypes;

public class TokenSearch
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int SourceTokenId { get; private set; }
    public string SourceType { get; private set; }
    public string TokenIdHash { get; private set; }
    public string TokenType { get; private set; }
    public string ClientId { get; private set; }
    public string ClientName { get; private set; }
    public int? UserId { get; private set; }
    public string UserName { get; private set; }
    public string Subject { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public TokenStatus Status { get; private set; }
    public string Scopes { get; private set; }
    public string Audience { get; private set; }
    public string ClaimsJson { get; private set; }
    public string MetadataJson { get; private set; }
    public string IssuedByIp { get; private set; }
    public string IssuedUserAgent { get; private set; }
    public string IssuedBy { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string RevokedBy { get; private set; }
    public string RevokedByIp { get; private set; }
    public string RevokedReason { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private TokenSearch()
    {
    }
}