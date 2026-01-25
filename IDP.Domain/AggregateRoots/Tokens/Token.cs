using IDP.Domain.AggregateRoots.Tokens;
using IDP.Domain.DomainEvents;
using IDP.Domain.Specifications;

public sealed class Token : AuditableAggregate<Guid>
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public string ClientId { get; private set; } = default!;

    public TokenStatus TokenStatus { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public GrantTypes GrantType { get; private set; }

    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public string? Scope { get; private set; }
    public string? Audience {get; private set; }
    public string? CreatedByIpAddress { get; private set; }
    public string? Roles { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? DeviceId { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIpAddress { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public RefreshToken RefreshToken { get; private set; } = default!;
    public ReferenceToken ReferenceToken { get; private set; } = default!;

    private Token() { }

    public static Token CreateToken(TokenContext ctx)
    {
        var token = new Token
        {
            Id = Guid.NewGuid(),
            UserId = ctx.UserId,
            TenantId = ctx.TenantId,
            ClientId = ctx.ClientId,
            TokenType = ctx.TokenType,
            GrantType = ctx.GrantType,
            TokenStatus = TokenStatus.Active,
            IssuedAt = ctx.IssuedAt,
            ExpiresAt = ctx.ExpiresAt,
            Scope = string.Join(" ", ctx.Scopes),
            CreatedByIpAddress = ctx.IpAddress,
            Roles = string.Join(" ", ctx.Roles)
        };

        token.SetCreated(ctx.UserId);

        return token;
    }

    public void IssueJwt(TokenContext ctx)
    {
        AddDomainEvent(
            new TokenIssuedDomainEvent(Id, 
            TenantId, 
            UserId, 
            ClientId, 
            TokenTypes.JWT, 
            ctx.ExpiresAt));
    }

    public void AddReferenceToken(DateTime expires, byte[] tokenHash)
    {
        ReferenceToken = ReferenceToken.Create(Id, tokenHash);

        AddDomainEvent(
            new ReferenceTokenIssuedDomainEvent(Id, 
            Id, 
            TenantId, 
            UserId, 
            ClientId, 
            expires));
    }

    public void AddRefreshToken(DateTime expiresAt, byte[] tokenHash, string ip)
    {
        RefreshToken = RefreshToken.Create(Id, tokenHash, expiresAt);

        AddDomainEvent(
            new RefreshTokenIssuedDomainEvent(Id, 
            Id, 
            TenantId, 
            UserId, 
            ClientId, 
            expiresAt, 
            CreatedByIpAddress));

    }

    public void Revoke(string reason, int userId)
    {
        if (TokenStatus != TokenStatus.Active) return;

        TokenStatus = TokenStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        ReasonRevoked = reason;

        AddDomainEvent(
            new TokenRevokedDomainEvent(Id, 
            TenantId,
            userId, 
            ClientId,
            ReasonRevoked));
    }

    public void Expire()
    {
        if (TokenStatus != TokenStatus.Active) return;
        TokenStatus = TokenStatus.Expired;
    }

    public bool IsActive() =>
        TokenStatus == TokenStatus.Active && ExpiresAt > DateTime.UtcNow;

}