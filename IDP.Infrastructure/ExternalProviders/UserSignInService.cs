using IDP.ExternalProviders.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace IDP.Infrastructure.ExternalProviders;

internal sealed class UserSignInService : IUserSignInService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSignInService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SignInAsync(
        int userId,
        string userName,
        string email,
        int tenantId,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available for sign-in.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new("uid", tenantId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            "idp_session",
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
            });
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = _httpContextAccessor.HttpContext
                            ?? throw new InvalidOperationException("HTTP context is not available for sign-out.");

        await context.SignOutAsync("idp_session");
    }
}
