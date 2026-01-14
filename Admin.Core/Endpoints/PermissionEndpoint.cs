using Admin.Core.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

internal class PermissionEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/permissions")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("/", async (
            IAppLogger<PermissionEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.GetPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Permissions")
        .WithTags("Permissions");

        authGroup.MapGet("/parents", async (
            IAppLogger<PermissionEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.GetParentPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("PermissionParents")
        .WithTags("Permissions");

        authGroup.MapPost("/", async (CreatePermissionRequest request,
            IAppLogger<PermissionEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.CreatePermission(request);

            var location = response.IsSuccess
                ? $"permissions/{response.Value?.Id}"
                : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreatePermission")
        .WithTags("Permissions");
    }
}
