using IDP.Foundation.Abstractions;
using IDP.Foundation.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Core.Abstractions;
using Notification.Core.Concrete;
using Notification.Core.Primitives;
using Notification.Core.Worker;

namespace Notification.Core;

public static class DependencyInjection
{
    public static void AddNotificationServices(this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        services.AddDbContext<NotificationDbContext>(options =>
          options.UseSqlServer(
              configuration.GetConnectionString(connectionStringName)));

        AddEmailServices(services);

        services.AddHostedService<EmailDispatcherWorker>();
    }

    private static void AddEmailServices(IServiceCollection services)
    {
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<SendGridEmailSender>();
        services.AddScoped<EmailProviderFactory>();
        services.AddScoped<IEmailConfigurationProvider, EmailConfigurationProvider>();
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
}
