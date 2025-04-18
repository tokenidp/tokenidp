using System.Security.Claims;

namespace Identity.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId => _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

    public int UserId => Convert.ToInt32(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));

    public int TenantId => Convert.ToInt32(_httpContextAccessor.HttpContext?.User?.FindFirstValue("uid"));

    public string UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

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
