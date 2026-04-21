namespace TokenIDP.Core.Abstractions;

public sealed record TenantResolutionResult(
    TenantContext Context,
    bool IsActive);
