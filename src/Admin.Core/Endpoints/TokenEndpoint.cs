using Admin.Core.Tokens;
using Admin.Core.Tokens.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Core.Endpoints;

internal class TokenEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/token")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
             TokenQueryUseCase useCase) =>
        {
            var response = await useCase.GetTokens(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tokens.view"
         })
         .WithName("Tokens")
         .WithTags("Tokens");

        authGroup.MapGet("/lookups", async (TokenLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TokenLookups")
        .WithTags("Tokens");

        authGroup.MapGet("/{id}", async (Guid id,
            TokenQueryUseCase useCase) =>
        {
            if (string.IsNullOrEmpty(id.ToString()))
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetTokenById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "tokens.view"
        })
        .WithName("TokenById")
        .WithTags("Tokens");

        authGroup.MapPost("/{id}/revoke", async (Guid id,
            [FromBody] TokenRevokeRequest request,
            TokenCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await useCase.RevokeToken(id, ipAddress, request?.Reason, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "tokens.delete"
        })
        .WithName("TokenRevoke")
        .WithTags("Tokens");

        authGroup.MapPost("/{id}/expire", async (Guid id,
            TokenCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await useCase.ExpireToken(id, ipAddress, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "tokens.delete"
         })
        .WithName("TokenExpire")
        .WithTags("Tokens");
    }
}