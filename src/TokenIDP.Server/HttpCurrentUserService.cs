using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Server;

internal class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _baseUrl;

    public string? IpAddress { get; }
    public string? UserAgent { get; }

    public HttpCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TokenOptions> tokenOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _baseUrl = TokenOptionsResolver.ResolveIssuer(tokenOptions.Value);

        var ctx = _httpContextAccessor.HttpContext;

        IpAddress = ctx?.Request.Headers["X-Forwarded-For"]
            .FirstOrDefault()?.Split(',').FirstOrDefault()
            ?? ctx?.Connection.RemoteIpAddress?.ToString();

        UserAgent = ctx?.Request.Headers["User-Agent"].ToString();
    }

    public int UserId => TryGetIntClaim(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub) ?? 0;

    public int TenantId => TryGetIntClaim("uid") ?? 0;

    public string ClientId => GetClaimValue("client_id") ?? string.Empty;

    public Guid CorrelationId
    {
        get
        {
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

            Guid.TryParse(correlationId, out var guid);

            return guid;
        }
    }

    public string UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string BaseUrl => _baseUrl;

    public string Scopes
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindAll("scope").Select(c => c.Value).FirstOrDefault() ?? string.Empty;
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

    private string? GetClaimValue(params string[] claimTypes)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        foreach (var claimType in claimTypes)
        {
            var value = user?.FindFirstValue(claimType);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private int? TryGetIntClaim(params string[] claimTypes)
    {
        var value = GetClaimValue(claimTypes);

        return int.TryParse(value, out var parsedValue)
            ? parsedValue
            : null;
    }
}

