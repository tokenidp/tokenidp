using IDP.Core.OAuthEndpoints;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Core.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenVaultServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddServices(builder.Configuration);
        builder.Services.AddPersistence(builder.Configuration);

        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    public static WebApplication UseTokenVault(this WebApplication app)
    {
        app.RegisterEndpointDefinitions();
        return app;
    }
}
