using TokenIDP.Core.Admin.ApiResources;
using TokenIDP.Core.Admin.ApiResources.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Core.Admin.Endpoints;

internal sealed class ApiResourceEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("apiresources")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("", async (ApiResourceQueryUseCase useCase, HttpContext httpContext) =>
        {
            var response = await useCase.GetAllAsync(httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute { Policy = "apiresources.view" })
        .WithName("ApiResources")
        .WithTags("ApiResources");

        authGroup.MapGet("/{id:guid}", async (Guid id, ApiResourceQueryUseCase useCase, HttpContext httpContext) =>
        {
            var response = await useCase.GetByIdAsync(id, httpContext.RequestAborted);
            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute { Policy = "apiresources.view" })
        .WithName("ApiResourceById")
        .WithTags("ApiResources");

        authGroup.MapPost("/", async (CreateUpdateApiResource request, ApiResourceCommandUseCase useCase, HttpContext httpContext) =>
        {
            var response = await useCase.CreateAsync(request, httpContext.RequestAborted);
            var location = response.IsSuccess ? $"/api/apiresources/{response.Value}" : string.Empty;
            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .RequireAuthorization(new AuthorizeAttribute { Policy = "apiresources.add" })
        .WithName("CreateApiResource")
        .WithTags("ApiResources");

        authGroup.MapPut("/{id:guid}", async (Guid id, CreateUpdateApiResource request, ApiResourceCommandUseCase useCase, HttpContext httpContext) =>
        {
            if (id != request.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await useCase.UpdateAsync(id, request, httpContext.RequestAborted);
            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute { Policy = "apiresources.edit" })
        .WithName("UpdateApiResource")
        .WithTags("ApiResources");

        authGroup.MapDelete("/{id:guid}", async (Guid id, ApiResourceCommandUseCase useCase, HttpContext httpContext) =>
        {
            var response = await useCase.DeleteAsync(id, httpContext.RequestAborted);
            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute { Policy = "apiresources.delete" })
        .WithName("DeleteApiResource")
        .WithTags("ApiResources");
    }
}
