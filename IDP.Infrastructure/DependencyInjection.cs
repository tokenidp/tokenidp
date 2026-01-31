using IDP.Core.OAuth;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Notifications;
using IDP.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        AddPersistence(services, configuration, connectionStringName);
        AddStores(services);
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

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();
        //.AddDefaultTokenProviders();

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
        services.AddScoped<ILookupStore, LookupStore>();
        services.AddScoped<IPreAuthorizationStore, PreAuthorizationStore>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
    }

    private static void AddEmailServices(IServiceCollection services)
    {
        services.AddScoped<SmtpEmail>();
        services.AddScoped<SendGridEmail>();
        services.AddScoped<EmailProviderFactory>();
        services.AddScoped<IEmailSetting, EmailSetting>();

        services.AddTransient<Func<EmailProviderType, IEmailNotification>>(serviceProvider => key =>
        {
            return key switch
            {
                EmailProviderType.SendGrid => serviceProvider.GetRequiredService<SendGridEmail>(),
                _ => serviceProvider.GetRequiredService<SmtpEmail>()
            };
        });
    }
}
