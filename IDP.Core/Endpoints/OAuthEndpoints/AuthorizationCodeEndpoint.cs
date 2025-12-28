using IDP.Core.OAuth;

namespace IDP.Core.Endpoints.OAuthEndpoints;

internal class AuthorizationCodeEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth");

        authGroup.MapPost("/login", async (AuthRequest request,
            IAppLogger<AuthorizationCodeEndpoint> _logger,
            AuthorizationCodeUseCase authenticationUseCase) =>
        {
            _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

            var response = await authenticationUseCase.Authenticate(request);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return response;
        }).WithName("Authenticate")
        .WithTags("Authentication");
    }
}
