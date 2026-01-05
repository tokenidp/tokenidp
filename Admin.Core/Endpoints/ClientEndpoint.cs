using Admin.Core.Clients;

namespace Admin.Core.Endpoints;

internal class ClientEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/client")
            .RequireAuthorization()
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            IAppLogger<ClientEndpoint> _logger,
            ClientService clientService) =>
        {
            var response = await clientService.GetClients(data);

            return Results.Ok(response);
        })
        .WithName("Clients")
        .WithTags("Clients");

        authGroup.MapGet("/{id}", async (int id,
            IAppLogger<ClientEndpoint> _logger,
            ClientService clientService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiError.Failure("Record Id should be greater than zero."));
            }

            var response = await clientService.GetClientById(id);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("ClientById")
        .WithTags("ClientById");

        authGroup.MapPost("/", async (CreateUpdateClient client,
            IAppLogger<ClientEndpoint> _logger,
            ClientService clientService) =>
        {
            var response = await clientService.CreateClient(client);

            return Results.Created($"client/{response.Id}", Result.Success(response.Id));
        })
        .WithName("CreateClient")
        .WithTags("CreateClient");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateClient client,
            IAppLogger<ClientEndpoint> _logger,
            ClientService clientService) =>
        {
            if (id != client.Id)
            {
                return Results.BadRequest(ApiError.Failure("Record Ids didn't match."));
            }

            await clientService.UpdateClient(id, client);

            return Results.NoContent();
        })
        .WithName("UpdateClient")
        .WithTags("UpdateClient");
    }
}
