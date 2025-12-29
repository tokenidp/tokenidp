using Admin.Core.Tenants;

namespace Admin.Core.Endpoints;

internal class TenantEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/Tenant");

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            var response = await tenantService.GetTenants(data);

            return Results.Ok(response);
        })
        .WithName("Tenants")
        .WithTags("Tenants");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiError.Failure("Record Id should be greater than zero."));
            }

            var response = await tenantService.GetTenantById(id);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("TenantById")
        .WithTags("TenantById");

        authGroup.MapPost("/", async (CreateUpdateTenant tenant,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            var response = await tenantService.CreateTenant(tenant);

            return Results.Created($"tenant/{tenant.TenantName}", Result.Success(response.Id));
        })
        .WithName("CreateTenant")
        .WithTags("CreateTenant");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateTenant tenant,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            if (id != tenant.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await tenantService.UpdateTenant(id, tenant);

            return Results.NoContent();
        })
        .WithName("UpdateTenant")
        .WithTags("UpdateTenant");
    }
}