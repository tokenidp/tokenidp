using Admin.Core.OAuthEndpoints;
using Admin.Core.Options;
using IDP.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Admin.Core.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenTresorServices(
        this WebApplicationBuilder builder,
        string connectionStringName)
        => AddTokenTresorServices(builder, connectionStringName, null);

    public static WebApplicationBuilder AddTokenTresorServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        Action<TokenOption>? configureToken)
    {
        builder.Services.AddServices(builder.Configuration, options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(options.Key) && string.IsNullOrWhiteSpace(options.KeyPath))
                {
                    options.Key = TokenKeyDefault.DevelopmentKey;
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Key) && string.IsNullOrWhiteSpace(options.KeyPath))
            {
                throw new InvalidOperationException(
                    "Token signing key is required in production. Provide TokenSettings:KeyPath or TokenSettings:Key.");
            }

            if (string.Equals(options.Key, TokenKeyDefault.DevelopmentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Development signing key cannot be used in production.");
            }
        });

        if (configureToken is not null)
        {
            builder.Services.PostConfigure(configureToken);
        }

        builder.Services.AddPersistence(builder.Configuration, connectionStringName);

        return builder;
    }

    public static WebApplication UseTokenTresor(this WebApplication app)
    {
        app.RegisterEndpointDefinitions();

        return app;
    }
}