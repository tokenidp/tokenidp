namespace TokenIDP.Core.Foundation.Options;

public sealed class RefreshTokenCookieOptions
{
    public const string SectionName = "RefreshTokenCookie";

    public string CookieName { get; set; } = "tt_refresh";
    public bool HttpOnly { get; set; } = true;
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
    public string Path { get; set; } = "/token";
    public bool IsEssential { get; set; } = true;
}
