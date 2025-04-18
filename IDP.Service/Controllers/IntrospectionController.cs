namespace IDP.Service.Controllers;

[Route("introspect")]
[ApiController]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class IntrospectionController : ControllerBase
{
    private readonly IReferenceTokenValidator _referenceTokenValidator;
    private readonly IAppLogger<IntrospectionController> _logger;

    public IntrospectionController(IReferenceTokenValidator referenceTokenValidator,
        IAppLogger<IntrospectionController> logger)
    {
        _referenceTokenValidator = referenceTokenValidator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<TokenResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Introspect(IntrospectionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogWarning("Introspect called with invalid request");
            return BadRequest("Invalid request");
        }

        _logger.LogInfo("Introspect called for token (partial): {TokenPartial}",
            $"{request.Token?.Substring(request.Token.Length - 5, 5)}...");

        var response = await _referenceTokenValidator.ValidateReferenceToken(request.Token);

        _logger.LogInfo("Introspect completed. Active: {IsActive}", response?.Active);

        return Ok(response);
    }
}
