using IDP.Common.Options;
using IDP.Core.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IDP.Core.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenTresorServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        string audience)
        => AddTokenTresorServices(builder, connectionStringName, audience, null);

    public static WebApplicationBuilder AddTokenTresorServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        string audience,
        Action<TokenOption>? configureToken)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException("Token audience is required.", nameof(audience));
        }

        builder.Services.AddServices(builder.Configuration, builder.Environment, options =>
        {
            options.Audience = audience;

            if (builder.Environment.IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(options.Key) &&
                    string.IsNullOrWhiteSpace(options.KeyPath) &&
                    string.IsNullOrWhiteSpace(options.CertificateThumbprint) &&
                    string.IsNullOrWhiteSpace(options.CertificateSubjectName))
                {
                    options.Key = TokenKeyDefault.DevelopmentKey;
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(options.CertificateThumbprint) &&
                string.IsNullOrWhiteSpace(options.CertificateSubjectName))
            {
                throw new InvalidOperationException(
                    "Token signing certificate is required in production. Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
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
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.RegisterEndpointDefinitions();

        return app;
    }
}