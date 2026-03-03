using Admin.Core.Tenants;
using Admin.Core.Tenants.UseCases;
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
            TenantQueryUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenants(data, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tenants.view"
         })
        .WithName("Tenants")
        .WithTags("Tenants");

        authGroup.MapGet("/{id}", async (int id,
            TenantQueryUseCase useCase,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tenants.view"
         })
        .WithName("TenantById")
        .WithTags("Tenants");

        authGroup.MapGet("tenantlookups", async (TenantLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenantLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantLookups")
        .WithTags("Tenants");

        authGroup.MapPost("/", async (CreateUpdateTenant tenant,
            TenantCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateTenant(tenant, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"tenant/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tenants.add"
         })
        .WithName("CreateTenant")
        .WithTags("Tenants");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateTenant tenant,
            TenantCommandUseCase useCase,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tenants.edit"
         })
        .WithName("UpdateTenant")
        .WithTags("Tenants");

        authGroup.MapDelete("/{id}", async (int id,
            TenantCommandUseCase useCase,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tenants.delete"
         })
        .WithName("DeleteTenant")
        .WithTags("Tenants");

        authGroup.MapPost("/{id}/reveal-secret", async (int id,
            RevealTenantProviderSecretRequest request,
            TenantQueryUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.RevealTenantProviderSecret(id, request, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "Tenant.Secret.Reveal"
        })
        .WithName("RevealTenantProviderSecret")
        .WithTags("Tenants");
    }
}