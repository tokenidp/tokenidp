using Admin.Core.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

internal class TenantEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/Tenant")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            var response = await tenantService.GetTenants(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Tenants")
        .WithTags("Tenants");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await tenantService.GetTenantById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantById")
        .WithTags("TenantById");

        authGroup.MapPost("/", async (CreateUpdateTenant tenant,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            var response = await tenantService.CreateTenant(tenant);

            var location = response.IsSuccess ? $"tenant/{tenant.TenantName}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateTenant")
        .WithTags("CreateTenant");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateTenant tenant,
            IAppLogger<TenantEndpoint> _logger,
            TenantService tenantService) =>
        {
            if (id != tenant.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await tenantService.UpdateTenant(id, tenant);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateTenant")
        .WithTags("UpdateTenant");
    }
}
