using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.OAuth;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<int?> _tenantId = new();
    private static readonly AsyncLocal<int?> _clientId = new();

    public int TenantId =>
        _tenantId.Value ?? throw new InvalidOperationException("TenantId is not set.");

    public int ClientId =>
        _clientId.Value ?? throw new InvalidOperationException("ClientId is not set.");

    public void SetTenantId(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentException("TenantId must be greater than zero.");

        _tenantId.Value = tenantId;
    }

    public void SetClientId(int clientId)
    {
        if (clientId <= 0)
            throw new ArgumentException("ClientId must be greater than zero.");

        _clientId.Value = clientId;
    }

    public void Clear()
    {
        _tenantId.Value = null;
        _clientId.Value = null;
    }
}
