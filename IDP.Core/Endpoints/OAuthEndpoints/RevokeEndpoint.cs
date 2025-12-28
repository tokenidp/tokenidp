using IDP.Core.OAuth;
using Microsoft.AspNetCore.Mvc;

namespace IDP.Core.Endpoints.OAuthEndpoints;

internal class RevokeEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/revoke");

        authGroup.MapDelete("/", static async (HttpContext httpContext,
        [FromBody] RevokeTokenRequest request,
        IAppLogger<RevokeEndpoint> _logger,
        RevokeTokenService revokeTokenService) =>
        {
            string ipAddress = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            request.IpAddress = ipAddress;

            _logger.LogInfo("RevokeToken called from IP: {IP}, Reason: {Token}",
                ipAddress, request.RefreshToken);

            await revokeTokenService.RevokeToken(request);

            _logger.LogInfo("Refresh token revoked for IP: {IP}", ipAddress);

            return Results.Ok(new { message = "Refresh token revoked." });
        })
        .WithName("RevokeToken")
        .WithTags("RevokeToken");
    }
}