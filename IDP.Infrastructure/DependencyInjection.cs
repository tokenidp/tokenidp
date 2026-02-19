using Admin.Core.Bootstrap;
using Admin.Core.Users;
using IDP.Core.OAuth;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Bootstrap;
using IDP.Infrastructure.Emails;
using IDP.Infrastructure.Emails.Abstractions;
using IDP.Infrastructure.Emails.Concrete;
using IDP.Infrastructure.Emails.Primitives;
using IDP.Infrastructure.Persistence;
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
        AddBootstrapServices(services, configuration);
        AddEmailServices(services);
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
    }

    private static void AddStores(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationCodeStore, AuthorizationCodeStore>();
        services.AddScoped<IClientStore, ClientStore>();
        services.AddScoped<IIdentityStore, IdentityStore>();
        services.AddScoped<IRoleStore, RoleStore>();
        services.AddScoped<IConfigurationStore, ConfigurationStore>();
        services.AddScoped<IPreAuthorizationStore, PreAuthorizationStore>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        services.AddScoped<IUserCodeGenerator, UserCodeGenerator>();
    }

    private static void AddBootstrapServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BootstrapOptions>(configuration.GetSection("Bootstrap"));

        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IClientProvisioningService, ClientProvisioningService>();
        services.AddScoped<IRoleProvisioningService, RoleProvisioningService>();
        services.AddScoped<IPermissionSeeder, PermissionSeeder>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IConfigurationSeeder, ConfigurationSeeder>();

        services.AddScoped<ISystemBootstrapper, SystemBootstrapper>();
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

    public static async Task EnsureSystemBootstrap(this WebApplication app, string connectionStringName)
    {
        var opts = app.Services.GetRequiredService<IOptions<BootstrapOptions>>().Value;

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
