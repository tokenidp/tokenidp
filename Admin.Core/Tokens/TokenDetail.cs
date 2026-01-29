namespace Admin.Core.Tokens;

internal sealed class TokenDetail
{
    public Guid Id { get; private set; }
    public int TenantId { get; private set; }
    public Guid SourceTokenId { get; private set; } = default!;
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

    public static Expression<Func<TokenSearch, TokenDetail>> Projection =>
    token => new TokenDetail
    {
        Id = token.Id,
        TenantId = token.TenantId,

        SourceTokenId = token.TokenId,
        SourceType = token.SourceType,

        TokenType = token.TokenType,

        ClientId = token.ClientId,
        ClientName = token.ClientName,

        UserId = token.UserId,
        UserName = token.UserName,

        IssuedAt = token.IssuedAt,
        ExpiresAt = token.ExpiresAt,

        Status = token.Status,

        Scopes = token.Scopes,
        Audience = token.Audience,

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
