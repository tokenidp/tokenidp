using IDP.Core.TokenServices;

namespace IDP.Core.OAuthEndpoints;

internal class IntrospectionEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/introspect");

        authGroup.MapPost("/", async (IntrospectionRequest request,
            IAppLogger<IntrospectionEndpoint> _logger,
            IReferenceTokenValidator referenceTokenValidator) =>
        {
            _logger.LogInfo("Introspect process started.");

            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogWarning("Introspect called with invalid request");
                return Results.BadRequest("Invalid request.");
            }

            _logger.LogInfo("Introspect called for token (partial): {TokenPartial}",
                $"{request.Token?.Substring(request.Token.Length - 5, 5)}...");

            var response = await referenceTokenValidator.ValidateReferenceToken(request.Token);

            _logger.LogInfo("Introspect completed. Active: {IsActive}", response.Active);

            return Results.Ok(response);
        })
        .WithName("Introspection")
        .WithTags("Introspection");
    }
}
