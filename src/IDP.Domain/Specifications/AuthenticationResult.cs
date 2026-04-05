namespace IDP.Domain.Specifications;

public enum AuthenticationResult
{
    Success,
    Failed,
    Requested,
    Completed,
    Locked,
    Unlocked
}