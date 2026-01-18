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
            ConfigurationUseCases configurationService) =>
        {
            var response = await configurationService.GetConfigurations(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Configurations")
        .WithTags("Configurations");

        authGroup.MapGet("/{id}", async (int id,
            ConfigurationUseCases configurationService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await configurationService.GerConfigurationById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("ConfigById")
        .WithTags("ConfigById");

        authGroup.MapPost("/", async (CreateUpdateConfiguration configuration,
            ConfigurationUseCases configurationService) =>
        {
            var response = await configurationService.CreateConfiguration(configuration);

            var location = response.IsSuccess ? $"configuration/{configuration.ConfigKey}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateConfig")
        .WithTags("CreateConfig");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateConfiguration configuration,
            ConfigurationUseCases configurationService) =>
        {
            if (id != configuration.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await configurationService.UpdateConfiguration(id, configuration);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateConfig")
        .WithTags("UpdateConfig");
    }
}
