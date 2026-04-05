namespace IDP.Foundation.Abstractions;

public interface ITenantContextAccessor
{
    int TenantId { get; }
    int ClientId { get; }
    void SetTenantId(int tenantId);
    void SetClientId(int clientId);

    void Clear();
}
