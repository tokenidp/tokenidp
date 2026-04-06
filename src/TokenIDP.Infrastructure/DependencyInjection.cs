using TokenIDP.Core.Admin.Bootstrap;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;
using TokenIDP.Core.Foundation.Abstractions.Stores;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Infrastructure.Abstractions;
using TokenIDP.Infrastructure.Bootstrap;
using TokenIDP.Infrastructure.Emails;
using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Concrete;
using TokenIDP.Infrastructure.Emails.Primitives;
using TokenIDP.Infrastructure.ExternalProviders;
using TokenIDP.Infrastructure.Outbox;
using TokenIDP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TokenIDP.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        AddPersistence(services, configuration, connectionStringName);
        AddOutboxServices(services);
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
            DatabaseProviderResolver.Configure(
                options,
                configuration,
                connectionStringName));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddMemoryCache();
        services.AddCors();

        services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<JsonHelper>();

        var secretEncryptionOptions = configuration
            .GetSection("Security")
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

    private static void AddOutboxServices(IServiceCollection services)
    {
        services.AddScoped<IOutboxMapper, TokenOutboxMapper>();
        services.AddScoped<IOutboxMapper, UserOutboxMapper>();
        services.AddScoped<IOutboxMapperResolver, OutboxMapperResolver>();
        services.AddScoped<IOutboxConsumerRouter, OutboxConsumerRouter>();
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
        services.AddSingleton<OidcIdTokenValidator>();

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

                var connectionString = DatabaseProviderResolver.GetConnectionString(
                    configuration,
                    connectionStringName);
                var databaseProvider = DatabaseProviderResolver.ResolveProvider(configuration);

                await bootstrapper.BootstrapAsync(
                    CancellationToken.None,
                    databaseProvider,
                    connectionString);
            }
        }
    }
}

