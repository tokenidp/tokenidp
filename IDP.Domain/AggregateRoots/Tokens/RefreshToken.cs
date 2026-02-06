using System.Security;

namespace IDP.Domain.AggregateRoots.Tokens;

public class RefreshToken : Entity<Guid>
{
    public Guid TokenId { get; private set; }
    public byte[] TokenHash { get; private set; } = default!;
    public Guid? ParentTokenId { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public virtual Token Token { get; private set; } = default!;

    private RefreshToken() { }

    private RefreshToken(Guid tokenId, byte[] hash, DateTime expiresAt, Guid? parentId)
    {
        TokenId = tokenId;
        TokenHash = hash;
        ParentTokenId = parentId;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid tokenId, byte[] hash, DateTime expiresAt, Guid? parentId = null)
        => new RefreshToken(tokenId, hash, expiresAt, parentId);

    public bool IsConsumed => ConsumedAt.HasValue;

    public void Consume(Guid newTokenId)
    {
        if (IsConsumed)
            throw new SecurityException("Refresh token reuse detected");

        ConsumedAt = DateTime.UtcNow;
        ReplacedByTokenId = newTokenId;
    }
}