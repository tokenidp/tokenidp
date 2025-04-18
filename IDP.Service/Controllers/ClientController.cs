namespace IDP.Service.Controllers;

[Route("[controller]")]
[ApiController]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class ClientController : ControllerBase
{
    private readonly ClientService _clientService;
    private readonly IAppLogger<ClientController> _logger;

    public ClientController(ClientService clientService,
        IAppLogger<ClientController> appLogger)
    {
        _clientService = clientService;
        _logger = appLogger;
    }

    [HttpGet("{clientId}")]
    [ProducesResponseType(typeof(Result<ClientDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> IsValidClient(string clientId)
    {
        _logger.LogInfo("IsValidClient called for clientId: {ClientId}", clientId);

        var response = await _clientService.GetClientScope(clientId);

        _logger.LogInfo("IsValidClient result for clientId: {ClientId} is {Result}", clientId, response);

        return Ok(Result<ClientDto>.Success(response));
    }
}