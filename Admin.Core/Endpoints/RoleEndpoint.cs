using Admin.Core.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

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

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.GerRoles(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Roles")
        .WithTags("Roles");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await roleService.GetRoleById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("RoleById")
        .WithTags("RoleById");

        authGroup.MapPost("/", async (CreateUpdateRole role,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.CreateRole(role);

            var location = response.IsSuccess ? $"role/{role.Name}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateRole")
        .WithTags("CreateRole");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateRole role,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            if (id != role.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await roleService.UpdateRole(id, role);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateRole")
        .WithTags("UpdateRole");

        authGroup.MapDelete("/{id}", async (int id,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
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


        authGroup.MapPut("/{id}/permissions", async (int id, RolePermissionsUpdateRequest request,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.UpdateRolePermissions(id, request);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateRolePermissions")
        .WithTags("RolePermissions");
    }
}
