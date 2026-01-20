using Admin.Core.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

internal class TenantEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/tenant")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            GetTenantUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenants(data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Tenants")
        .WithTags("Tenants");

        authGroup.MapGet("/{id}", async (int id,
            GetTenantUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetTenantById(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantById")
        .WithTags("TenantById");

        authGroup.MapGet("tenantlookups", async (GetTenantLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenantLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantLookups")
        .WithTags("TenantLookups");

        authGroup.MapPost("/", async (CreateUpdateTenant tenant,
            CreateUpdateTenantUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateTenant(tenant, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"tenant/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateTenant")
        .WithTags("CreateTenant");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateTenant tenant,
            CreateUpdateTenantUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id != tenant.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await useCase.UpdateTenant(id, tenant, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateTenant")
        .WithTags("UpdateTenant");

        authGroup.MapDelete("/{id}", async (int id,
            CreateUpdateTenantUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.DeleteTenant(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("DeleteTenant")
        .WithTags("DeleteTenant");
    }
}