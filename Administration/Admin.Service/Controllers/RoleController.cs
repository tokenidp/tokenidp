using IDP.Core.Admin.Roles;
using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.BadRequest)]
public class RoleController : ApiControllerBase<RoleController>
{
    public RoleController(IAppLogger<RoleController> appLogger) : base(appLogger)
    {

    }

    [HttpPost("list")]
    [ProducesResponseType(typeof(Result<PaginatedList<RoleSearchDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(GetRoles request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<RoleDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiError.Failure("Record Id should be greater than zero."));
        }

        var response = await Mediator.Send(new GetRoleById { Id = id });

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Create(CreateRole request)
    {
        var response = await Mediator.Send(request);

        return Created($"user/{request.Name}", response.Id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> Update(int id, UpdateRole request)
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
