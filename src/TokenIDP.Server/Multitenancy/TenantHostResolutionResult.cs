namespace TokenIDP.Server.Multitenancy;

public sealed record TenantHostResolutionResult(
    TenantHostResolutionKind Kind,
    string? TenantKey = null);
