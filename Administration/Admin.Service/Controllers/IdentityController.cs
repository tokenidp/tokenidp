using Identity.Application.Identity.Authentication;
using Services.Common.Model;
using System.Net;

namespace Identity.Service.Controllers;

[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class IdentityController : ApiControllerBase<IdentityController>
{
    public IdentityController(IAppLogger<IdentityController> appLogger) : base(appLogger)
    {

    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Login(AuthRequest request)
    {
        var response = await Mediator.Send(request);

        if (response?.IsSuccess == true)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
        }

        return Ok(response);
    }

    [HttpPost("refres-token")]
    [ProducesResponseType(typeof(Result<AuthResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var request = new RefreshTokenRequest()
        {
            RefreshToken = refreshToken,
            IPAddress = IPAddress()
        };

        var response = await Mediator.Send(request);

        if (response?.IsSuccess == true)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
        }

        return Ok(response);
    }

    private string IPAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
        else
            return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
    }
}
