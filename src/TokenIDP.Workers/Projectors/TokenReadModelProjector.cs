using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Workers.Projectors;

internal sealed class TokenReadModelProjector
{
    private readonly ApplicationDbContext _db;
    private readonly IClientStore _clientStore;
    private IAppLogger<TokenReadModelProjector> _appLogger;

    public TokenReadModelProjector(ApplicationDbContext db,
        IAppLogger<TokenReadModelProjector> appLogger,
        IClientStore clientStore)
    {
        _db = db;
        _appLogger = appLogger;
        _clientStore = clientStore;
    }

    public async Task ProjectAsync(OutboxEvent evt, CancellationToken ct)
    {
        switch (evt.EventType)
        {
            case nameof(JwtTokenIssuedEvent):
                await OnTokenIssued(evt, ct);
                break;

            case nameof(ReferenceTokenIssuedEvent):
                await OnReferenceIssued(evt, ct);
                break;

            case nameof(RefreshTokenIssuedEvent):
                await OnRefreshIssued(evt, ct);
                break;

            case nameof(TokenRevokedEvent):
                await OnTokenRevoked(evt, ct);
                break;

            case nameof(TokenExpiredEvent):
                await OnTokenExpired(evt, ct);
                break;
        }
    }

    private Task OnTokenIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenAsync<JwtTokenIssuedEvent>(evt, "JWT", ct);

    private Task OnRefreshIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenAsync<RefreshTokenIssuedEvent>(evt, "Refresh", ct);

    private Task OnReferenceIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenAsync<ReferenceTokenIssuedEvent>(evt, "Reference", ct);

    private async Task OnTokenRevoked(OutboxEvent evt, CancellationToken ct)
    {
        var e = JsonSerializer.Deserialize<TokenRevokedEvent>(evt.PayloadJson)!;

        var read = await _db.TokenReadModel
            .SingleAsync(x => x.SourceTokenId == e.TokenId && x.SourceType == e.SourceType, ct);

        var token = await _db.Tokens.Where(t => t.Id == e.TokenId)
            .Select(t => new { t.UpdatedBy, t.RevokedByIpAddress }).FirstOrDefaultAsync();

        read.Revoke($"user:{token?.UpdatedBy}", e.Reason, token?.RevokedByIpAddress);

        await _db.SaveChangesAsync(ct);
    }

    private async Task OnTokenExpired(OutboxEvent evt, CancellationToken ct)
    {
        var e = JsonSerializer.Deserialize<TokenExpiredEvent>(evt.PayloadJson)!;

        var read = await _db.TokenReadModel
            .SingleAsync(x => x.SourceTokenId == e.TokenId && x.SourceType == e.SourceType, ct);

        read.Expire();

        await _db.SaveChangesAsync(ct);
    }

    private async Task ProjectTokenAsync<TEvent>(OutboxEvent evt,
        string sourceType,
        CancellationToken ct)
        where TEvent : class
    {
        var e = JsonSerializer.Deserialize<TEvent>(evt.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(TEvent).Name}");

        var tokenId = GetTokenId(e);

        var token = await _db.Tokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tokenId, ct);

        if (token == null)
            throw new InvalidOperationException(
                $"Token not found for TokenId={tokenId}");

        var client = await _clientStore.GetClientShortInfo(token.ClientId);

        var read = TokenReadModel.Create(
            outboxEventId: evt.Id,
            tenantId: token.TenantId,
            sourceTokenId: token.Id,
            sourceType: sourceType,
            tokenIdHash: null,
            tokenType: token.TokenType.ToString(),
            grantType: token.GrantType.ToString(),
            clientId: client.Id,
            userId: token.UserId,
            subject: token.UserId.ToString(),
            issuedAt: token.IssuedAt,
            expiresAt: token.ExpiresAt,
            status: "Active",
            scopes: token.Scope,
            audience: token.Audience ?? string.Empty,
            issuedByIp: token.CreatedByIpAddress,
            issuedUserAgent: token.UserAgent,
            issuedBy: $"user:{token.UserId}"
        );

        _db.TokenReadModel.Add(read);

        await _db.SaveChangesAsync(ct);
    }

    private static Guid GetTokenId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.TokenId,
        RefreshTokenIssuedEvent e => e.TokenId,
        ReferenceTokenIssuedEvent e => e.TokenId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };
}

