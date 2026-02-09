namespace IDP.Domain.AggregateRoots.Emails;

public enum EmailStatus : byte
{
    Pending = 0,
    Claimed = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
