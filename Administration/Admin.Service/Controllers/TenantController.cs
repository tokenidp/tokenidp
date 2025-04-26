using Identity.Application.Common.Models;
using Identity.Application.Tenants.Commands;
using Identity.Application.Tenants.Queries;
using Services.Common.Model;
using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.BadRequest)]
public class TenantController : ApiControllerBase<TenantController>
{
    public TenantController(IAppLogger<TenantController> appLogger) : base(appLogger)
    {

    }

    [HttpPost("list")]
    [ProducesResponseType(typeof(Result<PaginatedList<TenantSearchDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(GetTenants request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<TenantDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiError.Failure("Record Id should be greater than zero."));
        }

        var response = await Mediator.Send(new GetTenantById { Id = id });

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Create(CreateTenant request)
    {
        var response = await Mediator.Send(request);

        return Created($"user/{request.TenantName}", response.Id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> Update(int id, UpdateTenant request)
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
