namespace TokenIDP.Domain.AggregateRoots.Clients;

public sealed record ClientRateLimitProfile(
    string ClientId,
    int TenantId,
    int? PermitLimit,
    int? QueueLimit,
    TimeSpan? TimeWindow);
