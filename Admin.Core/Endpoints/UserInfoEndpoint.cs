using Admin.Core.Users;

namespace Admin.Core.Endpoints;

internal class UserInfoEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/userinfo")
            .RequireAuthorization()
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiError.Failure("Record Id should be greater than zero."));
            }

            var response = await userService.GetUserById(id);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("UserInfo")
        .WithTags("UserInfo");
    }
}
