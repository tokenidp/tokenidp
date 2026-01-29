using IDP.Domain.AggregateRoots;
using IDP.Domain.DomainEvents;
using IDP.Infrastructure.Persistence.ReadModels;
using System.Text.Json;

namespace IDP.Infrastructure.Persistence;

internal sealed class TokenReadModelStore : ITokenReadModelStore
{
    private readonly ApplicationDbContext _db;
    private IAppLogger<TokenReadModelStore> _appLogger;

    public TokenReadModelStore(ApplicationDbContext db,
        IAppLogger<TokenReadModelStore> appLogger)
    {
        _db = db;
        _appLogger = appLogger;
    }

    public async Task ProjectAsync(OutboxEvent evt, CancellationToken ct)
    {
        switch (evt.EventType)
        {
            case OutboxEventTypes.TokenIssued:
                await OnTokenIssued(evt, ct);
                break;

            case OutboxEventTypes.ReferenceTokenIssued:
                await OnReferenceIssued(evt, ct);
                break;

            case OutboxEventTypes.RefreshTokenIssued:
                await OnRefreshIssued(evt, ct);
                break;

            case OutboxEventTypes.TokenRevoked:
                await OnTokenRevoked(evt, ct);
                break;

            case OutboxEventTypes.TokenExpired:
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

        var read = new TokenReadModel(
            tenantId: token.TenantId,
            sourceTokenId: token.Id,
            sourceType: sourceType,
            tokenIdHash: null,
            tokenType: token.TokenType.ToString(),
            clientId: token.ClientId,
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
