namespace TokenIDP.Core.Abstractions;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveAsync(string tenantKey, CancellationToken cancellationToken = default);
}
