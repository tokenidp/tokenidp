using IDP.Infrastructure.Abstractions;
using IDP.Projection.Mappers;
using IDP.Projection.Projectors;
using IDP.Projection.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Projection;

public static class DependencyInjection
{
    public static void AddProjectionServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOutboxMapper, TokenOutboxMapper>();
        services.AddScoped<IOutboxMapper, UserOutboxMapper>();
        services.AddScoped<IOutboxMapperResolver, OutboxMapperResolver>();
        services.AddScoped<IOutboxConsumerRouter, OutboxConsumerRouter>();

        services.AddScoped<TokenReadModelProjector>();
        services.AddScoped<ActivityProjector>();

        services.AddHostedService<TokenOutboxWorker>();
        services.AddHostedService<ActivityOutboxWorker>();
    }
}
