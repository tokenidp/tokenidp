namespace IDP.Core.Endpoints;

internal class TokenEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/token");

        authGroup.MapPost("", static async (HttpContext httpContext,
            TokenRequest request,
            ITokenGrantUseCase accessTokenUseCase) =>
        {
            string ipAddress = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            request.IpAddress = ipAddress;

            var result = await accessTokenUseCase.GetAccessToken(request);

            return result;
        })
        .WithName("AccessToken")
        .WithTags("AccessToken");
    }
}
