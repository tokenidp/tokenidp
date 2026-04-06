using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Core.Admin.Settings.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Core.Admin.Endpoints;

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
            ConfigurationsQueryUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetTenantConfigurations(data, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "settings.view"
         })
        .WithName("TenantConfigurations")
        .WithTags("TenantConfigurations");

        authGroup.MapGet("/{id:int}", async (int id,
            ConfigurationQueryByIdUseCase useCase,
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
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "settings.view"
         })
        .WithName("TenantConfigurationById")
        .WithTags("TenantConfigurations");

        authGroup.MapGet("/key/{key}", async (string key,
            ConfigurationQueryByKeyUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetConfigurationByKey(key, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.view"
        })
        .WithName("TenantConfigurationByKey")
        .WithTags("TenantConfigurations");

        authGroup.MapPost("/", async (CreateUpdateConfiguration configuration,
            ConfigurationCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateConfiguration(configuration, httpContext.RequestAborted);
            var location = response.IsSuccess ? $"configuration/{configuration.ConfigKey}" : string.Empty;
            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.add"
        })
        .WithName("CreateTenantConfiguration")
        .WithTags("TenantConfigurations");

        authGroup.MapPut("/{id:int}", async (int id, CreateUpdateConfiguration configuration,
            ConfigurationUpdateCommandUseCase useCase,
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
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.edit"
        })
        .WithName("UpdateTenantConfiguration")
        .WithTags("TenantConfigurations");

        authGroup.MapDelete("/{id:int}", async (int id,
            ConfigurationDeleteCommandUseCase useCase,
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
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.delete"
        })
        .WithName("DeleteTenantConfiguration")
        .WithTags("TenantConfigurations");

        authGroup.MapPost("/upsert", async (CreateUpdateConfiguration configuration,
            ConfigurationUpsertCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.UpsertConfiguration(configuration, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.edit"
        })
        .WithName("UpsertTenantConfiguration")
        .WithTags("TenantConfigurations");

        authGroup.MapPost("/bulk", async (BulkUpdateTenantConfigurations request,
            ConfigurationsBulkCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.BulkUpdate(request, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "settings.edit"
        })
        .WithName("BulkUpdateTenantConfigurations")
        .WithTags("TenantConfigurations");
    }
}
