namespace IDP.Core.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
internal partial class RefreshToken
{
    [Key]
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime Expires { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTime? Revoked { get; private set; }
    public string RevokedByIp { get; private set; }
    public string ReasonRevoked { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int CreatedBy { get; private set; }
    public virtual User User { get; private set; }
    private RefreshToken() { }

    public RefreshToken(int userId,
        string token,
        string ipAddress,
        double expiry)
    {
        UserId = userId;
        Token = token;
        CreatedByIp = ipAddress;
        Expires = DateTime.UtcNow.AddDays(expiry);
        CreatedBy = userId;
        CreatedOn = DateTime.UtcNow;
    }

    public void RevokeToken(string ipAddress,
        string reason)
    {
        Revoked = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReasonRevoked = reason;
    }
}

internal partial class RefreshToken
{
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}