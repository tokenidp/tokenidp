using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.BadRequest)]
public class ReportController : ApiControllerBase<ReportController>
{
    public ReportController(IAppLogger<ReportController> appLogger) : base(appLogger)
    {

    }

    [HttpPost("list")]
    [ProducesResponseType(typeof(Result<PaginatedList<ReportSearchDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(GetReports request)
    {
        var response = await Mediator.Send(request);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<ReportDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiError.Failure("Record Id should be greater than zero."));
        }

        var response = await Mediator.Send(new GetReportById { Id = id });

        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Create(CreateReport request)
    {
        var response = await Mediator.Send(request);

        return Created($"user/{request.ReportName}", response.Id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> Update(int id, UpdateReport request)
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
