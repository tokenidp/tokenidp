using System.Security.Claims;

namespace IDP.Core;

internal class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId => Convert.ToInt32(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));

    public int TenantId => Convert.ToInt32(_httpContextAccessor.HttpContext?.User?.FindFirstValue("uid"));

    public string CorrelationId => _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? string.Empty;

    public string UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string BaseUrl
    {
        get
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                throw new InvalidOperationException("BaseUrl is missing and no HTTP request is available to infer it.");
            }

            return $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase}".TrimEnd('/');
        }
    }

    public string[] GetRoles()
    {
        var claims = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role);

        if (claims != null && claims.Any())
        {
            return claims.Select(s => s.Value).ToArray();
        }
        return Array.Empty<string>();
    }
}
