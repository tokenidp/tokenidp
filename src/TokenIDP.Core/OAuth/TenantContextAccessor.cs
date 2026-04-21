using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.OAuth;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> _tenantContext = new();
    private static readonly AsyncLocal<int?> _clientId = new();
    private static readonly AsyncLocal<int> _bypassDepth = new();

    public TenantContext? Current => _tenantContext.Value;

    public bool HasTenant => _tenantContext.Value is not null;

    public bool IsSystemTenant => _tenantContext.Value?.IsSystemTenant == true;

    public string TenantKey => _tenantContext.Value?.TenantKey ?? string.Empty;

    public int TenantId =>
        _tenantContext.Value?.TenantId
        ?? throw new InvalidOperationException("TenantId is not set.");

    public int ClientId =>
        _clientId.Value
        ?? _tenantContext.Value?.ClientId
        ?? throw new InvalidOperationException("ClientId is not set.");

    public bool ShouldBypassFilters => _bypassDepth.Value > 0;

    public void SetTenant(TenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (tenantContext.TenantId <= 0)
            throw new ArgumentException("TenantId must be greater than zero.", nameof(tenantContext));

        if (string.IsNullOrWhiteSpace(tenantContext.TenantKey))
            throw new ArgumentException("TenantKey is required.", nameof(tenantContext));

        _tenantContext.Value = tenantContext;

        if (tenantContext.ClientId.HasValue && tenantContext.ClientId.Value > 0)
        {
            _clientId.Value = tenantContext.ClientId.Value;
        }
    }

    public void SetTenantId(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentException("TenantId must be greater than zero.");

        var current = _tenantContext.Value;
        var tenantKey = current?.TenantKey ?? string.Empty;

        _tenantContext.Value = new TenantContext(
            tenantId,
            tenantKey,
            current?.IsSystemTenant == true,
            current?.ClientId);
    }

    public void SetClientId(int clientId)
    {
        if (clientId <= 0)
            throw new ArgumentException("ClientId must be greater than zero.");

        _clientId.Value = clientId;

        if (_tenantContext.Value is { } current)
        {
            _tenantContext.Value = current with { ClientId = clientId };
        }
    }

    public IDisposable BeginFilterBypass()
    {
        _bypassDepth.Value++;
        return new FilterBypassScope();
    }

    public void Clear()
    {
        _tenantContext.Value = null;
        _clientId.Value = null;
        _bypassDepth.Value = 0;
    }

    private sealed class FilterBypassScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_bypassDepth.Value > 0)
            {
                _bypassDepth.Value--;
            }

            _disposed = true;
        }
    }
}
