namespace IDP.Service.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ApiError), (int)HttpStatusCode.InternalServerError)]
public class UserController : ControllerBase
{
    private readonly UserRepo _userService;
    private readonly IAppLogger<UserController> _logger;

    public UserController(UserRepo userService,
        IAppLogger<UserController> appLogger)
    {
        _userService = userService;
        _logger = appLogger;
    }

    [HttpGet("{userId}")]
    //[Authorize(Policy = "Profile")]
    [ProducesResponseType(typeof(Result<UserInfo>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUserInfo(int userId)
    {
        _logger.LogInfo("GetUserInfo called for userId: {UserId}", userId);

        var response = await _userService.GetUserInfo(userId);

        _logger.LogInfo("GetUserInfo completed for userId: {UserId}", userId);

        return Ok(Result<UserInfo>.Success(response));
    }
}