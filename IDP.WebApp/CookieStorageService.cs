namespace IDP.WebApp;

public class CookieStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieStorageService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void SaveToken(string token)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token", token, options);
    }

    public void SaveAuthorizationCode(string authorizationCode)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("auth_code", authorizationCode, options);
    }

    public string GetToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"] ?? string.Empty;
    }

    public void RemoveToken()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("access_token");
    }

    public string? GetAuthorizationCode()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["auth_code"];
    }

    public void Logout()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("access_token");
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("auth_code");
    }
}

