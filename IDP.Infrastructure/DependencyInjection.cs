using Admin.Core.Bootstrap;
using AspNet.Security.OAuth.GitHub;
using IDP.Core.Model;
using IDP.Core.OAuth;
using IDP.Domain.AggregateRoots.Configurations;
using IDP.ExternalProviders.Abstractions;
using IDP.Foundation.Abstractions.Stores;
using IDP.Foundation.Options;
using IDP.Foundation.Security;
using IDP.Infrastructure.Bootstrap;
using IDP.Infrastructure.Emails;
using IDP.Infrastructure.Emails.Abstractions;
using IDP.Infrastructure.Emails.Concrete;
using IDP.Infrastructure.Emails.Primitives;
using IDP.Infrastructure.ExternalProviders;
using IDP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IDP.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        AddPersistence(services, configuration, connectionStringName);
        AddStores(services);
        AddExternalProviders(services, configuration);
        AddEmailServices(services);
        AddBootstrapServices(services, configuration);
    }

    private static void AddPersistence(IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(connectionStringName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddMemoryCache();
        services.AddCors();

        services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<JsonHelper>();

        var secretEncryptionOptions = configuration
            .GetSection(SecretEncryptionOptions.SectionName)
            .Get<SecretEncryptionOptions>() ?? new SecretEncryptionOptions();

        if (string.IsNullOrWhiteSpace(secretEncryptionOptions.KeyBase64))
        {
            secretEncryptionOptions.KeyBase64 =
                Environment.GetEnvironmentVariable("IDP_SECRET_ENCRYPTION_KEY") ?? string.Empty;
        }

        services.AddSingleton(secretEncryptionOptions);
        services.AddSingleton<ISecretProtector>(_ =>
            new AesGcmSecretProtector(
                secretEncryptionOptions.KeyBase64,
                secretEncryptionOptions.KeyId));
    }

    private static void AddStores(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationStore, AuthorizationStore>();
        services.AddScoped<IClientStore, ClientStore>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IRoleStore, RoleStore>();
        services.AddScoped<IConfigurationStore, ConfigurationStore>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        services.AddScoped<ICodeSequenceGenerator, CodeSequenceGenerator>();
        services.AddScoped<IUserStore, UserStore>();
    }

    private static void AddExternalProviders(IServiceCollection services,
       IConfiguration configuration)
    {
        services.AddHttpClient();

        services.Configure<ExternalAuthOptions>(
            configuration.GetSection(ExternalAuthOptions.SectionName));

        services.AddScoped<ExternalProviderConfigurationResolver>();

        services.AddSingleton<IConfigureOptions<GoogleOptions>, DynamicGoogleOptionsSetup>();
        services.AddSingleton<IConfigureOptions<MicrosoftAccountOptions>, DynamicMicrosoftOptionsSetup>();
        services.AddSingleton<IConfigureOptions<GitHubAuthenticationOptions>, DynamicGitHubOptionsSetup>();

        services.AddScoped<IExternalAuthSessionStore, ExternalAuthSessionStore>();
        services.AddScoped<IExternalIdentityLinkService, ExternalIdentityLinkService>();
        services.AddScoped<IUserSignInService, UserSignInService>();

        services.AddScoped<IExternalProviderClient, GoogleExternalProviderClient>();
        services.AddScoped<IExternalProviderClient, MicrosoftExternalProviderClient>();
        services.AddScoped<IExternalProviderClient, GitHubExternalProviderClient>();
        services.AddScoped<IExternalProviderFactory, ExternalProviderFactory>();
    }

    private static void AddEmailServices(IServiceCollection services)
    {
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<SendGridEmailSender>();
        services.AddScoped<EmailProviderFactory>();
        services.AddScoped<EmailConfigurationProvider>();
        services.AddScoped<IEmailQueueStore, EmailQueueStore>();
        services.AddScoped<IRetrySchedule, ExponentialRetrySchedule>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

        services.AddTransient<Func<EmailProviderType, IEmailSender>>(serviceProvider => key =>
        {
            return key switch
            {
                EmailProviderType.SendGrid => serviceProvider.GetRequiredService<SendGridEmailSender>(),
                _ => serviceProvider.GetRequiredService<SmtpEmailSender>()
            };
        });
    }

    private static void AddBootstrapServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BootstrapOption>(configuration.GetSection("Bootstrap"));

        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IClientProvisioningService, ClientProvisioningService>();
        services.AddScoped<IRoleProvisioningService, RoleProvisioningService>();
        services.AddScoped<IPermissionSeeder, PermissionSeeder>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IConfigurationSeeder, ConfigurationSeeder>();

        services.AddScoped<ISystemBootstrapper, SystemBootstrapper>();
    }

    public static async Task EnsureSystemBootstrap(this WebApplication app, string connectionStringName)
    {
        var opts = app.Services.GetRequiredService<IOptions<BootstrapOption>>().Value;

        if (!app.Environment.IsProduction() && opts.Enable)
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync();

                var bootstrapper = scope.ServiceProvider.GetRequiredService<ISystemBootstrapper>();

                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var connectionString = configuration.GetConnectionString(connectionStringName);

                await bootstrapper.BootstrapAsync(CancellationToken.None, connectionString!);
            }
        }
    }
}
