using IDP.Core.Admin.Clients;

namespace IDP.Core.AdminEndpoints;

internal class ClientEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/client");

        authGroup.MapGet("/{clientId}", async (string clientId,
            IAppLogger<ClientEndpoint> _logger,
            ClientService clientService) =>
        {
            _logger.LogInfo("IsValidClient called for clientId: {ClientId}", clientId);

            var response = await clientService.GetClientScopes(clientId);

            _logger.LogInfo("IsValidClient result for clientId: {ClientId} is {Result}", clientId, response);

            return Results.Ok(ApiResult<ClientValidationResult>.Success(response));
        })
        .WithName("Client")
        .WithTags("Client");
    }
}
