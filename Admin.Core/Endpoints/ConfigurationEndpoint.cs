using Admin.Core.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

internal class ConfigurationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/configuration")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            GetTenantConfigurationsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenantConfigurations(data, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantConfigurations")
        .WithTags("TenantConfigurations");

        authGroup.MapGet("/{id:int}", async (int id,
            GetTenantConfigurationByIdUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetConfigurationById(id, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantConfigurationById")
        .WithTags("TenantConfigurationById");

        authGroup.MapGet("/key/{key}", async (string key,
            GetTenantConfigurationByKeyUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetConfigurationByKey(key, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TenantConfigurationByKey")
        .WithTags("TenantConfigurationByKey");

        authGroup.MapPost("/", async (CreateUpdateConfiguration configuration,
            CreateTenantConfigurationUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateConfiguration(configuration, httpContext.RequestAborted);
            var location = response.IsSuccess ? $"configuration/{configuration.ConfigKey}" : string.Empty;
            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateTenantConfiguration")
        .WithTags("CreateTenantConfiguration");

        authGroup.MapPut("/{id:int}", async (int id, CreateUpdateConfiguration configuration,
            UpdateTenantConfigurationUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id != configuration.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await useCase.UpdateConfiguration(id, configuration, httpContext.RequestAborted);
            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateTenantConfiguration")
        .WithTags("UpdateTenantConfiguration");

        authGroup.MapDelete("/{id:int}", async (int id,
            DeleteTenantConfigurationUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.DeleteConfiguration(id, httpContext.RequestAborted);
            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("DeleteTenantConfiguration")
        .WithTags("DeleteTenantConfiguration");

        authGroup.MapPost("/upsert", async (CreateUpdateConfiguration configuration,
            UpsertTenantConfigurationUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.UpsertConfiguration(configuration, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("UpsertTenantConfiguration")
        .WithTags("UpsertTenantConfiguration");

        authGroup.MapPost("/bulk", async (BulkUpdateTenantConfigurations request,
            BulkUpdateTenantConfigurationsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.BulkUpdate(request, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("BulkUpdateTenantConfigurations")
        .WithTags("BulkUpdateTenantConfigurations");
    }
}