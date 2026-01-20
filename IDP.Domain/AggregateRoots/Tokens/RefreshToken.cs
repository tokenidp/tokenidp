namespace IDP.Domain;

public partial class RefreshToken : BaseEntity
{
    public int UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime Expires { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTime? Revoked { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReasonRevoked { get; private set; }

    private RefreshToken() { }

    public RefreshToken(int userId,
        string token,
        string ipAddress,
        int expiry)
    {
        UserId = userId;
        Token = token;
        CreatedByIp = ipAddress;
        Expires = DateTime.UtcNow.AddDays(expiry);
    }

    public void RevokeToken(string ipAddress,
        string reason)
    {
        Revoked = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReasonRevoked = reason;
    }
}

public partial class RefreshToken
{
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}