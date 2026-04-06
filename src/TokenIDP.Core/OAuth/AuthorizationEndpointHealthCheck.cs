using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TokenIDP.Core.OAuth;

public sealed class AuthorizationEndpointHealthCheck : IHealthCheck
{
    private readonly IAuthorizationRequestValidator _validator;

    public AuthorizationEndpointHealthCheck(
        IAuthorizationRequestValidator validator)
    {
        _validator = validator;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AuthorizationRequest
            {
                ClientId = "__healthcheck__",
                ResponseType = "code",
                RedirectUri = "https://localhost/health",
                Scopes = "openid"
            };

            // We EXPECT this to fail validation,
            // but the pipeline must execute
            await _validator.ValidateAsync(request, cancellationToken);

            return await Task.FromResult(
                HealthCheckResult.Healthy("Authorization endpoint pipeline is active"));
        }
        catch (AuthorizationRequestException)
        {
            // Expected ? means pipeline is alive
            return await Task.FromResult(
                HealthCheckResult.Healthy("Authorization endpoint reachable (validation failed as expected)"));
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
