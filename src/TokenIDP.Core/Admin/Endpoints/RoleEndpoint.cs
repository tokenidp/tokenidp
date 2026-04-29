using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TokenIDP.Core.Admin.Roles;
using TokenIDP.Core.Admin.Roles.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal class RoleEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/role")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async ([FromBody] SearchData data,
            [FromServices] RoleQueryUseCase roleService,
            HttpContext httpContext) =>
        {
            var response = await roleService.GerRoles(data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.view"
         })
        .WithName("Roles")
        .WithTags("Roles");

        authGroup.MapPost("/user-counts", async ([FromBody] RoleUserCountRequest request,
            [FromServices] RoleQueryUseCase roleService,
            HttpContext httpContext) =>
        {
            var response = await roleService.GetRoleUserCounts(request, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "roles.view"
        })
        .WithName("RoleUserCounts")
        .WithTags("Roles");

        authGroup.MapGet("/{id}", async (int id,
            [FromServices] RoleQueryUseCase roleService,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await roleService.GetRoleById(id, httpContext.RequestAborted);

            var location = response.IsSuccess
                ? $"admin/role/{response.Value?.Id}"
                : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.view"
         })
        .WithName("RoleById")
        .WithTags("Roles");

        authGroup.MapPost("/{id}/users/list", async (int id,
            [FromBody] SearchData data,
            [FromServices] RoleQueryUseCase roleService,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await roleService.GetUsersByRole(id, data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "roles.view"
        })
        .WithName("RoleUsers")
        .WithTags("Roles");

        authGroup.MapPost("/", async ([FromBody] CreateUpdateRole role,
            [FromServices] RoleCommandUseCase roleService,
            HttpContext httpContext) =>
        {
            var response = await roleService.CreateRole(role, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"role/{role.RoleName}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.add"
         })
        .WithName("CreateRole")
        .WithTags("Roles");

        authGroup.MapPut("/{id}", async (int id, [FromBody] CreateUpdateRole role,
            [FromServices] RoleCommandUseCase roleService,
            HttpContext httpContext) =>
        {
            if (id != role.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await roleService.UpdateRole(id, role, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.edit"
         })
        .WithName("UpdateRole")
        .WithTags("Roles");

        authGroup.MapDelete("/{id}", async (int id,
            [FromServices] RoleCommandUseCase roleService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await roleService.DeleteRole(id);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.delete"
         })
        .WithName("DeleteRole")
        .WithTags("Roles");
    }
}
