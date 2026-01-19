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
            GetUserUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.GetUsers(data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Users")
        .WithTags("Users");

        authGroup.MapGet("/{id}", async (int id,
            GetUserUseCase userService,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await userService.GetUserById(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UserById")
        .WithTags("UserById");

        authGroup.MapGet("userlookups", async (GetUserLookupsUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.GetUserLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);

        })
        .WithName("UserLookups")
        .WithTags("UserLookups");

        authGroup.MapPost("/", async (UserDetail user,
            CreateUpdateUserUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.CreateUser(user, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"user/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateUser")
        .WithTags("CreateUser");

        authGroup.MapPut("/{id}", async (int id, UserDetail user,
            CreateUpdateUserUseCase userService,
            HttpContext httpContext) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await userService.UpdateUser(id, user, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateUser")
        .WithTags("UpdateUser");

        authGroup.MapPatch("/{id}", async (int id, UpdateUserStatus user,
            CreateUpdateUserUseCase userService,
            HttpContext httpContext) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await userService.UpdateUserStatus(id, user, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateUserStatus")
        .WithTags("UpdateUserStatus");

        authGroup.MapGet("/userclaims", async (
            GetUserPermissionsUseCase userService) =>
        {
            var response = await userService.GetUserPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UserClaims")
        .WithTags("UserClaims");
    }
}