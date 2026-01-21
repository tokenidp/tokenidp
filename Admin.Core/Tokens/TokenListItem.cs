namespace Admin.Core.Tokens;

internal sealed class TokenListItem
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

    public static Expression<Func<TokenSearch, TokenListItem>> Projection =>
        token => new TokenListItem
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
            Status = token.Status
        };
}