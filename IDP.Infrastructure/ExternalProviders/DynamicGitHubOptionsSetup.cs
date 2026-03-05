using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IDP.Infrastructure.ExternalProviders;

public sealed class DynamicGitHubOptionsSetup : IConfigureNamedOptions<GitHubAuthenticationOptions>
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ISecretProtector _encryptionService;
    private readonly IServiceProvider _serviceProvider;

    public DynamicGitHubOptionsSetup(ITenantContextAccessor tenantAccessor,
        ISecretProtector encryptionService,
        IServiceProvider serviceProvider)
    {
        _tenantAccessor = tenantAccessor;
        _encryptionService = encryptionService;
        _serviceProvider = serviceProvider;
    }

    public void Configure(string? name, GitHubAuthenticationOptions options)
    {
        // handled dynamically
    }

    public void Configure(GitHubAuthenticationOptions options)
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
                ExternalProviderTypes.GitHub);

            if (config == null)
                throw new Exception("GitHub provider not configured.");

            var decryptedSecret = _encryptionService.Decrypt(
                config.OidcConfig?.ClientSecret,
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