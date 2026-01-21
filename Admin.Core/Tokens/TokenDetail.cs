using System.Linq.Expressions;
using IDP.Domain.ComplexTypes;
using IDP.Domain.Specifications;

namespace Admin.Core.Tokens;

internal sealed class TokenDetail
{
    public int Id { get; private set; }
    public string TokenId { get; private set; }
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

    public static Expression<Func<TokenSearch, TokenDetail>> Projection =>
        token => new TokenDetail
        {
            Id = token.Id,
            TokenId = token.TokenIdHash,
            TokenType = token.TokenType,
            ClientId = token.ClientId,
            ClientName = token.ClientName,
            UserId = token.UserId,
            UserName = token.UserName,
            Subject = token.Subject,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Status = token.Status,
            Scopes = token.Scopes,
            Audience = token.Audience,
            ClaimsJson = token.ClaimsJson,
            MetadataJson = token.MetadataJson,
            IssuedByIp = token.IssuedByIp,
            IssuedUserAgent = token.IssuedUserAgent,
            IssuedBy = token.IssuedBy,
            RevokedAt = token.RevokedAt,
            RevokedBy = token.RevokedBy,
            RevokedByIp = token.RevokedByIp,
            RevokedReason = token.RevokedReason,
            CreatedOn = token.CreatedOn,
            UpdatedOn = token.UpdatedOn
        };
}
