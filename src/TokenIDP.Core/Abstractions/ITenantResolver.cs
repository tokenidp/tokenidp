namespace TokenIDP.Core.Abstractions;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
