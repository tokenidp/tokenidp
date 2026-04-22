using Microsoft.AspNetCore.Http;

namespace TokenIDP.Server.Multitenancy;

public interface ITenantRequestResolver
{
    TenantRequestResolutionResult Resolve(HttpContext httpContext);
}
