namespace TokenIDP.Core.OAuth.Endpoints;

internal class TokenEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/token");

        authGroup.MapPost("", static async (HttpContext httpContext,
            TokenEndpointClientAuthService clientAuthenticationService,
            ITokenGrantUseCase accessTokenUseCase) =>
        {
            TokenRequest request;

            try
            {
                request = await clientAuthenticationService.BuildValidatedRequestAsync(httpContext);
            }
            catch (TokenRequestValidationException ex)
            {
                return TokenRequestValidationResultFactory.Create(ex);
            }

            string ipAddress = httpContext.Connection?.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;

            request.IpAddress = ipAddress;

            var result = await accessTokenUseCase.GetAccessToken(request);

            return result;
        })
        .WithName("AccessToken")
        .WithTags("AccessToken");
    }
}
