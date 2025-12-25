using IDP.Core.Admin;
using IDP.Core.Admin.Configurations;

namespace IDP.Core.AdminEndpoints;

internal class ConfigurationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/configuration");

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<ConfigurationEndpoint> _logger,
            ConfigurationService configurationService) =>
        {
            var response = await configurationService.GetConfigurations(data);

            return Results.Ok(response);
        })
        .WithName("Configurations")
        .WithTags("Configurations");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<ConfigurationEndpoint> _logger,
            ConfigurationService configurationService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiError.Failure("Record Id should be greater than zero."));
            }

            var response = await configurationService.GerConfigurationById(id);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("ConfigById")
        .WithTags("ConfigById");

        authGroup.MapPost("/", async (CreateUpdateConfiguration configuration,
            IAppLogger<ConfigurationEndpoint> _logger,
            ConfigurationService configurationService) =>
        {
            var response = await configurationService.CreateConfiguration(configuration);

            return Results.Created($"configuration/{configuration.ConfigKey}", Result.Success(response.Id));
        })
        .WithName("CreateConfig")
        .WithTags("CreateConfig");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateConfiguration configuration,
            IAppLogger<ConfigurationEndpoint> _logger,
            ConfigurationService configurationService) =>
        { 
            if (id != configuration.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await configurationService.UpdateConfiguration(id, configuration);

            return Results.NoContent();
        })
        .WithName("UpdateConfig")
        .WithTags("UpdateConfig");
    }
}
