namespace TokenIDP.Core.OAuth;

internal sealed class RefreshTokenResponseTransport
{
    private readonly IAppLogger<RefreshTokenResponseTransport> _logger;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;

    public RefreshTokenResponseTransport(
        IAppLogger<RefreshTokenResponseTransport> logger,
        IRefreshTokenCookieService refreshTokenCookieService)
    {
        _logger = logger;
        _refreshTokenCookieService = refreshTokenCookieService;
    }

    public void Apply(HttpContext context, TokenResponse response)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(response);

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            return;
        }

        switch (response.RefreshTokenDeliveryMode)
        {
            case RefreshTokenDeliveryMode.Response:
                return;
            case RefreshTokenDeliveryMode.Cookie:
                _refreshTokenCookieService.Set(context, response.RefreshToken);
                response.RemoveRefreshToken();
                return;
            case RefreshTokenDeliveryMode.Both:
                _refreshTokenCookieService.Set(context, response.RefreshToken);
                return;
            default:
                _logger.LogWarning(
                    "Unknown refresh token delivery mode {Mode}. Falling back to response transport.",
                    response.RefreshTokenDeliveryMode);
                return;
        }
    }
}
