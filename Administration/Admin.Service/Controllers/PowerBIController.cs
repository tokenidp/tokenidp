using Identity.Application.PowerBI;
using Services.Common.Model;
using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class PowerBIController : ApiControllerBase<PowerBIController>
{
    public PowerBIController(IAppLogger<PowerBIController> appLogger) : base(appLogger)
    {

    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<PowerBIResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetEmbedUrl(PowerBIRequest request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }
}
