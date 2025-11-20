using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Identity.Service.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class ApiControllerBase<T> : ControllerBase where T : class
{
    private readonly IAppLogger<T> _appLogger;

    public ApiControllerBase(IAppLogger<T> appLogger)
    {
        _appLogger = appLogger;
    }

    private ISender _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetService<ISender>();

    public override CreatedResult Created(string uri, [ActionResultObjectValue] object value)
    {
        return base.Created(uri, Result<object>.Success(value));
    }

    public override OkObjectResult Ok([ActionResultObjectValue] object value)
    {
        return base.Ok(Result<object>.Success(value));
    }

    public override BadRequestObjectResult BadRequest([ActionResultObjectValue] object error)
    {
        _appLogger.LogWarning(error, "BadRequest: {message}");

        return base.BadRequest(error);
    }

    public override ConflictObjectResult Conflict([ActionResultObjectValue] object error)
    {
        return base.Conflict(error);
    }

    public override NotFoundObjectResult NotFound([ActionResultObjectValue] object value)
    {
        return base.NotFound(Result<object>.Success(value));
    }

    public override ObjectResult StatusCode(int statusCode, [ActionResultObjectValue] object value)
    {
        return base.StatusCode(statusCode, value);
    }
}
