using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IDP.Infrastructure.ExternalProviders;

public sealed class DynamicGoogleOptionsSetup : IConfigureNamedOptions<GoogleOptions>
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ISecretProtector _encryptionService;
    private readonly IServiceProvider _serviceProvider;

    public DynamicGoogleOptionsSetup(ITenantContextAccessor tenantAccessor,
        ISecretProtector encryptionService,
        IServiceProvider serviceProvider)
    {
        _tenantAccessor = tenantAccessor;
        _encryptionService = encryptionService;
        _serviceProvider = serviceProvider;
    }

    public void Configure(string? name, GoogleOptions options)
    {
        // handled in async event
    }

    public void Configure(GoogleOptions options)
    {
        options.Events ??= new OAuthEvents();

        options.Events.OnRedirectToAuthorizationEndpoint = async context =>
        {
            var tenantId = _tenantAccessor.TenantId;
            var clientId = _tenantAccessor.ClientId;

            using var scope = _serviceProvider.CreateScope();

            var resolver = scope.ServiceProvider
                .GetRequiredService<ExternalProviderConfigurationResolver>();

            var config = await resolver.ResolveAsync(
                tenantId,
                clientId,
                ExternalProviderTypes.Google);

            if (config == null)
                throw new Exception("Google not configured for tenant.");

            var decryptedSecret = _encryptionService.Decrypt(config.OidcConfig?.ClientSecret,
                BuildSecretContext(tenantId.ToString(), config.ProviderType));

            context.Options.ClientId = config.OidcConfig?.ClientId ?? string.Empty;
            context.Options.ClientSecret = decryptedSecret!;
            context.Options.CallbackPath = config.OidcConfig?.CallbackPath;

            context.Response.Redirect(context.RedirectUri);
        };
    }

    private static string BuildSecretContext(string tenantId, ExternalProviderTypes providerType)
    {
        return $"tenant:{tenantId}:provider:{providerType}";
    }
}
