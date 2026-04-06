namespace TokenIDP.Domain.AggregateRoots.Users;

public enum PasswordResetRequestedByType
{
    SelfService = 0,
    Admin = 1
}

public class PasswordResetToken : AggregateRoot<long>
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public byte[] TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public PasswordResetRequestedByType RequestedByType { get; private set; }

    public virtual User User { get; private set; } = default!;

    private PasswordResetToken() { }

    private PasswordResetToken(
        int tenantId,
        int userId,
        byte[] tokenHash,
        DateTime expiresAt,
        PasswordResetRequestedByType requestedByType)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RequestedByType = requestedByType;
        IsUsed = false;
    }

    public static PasswordResetToken Create(
        int tenantId,
        int userId,
        byte[] tokenHash,
        DateTime expiresAt,
        PasswordResetRequestedByType requestedByType)
    {
        if (tenantId <= 0)
            throw new ArgumentException("TenantId must be greater than zero.", nameof(tenantId));

        if (userId <= 0)
            throw new ArgumentException("UserId must be greater than zero.", nameof(userId));

        if (tokenHash is null || tokenHash.Length == 0)
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        return new PasswordResetToken(tenantId, userId, tokenHash, expiresAt, requestedByType);
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public void MarkUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Password reset token has already been used.");

        IsUsed = true;
    }
}

