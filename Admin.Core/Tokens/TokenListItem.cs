namespace Admin.Core.Tokens;

internal sealed class TokenListItem
{
    public int Id { get; private set; }
    public string TokenId { get; private set; } = string.Empty;
    public string TokenType { get; private set; } = string.Empty;
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
            TokenType = token.TokenType,
            ClientName = token.ClientName,
            UserName = token.UserName ?? string.Empty,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt,
            Status = token.Status
        };
}