namespace IDP.Service.Controllers;

public class TokenController : ApiControllerBase
{
    private readonly TokenServiceFactory _tokenServiceFactory;
    private readonly ClientService _clientService;
    private readonly IAppLogger<TokenController> _logger;

    public TokenController(TokenServiceFactory tokenServiceFactory,
         ClientService clientService,
        IAppLogger<TokenController> appLogger)
    {
        _tokenServiceFactory = tokenServiceFactory;
        _clientService = clientService;
        _logger = appLogger;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<TokenResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAccessToken(TokenRequest request)
    {
        string ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        _logger.LogInfo("GetAccessToken called for ClientId: {ClientId} from IP: {IP}", request.ClientId, ipAddress);

        var tokenType = await _clientService.GetClientTokenType(request.ClientId);

        if (!Enum.IsDefined(typeof(TokenType), tokenType))
        {
            _logger.LogWarning("TokenType not found for ClientId: {ClientId}", request.ClientId);
            return BadRequestResult(ApiError.Failure("Invalid client."));
        }

        ITokenService _tokenService = _tokenServiceFactory.GetService(tokenType);

        var response = await _tokenService.GenerateToken(request, ipAddress);

        _logger.LogInfo("Token generated for ClientId: {ClientId} with TokenType: {TokenType}", request.ClientId, tokenType);

        return OkResult(response);
    }
}