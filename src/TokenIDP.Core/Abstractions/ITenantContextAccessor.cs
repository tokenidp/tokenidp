namespace TokenIDP.Core.Abstractions;

public interface ITenantContextAccessor
{
    TenantContext? Current { get; }
    bool HasTenant { get; }
    bool IsSystemTenant { get; }
    string TenantKey { get; }
    int? CurrentTenantId { get; }
    int TenantId { get; }
    int ClientId { get; }
    bool ShouldBypassFilters { get; }

    void SetTenant(TenantContext tenantContext);
    void SetTenantId(int tenantId);
    void SetClientId(int clientId);
    IDisposable BeginFilterBypass();

    void Clear();
}

