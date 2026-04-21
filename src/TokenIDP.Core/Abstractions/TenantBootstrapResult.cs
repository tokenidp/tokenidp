namespace TokenIDP.Core.Abstractions;

public sealed record TenantBootstrapResult(
    int TenantId,
    string TenantKey,
    int AdminUserId,
    string AdminUserName,
    string TemporaryPassword,
    string DefaultClientId);
