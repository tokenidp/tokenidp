using IDP.Core.OAuth.Model;
using IDP.Core.TokenServices;
using Microsoft.AspNetCore.Mvc;

namespace IDP.Core.OAuthEndpoints;

internal class RefreshTokenEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/refresh-token");

        authGroup.MapPost("/", static async (HttpContext httpContext,
            [FromBody] RefreshTokenRequest request,
            IAppLogger<RefreshTokenEndpoint> _logger,
            RefreshTokenService refreshTokenService) =>
        {
            string ipAddress = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            _logger.LogInfo("GetRefreshToken called from IP: {IP}", ipAddress);

            var response = await refreshTokenService.GenerateRefreshToken(request.RefreshToken,
                request.ClientId, ipAddress);

            _logger.LogInfo("Refresh token generated for ClientId: {ClientId}", request.ClientId);

            return Results.Ok(response);
        })
        .WithName("RefreshToken")
        .WithTags("RefreshToken");

        authGroup.MapDelete("/", static async (HttpContext httpContext,
        [FromBody] RevokeTokenRequest request,
        IAppLogger<RefreshTokenEndpoint> _logger,
        RefreshTokenService refreshTokenService) =>
        {
            string ipAddress = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            _logger.LogInfo("RevokeToken called from IP: {IP}, Reason: {Reason}", ipAddress, request.ReasonRevoked);

            await refreshTokenService.RevokeRefreshToken(request.RefreshToken, ipAddress, request.ReasonRevoked);

            _logger.LogInfo("Refresh token revoked for IP: {IP}", ipAddress);

            return Results.Ok(new { message = "Refresh token revoked." });
        })
        .WithName("RefreshTokenRevoked")
        .WithTags("RefreshTokenRevoked");
    }
}
