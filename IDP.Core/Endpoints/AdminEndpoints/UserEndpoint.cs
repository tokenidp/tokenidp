using IDP.Core.Admin;
using IDP.Core.Admin.Users;
using IDP.Core.Common.Interfaces;
using IDP.Core.Common.Model;

namespace IDP.Core.Endpoints.AdminEndpoints;

internal class UserEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/user")
            .RequireAuthorization();

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            var response = await userService.GetUsers(data);

            return Results.Ok(response);
        })
        .WithName("Users")
        .WithTags("Users");

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
        .WithName("UserById")
        .WithTags("UserById");
      
        authGroup.MapPost("/", async (CreateUpdateUser user,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            var response = await userService.CreateUser(user);

            return Results.Created($"user/{response.Id}", Result.Success(response.Id));
        })
        .WithName("CreateUser")
        .WithTags("CreateUser");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateUser user,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await userService.UpdateUser(id, user);

            return Results.NoContent();
        })
        .WithName("UpdateUser")
        .WithTags("UpdateUser");

        authGroup.MapPatch("/{id}", async (int id, UpdateUserStatus user,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await userService.UpdateUserStatus(id, user);

            return Results.NoContent();
        })
        .WithName("UpdateUserStatus")
        .WithTags("UpdateUserStatus");

        authGroup.MapGet("/userclaims/{userId}", async (int userId,
            IAppLogger<UserEndpoint> _logger,
            UserService userService) =>
        {
            _logger.LogInfo("GetUserInfo called for userId: {UserId}", userId);

            var response = await userService.GetUserClaims(userId);

            _logger.LogInfo("GetUserInfo completed for userId: {UserId}", userId);

            return Results.Ok(response);
        })
        .WithName("UserClaims")
        .WithTags("UserClaims");
    }
}
