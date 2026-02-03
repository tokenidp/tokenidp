using IDP.Core.UseCases;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IDP.Core;

public sealed class TokenEndpointHealthCheck : IHealthCheck
{
    private readonly ITokenGrantUseCase _tokenService;

    public TokenEndpointHealthCheck(
        ITokenGrantUseCase tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            TokenRequest request = new() { GrantType = "authorization_code", ClientId = "sfsfsfdsfs" };

            await _tokenService.GetAccessToken(request);

            return HealthCheckResult.Healthy(
                "Token endpoint dependencies are healthy");
        }
        catch (NotFoundException)
        {
            return await Task.FromResult(
              HealthCheckResult.Healthy("Token endpoint reachable (validation failed as expected)"));
        }
        catch (Exception)
        {
            return await Task.FromResult(
                HealthCheckResult.Unhealthy("Authorization endpoint broken"));
        }
    }
}