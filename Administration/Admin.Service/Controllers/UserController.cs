using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.BadRequest)]
public class UserController : ApiControllerBase<UserController>
{
    public UserController(IAppLogger<UserController> appLogger) : base(appLogger)
    {

    }

    [HttpPost("list")]
    [Authorize(Policy = "UsersManagement")]
    [ProducesResponseType(typeof(Result<PaginatedList<UserSearchDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(GetUsers request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }

    [HttpGet("lookups")]
    [Authorize(Policy = "UsersManagement")]
    [ProducesResponseType(typeof(Result<UserLookups>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        var response = await Mediator.Send(new GetUserLookups());

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "UM_CanEdit")]
    [ProducesResponseType(typeof(Result<UserDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiError.Failure("Record Id should be greater than zero."));
        }

        var response = await Mediator.Send(new GetUserById { Id = id });

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "UM_CanAdd")]
    [ProducesResponseType(typeof(Result<int>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Create(CreateUser request)
    {
        var response = await Mediator.Send(request);

        return Created($"user/{response.Id}", request.UserName);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "UM_CanEdit")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> Update(int id, UpdateUser request)
    {
        if (id != request.Id)
        {
            return BadRequest(ApiError.Failure("Record Ids didn't match."));
        }

        request.Id = id;

        await Mediator.Send(request);

        return NoContent();
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "UM_CanEdit")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatus request)
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
