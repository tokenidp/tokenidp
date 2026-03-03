namespace IDP.Foundation.Abstractions;

public interface ITenantContextAccessor
{
    int TenantId { get; }
    void SetTenantId(int tenantId);
}
