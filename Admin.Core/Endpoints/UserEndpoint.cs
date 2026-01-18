using Admin.Core.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;


namespace Admin.Core.Endpoints;

internal class UserEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/user")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            UserUseCases userService) =>
        {
            var response = await userService.GetUsers(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Users")
        .WithTags("Users");

        authGroup.MapGet("/{id}", async (int id,
            UserUseCases userService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await userService.GetUserById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UserById")
        .WithTags("UserById");

        authGroup.MapPost("/", async (CreateUpdateUser user,
            UserUseCases userService) =>
        {
            var response = await userService.CreateUser(user);

            var location = response.IsSuccess ? $"user/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateUser")
        .WithTags("CreateUser");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateUser user,
            UserUseCases userService) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await userService.UpdateUser(id, user);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateUser")
        .WithTags("UpdateUser");

        authGroup.MapPatch("/{id}", async (int id, UpdateUserStatus user,
            UserUseCases userService) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await userService.UpdateUserStatus(id, user);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateUserStatus")
        .WithTags("UpdateUserStatus");

        authGroup.MapGet("/userclaims", async (
            UserUseCases userService) =>
        {
            var response = await userService.GetUserPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UserClaims")
        .WithTags("UserClaims");
    }
}
