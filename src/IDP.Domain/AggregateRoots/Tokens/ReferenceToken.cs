namespace IDP.Domain.AggregateRoots.Tokens;

public class ReferenceToken : Entity<Guid>
{
    public Guid TokenId { get; private set; }
    public byte[] TokenHash { get; private set; }

    private ReferenceToken() { }

    private ReferenceToken(Guid tokenId, byte[] hash)
    {
        TokenId = tokenId;
        TokenHash = hash;
    }

    public static ReferenceToken Create(Guid tokenId, byte[] hash)
        => new ReferenceToken(tokenId, hash);
}