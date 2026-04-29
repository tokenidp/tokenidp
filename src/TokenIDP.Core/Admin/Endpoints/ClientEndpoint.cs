using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Clients.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

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
            ClientQueryUseCase useCase) =>
        {
            var response = await useCase.GetClients(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "applications.view"
        })
        .WithName("Clients")
        .WithTags("Clients");

        authGroup.MapGet("/{id}", async (int id,
            ClientQueryUseCase useCase) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetClientById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "applications.view"
        })
        .WithName("ClientById")
        .WithTags("Clients");

        authGroup.MapGet("clientlookups", async (ClientLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetClientLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("ClientLookups")
        .WithTags("Clients");

        authGroup.MapPost("/", async (CreateUpdateClient client,
            ClientCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateClient(client, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"client/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "applications.add"
        })
        .WithName("CreateClient")
        .WithTags("Clients");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateClient client,
            ClientCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id != client.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await useCase.UpdateClient(id, client, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "applications.edit"
        })
        .WithName("UpdateClient")
        .WithTags("Clients");

        authGroup.MapPost("/{id}/regenerate-secret", async (int id,
            RotateClientSecretRequest request,
            ClientCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.RotateClientSecret(id, request, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "applications.edit"
        })
        .WithName("RegenerateClientSecret")
        .WithTags("Clients");
    }
}
