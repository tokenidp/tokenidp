using IDP.Core.Admin.Model.Configurations;
using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.BadRequest)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class ConfigurationController : ApiControllerBase<ConfigurationController>
{
    public ConfigurationController(IAppLogger<ConfigurationController> appLogger) : base(appLogger)
    {

    }

    [HttpPost("list")]
    [ProducesResponseType(typeof(Result<PaginatedList<ConfigurationSearchDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(GetConfigurations request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<ConfigurationDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiError.Failure("Record Id should be greater than zero."));
        }

        var response = await Mediator.Send(new GetConfigurationById { Id = id });

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Create(CreateConfiguration request)
    {
        var response = await Mediator.Send(request);

        return Created($"user/{request.ConfigKey}", response.Id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> Update(int id, UpdateConfiguration request)
    {
        if (id != request.Id)
        {
            return BadRequest(ApiError.Failure("Record Ids didn't match."));
        }

        request.Id = id;

        await Mediator.Send(request);

        return NoContent();
    }
}
