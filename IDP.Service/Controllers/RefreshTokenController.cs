namespace IDP.Service.Controllers;

public class RefreshTokenController : ApiControllerBase
{
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IAppLogger<RefreshTokenController> _logger;

    public RefreshTokenController(RefreshTokenService refreshTokenService,
        IAppLogger<RefreshTokenController> appLogger)
    {
        _refreshTokenService = refreshTokenService;
        _logger = appLogger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<TokenResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRefreshToken(RefreshTokenRequest request)
    {
        string ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        _logger.LogInfo("GetRefreshToken called from IP: {IP}", ipAddress);

        var response = await _refreshTokenService.GenerateRefreshToken(request.RefreshToken,
            request.ClientId, ipAddress);

        _logger.LogInfo("Refresh token generated for ClientId: {ClientId}", request.ClientId);


        return OkResult(response);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(Result<object>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        string ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        _logger.LogInfo("RevokeToken called from IP: {IP}, Reason: {Reason}", ipAddress, request.ReasonRevoked);

        await _refreshTokenService.RevokeRefreshToken(request.RefreshToken, ipAddress, request.ReasonRevoked);

        _logger.LogInfo("Refresh token revoked for IP: {IP}", ipAddress);

        return OkResult(new { message = "Refresh token revoked." });
    }
}
