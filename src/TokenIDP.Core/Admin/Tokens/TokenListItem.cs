using TokenIDP.Domain.AggregateRoots.Tokens;

namespace TokenIDP.Core.Admin.Tokens;

public sealed class TokenListItem
{
    public Guid Id { get; private set; }
    public Guid TokenId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public TokenStatus Status { get; private set; }

    public static Expression<Func<TokenSearch, TokenListItem>> Projection =>
        token => new TokenListItem
        {
            Id = token.Id,
            TokenId = token.TokenId,
            SourceType = token.SourceType,
            ClientId = token.ClientId,
            ClientName = token.ClientName,
            UserName = token.UserName ?? string.Empty,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Status = token.Status
        };
}
