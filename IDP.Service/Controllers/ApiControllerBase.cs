using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace IDP.Service.Controllers;

[Route("[controller]")]
[ApiController]
[ProducesResponseType(typeof(Result<ApiError>), (int)HttpStatusCode.InternalServerError)]
[ProducesResponseType(typeof(Result<ApiError>), (int)HttpStatusCode.Unauthorized)]
[ProducesResponseType(typeof(Result<ApiError>), (int)HttpStatusCode.BadRequest)]
[ProducesResponseType(typeof(Result<ApiError>), (int)HttpStatusCode.Conflict)]
public class ApiControllerBase: ControllerBase
{
    public IActionResult CreateResult<TResult>(string uri, TResult value)
    {
        return base.Created(uri, Result<TResult>.Success(value));
    }

    public IActionResult OkResult<TResult>(TResult value)
    {
        return base.Ok(Result<TResult>.Success(value));
    }

    public IActionResult BadRequestResult<TResult>(TResult error)
    {
        return base.BadRequest(Result<TResult>.Failure(error));
    }

    public IActionResult NotFoundResult<TResult>(TResult error)
    {
        return base.NotFound(Result<TResult>.Failure(error));
    }

    public IActionResult UnauthorizedResult<TResult>(TResult error)
    {
        return base.Unauthorized(Result<TResult>.Failure(error));
    }

    public IActionResult ConflictResult<TResult>(TResult error)
    {
        return base.Conflict(Result<TResult>.Failure(error));
    }

    public override ObjectResult StatusCode(int statusCode, [ActionResultObjectValue] object value)
    {
        return base.StatusCode(statusCode, value);
    }
}
