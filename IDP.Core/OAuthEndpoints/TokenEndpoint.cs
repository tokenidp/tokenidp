using IDP.Core.OAuth.Model;
using IDP.Core.TokenServices.UseCases;

namespace IDP.Core.OAuthEndpoints;

internal class TokenEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/token");

        authGroup.MapPost("/", static async (HttpContext httpContext,
            TokenRequest request,
            AccessTokenUseCase accessTokenUseCase) =>
        {
            string ipAddress = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            var result = await accessTokenUseCase.GetAccessToken(request, ipAddress);

            return result;
        })
        .WithName("AccessToken")
        .WithTags("AccessToken");
    }
}
