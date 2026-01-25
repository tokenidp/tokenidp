using Admin.Core.Roles;
using Admin.Core.Roles.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Core.Endpoints;

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
        .WithName("Roles")
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
        .WithName("RoleById")
        .WithTags("RoleById");

        authGroup.MapPost("/", async ([FromBody] CreateUpdateRole role,
            [FromServices] RoleCommandUseCase roleService,
            HttpContext httpContext) =>
        {
            var response = await roleService.CreateRole(role, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"role/{role.RoleName}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateRole")
        .WithTags("CreateRole");

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
        .WithName("UpdateRole")
        .WithTags("UpdateRole");

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
        .WithName("DeleteRole")
        .WithTags("DeleteRole");
    }
}