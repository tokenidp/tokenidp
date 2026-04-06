namespace TokenIDP.Domain.AggregateRoots.Users;

public class EmailConfirmationToken : AggregateRoot<long>
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public byte[] TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }

    public virtual User User { get; private set; } = default!;

    private EmailConfirmationToken() { }

    private EmailConfirmationToken(
        int tenantId,
        int userId,
        byte[] tokenHash,
        DateTime expiresAt)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        IsUsed = false;
    }

    public static EmailConfirmationToken Create(
        int tenantId,
        int userId,
        byte[] tokenHash,
        DateTime expiresAt)
    {
        if (tenantId <= 0)
            throw new ArgumentException("TenantId must be greater than zero.", nameof(tenantId));

        if (userId <= 0)
            throw new ArgumentException("UserId must be greater than zero.", nameof(userId));

        if (tokenHash is null || tokenHash.Length == 0)
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        return new EmailConfirmationToken(tenantId, userId, tokenHash, expiresAt);
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public void MarkUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Email confirmation token has already been used.");

        IsUsed = true;
    }
}
