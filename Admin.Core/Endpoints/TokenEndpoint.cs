using Admin.Core.Tokens;
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
             GetTokenUseCase useCase) =>
        {
            var response = await useCase.GetTokens(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
         .WithName("Tokens")
         .WithTags("Tokens");

        authGroup.MapGet("/lookups", async (GetTokenLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TokenLookups")
        .WithTags("TokenLookups");

        var tokensGroup = app.MapGroup("/admin/tokens")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        tokensGroup.MapGet("/{id}", async (int id,
            GetTokenUseCase useCase) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await useCase.GetTokenById(id);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("TokenById")
        .WithTags("TokenById");

        tokensGroup.MapPost("/{id}/revoke", async (int id,
            [FromBody] TokenRevokeRequest request,
            TokenCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await useCase.RevokeToken(id, ipAddress, request?.Reason, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("TokenRevoke")
        .WithTags("TokenRevoke");

        tokensGroup.MapPost("/{id}/expire", async (int id,
            TokenCommandUseCase useCase,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await useCase.ExpireToken(id, ipAddress, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .WithName("TokenExpire")
        .WithTags("TokenExpire");
    }
}