using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Core.OAuth;

internal sealed class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private readonly RefreshTokenCookieOptions _options;

    public RefreshTokenCookieService(IOptions<RefreshTokenCookieOptions> options)
    {
        _options = options.Value;
    }

    public void Set(HttpContext context, string token)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        context.Response.Cookies.Append(
            _options.CookieName,
            token,
            BuildCookieOptions());
    }

    public void Delete(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Cookies.Delete(
            _options.CookieName,
            BuildCookieOptions());
    }

    public string? Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Request.Cookies.TryGetValue(_options.CookieName, out var value) &&
               !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = _options.HttpOnly,
            Secure = _options.Secure,
            SameSite = _options.SameSite,
            Path = string.IsNullOrWhiteSpace(_options.Path) ? "/token" : _options.Path,
            IsEssential = _options.IsEssential
        };
    }
}
