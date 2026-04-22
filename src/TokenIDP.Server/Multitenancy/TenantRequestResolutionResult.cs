namespace TokenIDP.Server.Multitenancy;

public sealed record TenantRequestResolutionResult(
    TenantRequestResolutionStatus Status,
    string? TenantKey = null,
    TenantResolutionSource? Source = null,
    string? FailureReason = null);
