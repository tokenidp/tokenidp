namespace TokenIDP.Core.OAuth.Abstractions;

public interface IRefreshTokenCookieService
{
    void Set(HttpContext context, string token);
    void Delete(HttpContext context);
    string? Get(HttpContext context);
}
