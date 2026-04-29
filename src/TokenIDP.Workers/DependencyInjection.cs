using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TokenIDP.Infrastructure.Outbox.Abstractions;
using TokenIDP.Workers.HealthChecks.States;
using TokenIDP.Workers.Mappers;
using TokenIDP.Workers.Projectors;
using TokenIDP.Workers.Workers;

namespace TokenIDP.Workers;

public static class DependencyInjection
{
    public static void AddProjectionServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOutboxMapper, TokenOutboxMapper>();
        services.AddScoped<IOutboxMapper, UserOutboxMapper>();
        services.AddScoped<IOutboxMapper, TenantOutboxMapper>();
        services.AddScoped<IOutboxMapperResolver, OutboxMapperResolver>();
        services.AddScoped<IOutboxConsumerRouter, OutboxConsumerRouter>();

        services.AddScoped<TokenReadModelProjector>();
        services.AddScoped<ActivityProjector>();

        services.AddHostedService<TokenOutboxWorker>();
        services.AddHostedService<ActivityOutboxWorker>();
        services.AddHostedService<DashboardMetricsWorker>();
        services.AddHostedService<EmailDispatcherWorker>();

        services.AddSingleton<TokenWorkerState>();
        services.AddSingleton<ActivityWorkerState>();
    }
}

