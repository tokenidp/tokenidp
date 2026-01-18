using Admin.Core.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

internal class ClientEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/client")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            ClientUseCases clientService) =>
        {
            var response = await clientService.GetClients(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Clients")
        .WithTags("Clients");

        authGroup.MapGet("/{id}", async (int id,
            ClientUseCases clientService) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await clientService.GetClientById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("ClientById")
        .WithTags("ClientById");

        authGroup.MapPost("/", async (CreateUpdateClient client,
            ClientUseCases clientService) =>
        {
            var response = await clientService.CreateClient(client);

            var location = response.IsSuccess ? $"client/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateClient")
        .WithTags("CreateClient");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateClient client,
            ClientUseCases clientService) =>
        {
            if (id != client.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await clientService.UpdateClient(id, client);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("UpdateClient")
        .WithTags("UpdateClient");
    }
}
