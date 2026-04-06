namespace TokenIDP.Domain.AggregateRoots.Tokens;

public enum RevokeReason
{
    Logout,
    Admin,
    RefreshReuse,
    ClientDisabled,
    UserDisabled
}
