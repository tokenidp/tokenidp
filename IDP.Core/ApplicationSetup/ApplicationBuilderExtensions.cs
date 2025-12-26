using IDP.Core.OAuthEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IDP.Core.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenTresorServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddServices(builder.Configuration, options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(options.Key) && string.IsNullOrWhiteSpace(options.KeyPath))
                {
                    options.Key = TokenKeyDefaults.DevelopmentKey;
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Key) && string.IsNullOrWhiteSpace(options.KeyPath))
            {
                throw new InvalidOperationException(
                    "Token signing key is required in production. Provide TokenSettings:KeyPath or TokenSettings:Key.");
            }

            if (string.Equals(options.Key, TokenKeyDefaults.DevelopmentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Development signing key cannot be used in production.");
            }
        });

        builder.Services.AddPersistence(builder.Configuration);

        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    public static WebApplication UseTokenTresor(this WebApplication app)
    {
        app.RegisterEndpointDefinitions();

        return app;
    }
}