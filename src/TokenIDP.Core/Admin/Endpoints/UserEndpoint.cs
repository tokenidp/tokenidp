using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Core.Admin.Endpoints;

internal class UserEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/user")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            UserQueryUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.GetUsers(data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "users.view"
        })
        .WithName("Users")
        .WithTags("Users");

        authGroup.MapGet("/{id}", async (int id,
            UserQueryUseCase userService,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await userService.GetUserById(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        }).RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "users.view"
        })
        .WithName("UserById")
        .WithTags("Users");

        authGroup.MapGet("userlookups", async (UserLookupsUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.GetUserLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);

        })
        .WithName("UserLookups")
        .WithTags("Users");

        authGroup.MapPost("/", async (UserDetail user,
            UserCommandUseCase userService,
            HttpContext httpContext) =>
        {
            var response = await userService.CreateUser(user, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"user/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "users.add"
         })
        .WithName("CreateUser")
        .WithTags("Users");

        authGroup.MapPut("/{id}", async (int id, UserDetail user,
            UserCommandUseCase userService,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "users.edit"
         })
        .WithName("UpdateUser")
        .WithTags("Users");

        authGroup.MapPatch("/{id}", async (int id, UpdateUserStatus user,
            UserCommandUseCase userService,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "users.edit"
         })
        .WithName("UpdateUserStatus")
        .WithTags("Users");

        authGroup.MapGet("/permissions", async (
            UserPermissionsUseCase userService) =>
        {
            var response = await userService.GetUserPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UserPermissions")
        .WithTags("Users");

        authGroup.MapPost("/{id}/reset-password", async (int id,
            PasswordResetUseCase passwordResetUseCase,
            HttpContext httpContext) =>
        {
            var command = new InitiateAdminPasswordResetCommand { UserId = id };

            var response = await passwordResetUseCase
                .InitiateAdminPasswordReset(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "users.resetpassword"
        })
        .WithName("AdminPasswordReset")
        .WithTags("PasswordReset");

        authGroup.MapDelete("/{id}", async (int id,
            UserCommandUseCase userService,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await userService.DeleteUser(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "users.delete"
        })
        .WithName("DeleteUser")
        .WithTags("Users");
    }
}
