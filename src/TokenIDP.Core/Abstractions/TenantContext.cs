namespace TokenIDP.Core.Abstractions;

public sealed record TenantContext(
    int TenantId,
    string TenantKey,
    bool IsSystemTenant,
    int? ClientId = null);
