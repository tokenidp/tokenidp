using Admin.Core.Roles;

namespace Admin.Core.Endpoints;

internal class RoleEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/Role");

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.GerRoles(data);

            return Results.Ok(response);
        })
        .WithName("Roles")
        .WithTags("Roles");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiError.Failure("Record Id should be greater than zero."));
            }

            var response = await roleService.GetRoleById(id);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("RoleById")
        .WithTags("RoleById");

        authGroup.MapPost("/", async (CreateUpdateRole role,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            var response = await roleService.CreateRole(role);

            return Results.Created($"role/{role.Name}", Result.Success(response.Id));
        })
        .WithName("CreateRole")
        .WithTags("CreateRole");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateRole role,
            IAppLogger<RoleEndpoint> _logger,
            RoleService roleService) =>
        {
            if (id != role.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await roleService.UpdateRole(id, role);

            return Results.NoContent();
        })
        .WithName("UpdateRole")
        .WithTags("UpdateRole");
    }
}
