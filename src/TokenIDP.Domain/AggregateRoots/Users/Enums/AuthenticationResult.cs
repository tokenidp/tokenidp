namespace TokenIDP.Domain.AggregateRoots.Users.Enums;

public enum AuthenticationResult
{
    Success,
    Failed,
    Requested,
    Completed,
    Locked,
    Unlocked
}
