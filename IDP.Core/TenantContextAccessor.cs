namespace IDP.Core;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<int> _tenantId = new();

    public int TenantId => _tenantId.Value;

    public void SetTenantId(int tenantId)
    {
        _tenantId.Value = tenantId;
    }
}
