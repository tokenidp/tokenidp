using TokenIDP.Core.OAuth.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class RevokeEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/revoke")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            });

        authGroup.MapDelete("", static async (HttpContext httpContext,
            [FromBody] RevokeTokenRequest request,
            IAppLogger<RevokeEndpoint> _logger,
            RevokeTokenUseCase revokeTokenService) =>
        {
            string ipAddress = httpContext.Connection?.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;

            request.IpAddress = ipAddress;

            _logger.LogInfo("RevokeToken called from IP: {IP}, Reason: {Token}",
                ipAddress, request.Token);

            await revokeTokenService.RevokeToken(request);

            _logger.LogInfo("Refresh token revoked for IP: {IP}", ipAddress);

            return Results.Ok(new { message = "Refresh token revoked." });
        })
        .WithName("RevokeToken")
        .WithTags("RevokeToken");
    }
}
