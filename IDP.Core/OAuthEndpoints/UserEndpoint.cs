using IDP.Core.Admin.Services;

namespace IDP.Core.OAuthEndpoints;

internal class UserEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/userinfo");

        authGroup.MapGet("/{userId}", async (int userId,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            _logger.LogInfo("GetUserInfo called for userId: {UserId}", userId);

            var response = await userService.GetUserClaims(userId);

            _logger.LogInfo("GetUserInfo completed for userId: {UserId}", userId);

            return Results.Ok(response);
        })
        .WithName("UserInfo")
        .WithTags("UserInfo");
    }
}
