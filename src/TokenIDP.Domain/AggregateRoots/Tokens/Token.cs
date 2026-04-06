using TokenIDP.Domain.DomainEvents.Tokens;

namespace TokenIDP.Domain.AggregateRoots.Tokens;

public enum TokenStatus
{
    Active,
    Expired,
    Revoked
}

public sealed class Token : AggregateRoot<Guid>
{
    public int TenantId { get; private set; }
    public int? UserId { get; private set; }
    public string ClientId { get; private set; } = default!;

    public TokenStatus TokenStatus { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public GrantTypes GrantType { get; private set; }

    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public string? Scope { get; private set; }
    public string? Audience { get; private set; }
    public string? CreatedByIpAddress { get; private set; }
    public string? Roles { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? DeviceId { get; private set; }
    public string? UserAgent { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIpAddress { get; private set; }
    public string? RevokeReason { get; private set; }

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
            Audience = ctx.Audiences.FirstOrDefault(),
            CreatedByIpAddress = ctx.IpAddress,
            Roles = string.Join(" ", ctx.Roles)
        };

        token.SetCreated(ctx.UserId ?? 1);

        return token;
    }

    public void IssueJwt(string clientName)
    {
        AddDomainEvent(
            new JwtTokenIssuedEvent(Id,
            TenantId,
            UserId,
            ClientId,
            TokenType,
            ExpiresAt));
    }

    public void AddReferenceToken(
        byte[] tokenHash,
        string clientName,
        string userName)
    {
        ReferenceToken = ReferenceToken.Create(Id, tokenHash);

        AddDomainEvent(
            new ReferenceTokenIssuedEvent(Id,
            TenantId,
            UserId,
            ClientId,
            TokenType,
            ExpiresAt));
    }

    public void AddRefreshToken(
        DateTime expiresAt,
        byte[] tokenHash,
        string ip,
        string clientName,
        string userName,
        Guid? parentRefreshTokenId = null,
        Guid? refreshTokenId = null)
    {
        RefreshToken = RefreshToken.Create(Id, tokenHash, expiresAt, parentRefreshTokenId, refreshTokenId);

        AddDomainEvent(
            new RefreshTokenIssuedEvent(Id,
            TenantId,
            UserId,
            ClientId,
            TokenType,
            expiresAt,
            CreatedByIpAddress));
    }

    public void RotateRefreshToken(Guid newRefreshTokenId)
    {
        if (RefreshToken == null)
        {
            throw new InvalidOperationException("Refresh token is missing.");
        }

        var currentRefreshTokenId = RefreshToken.Id;

        RefreshToken.Consume(newRefreshTokenId);

        AddDomainEvent(
            new TokenRefreshRotatedEvent(
                currentRefreshTokenId,
                newRefreshTokenId,
                TenantId,
                UserId ?? 0,
                ClientId,
                DateTime.UtcNow));
    }

    public void DetectRefreshTokenReuse()
    {
        if (RefreshToken == null)
        {
            throw new InvalidOperationException("Refresh token is missing.");
        }

        AddDomainEvent(
            new TokenRefreshReuseDetectedEvent(
                RefreshToken.Id,
                TenantId,
                UserId ?? 0,
                ClientId));
    }

    public void Revoke(string reason, string revokeByIp, int userId)
    {
        if (TokenStatus != TokenStatus.Active) return;

        TokenStatus = TokenStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevokeReason = reason;
        RevokedByIpAddress = revokeByIp;

        AddDomainEvent(
            new TokenRevokedEvent(Id,
            TenantId,
            userId,
            ClientId,
            RefreshToken != null ? "Refresh" : "Reference",
            RevokeReason));
    }

    public void Expire(int userId)
    {
        if (TokenStatus != TokenStatus.Active) return;
        TokenStatus = TokenStatus.Expired;
        ExpiresAt = DateTime.UtcNow;

        AddDomainEvent(
            new TokenExpiredEvent(Id,
            TenantId,
            userId,
            ClientId,
            RefreshToken != null ? "Refresh" : "Reference"));
    }
}
