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
            GetClientUseCase useCase) =>
        {
            var response = await useCase.GetClients(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("Clients")
        .WithTags("Clients");

        authGroup.MapGet("/{id}", async (int id,
            GetClientUseCase useCase) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetClientById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("ClientById")
        .WithTags("ClientById");

        authGroup.MapGet("clientlookups", async (GetClientLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetClientLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("ClientLookups")
        .WithTags("ClientLookups");

        authGroup.MapPost("/", async (CreateUpdateClient client,
            CreateUpdateClientUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.CreateClient(client, httpContext.RequestAborted);

            var location = response.IsSuccess ? $"client/{response.Value}" : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreateClient")
        .WithTags("CreateClient");

        authGroup.MapPut("/{id}", async (int id, CreateUpdateClient client,
            CreateUpdateClientUseCase useCase,
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
        .WithName("UpdateClient")
        .WithTags("UpdateClient");

        authGroup.MapDelete("/{id}", async (int id,
            CreateUpdateClientUseCase useCase,
            HttpContext httpContext) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.DeleteClient(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("DeleteClient")
        .WithTags("DeleteClient");
    }
}