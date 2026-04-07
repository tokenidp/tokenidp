using TokenIDP.Core.OAuth.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class IntrospectionEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/introspect")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            });

        authGroup.MapPost("", async (IntrospectionRequest request,
            IAppLogger<IntrospectionEndpoint> _logger,
            IntrospectionUseCase useCase) =>
        {
            _logger.LogInfo("Introspect process started.");

            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogWarning("Introspect called with invalid request");
                return Results.BadRequest("Invalid request.");
            }

            _logger.LogInfo("Introspect called for token (partial): {TokenPartial}",
                $"{request.Token?.Substring(request.Token.Length - 5, 5)}...");

            var response = await useCase.ValidateReferenceToken(request.Token!);

            _logger.LogInfo("Introspect completed. Active: {IsActive}", response.Active);

            return Results.Ok(ApiResult<IntrospectionResponse>.Success(response));
        })
        .WithName("Introspection")
        .WithTags("Introspection");
    }
}

