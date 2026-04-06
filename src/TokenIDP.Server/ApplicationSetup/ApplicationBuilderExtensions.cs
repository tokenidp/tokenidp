using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Endpoints;
using HealthChecks.UI.Client;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.Endpoints;
using TokenIDP.Core.Foundation.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Infrastructure;
using TokenIDP.Server.HealthChecks;
using TokenIDP.Server.Middlewares;
using TokenIDP.Server.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace TokenIDP.Server.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenIDPServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        string audience)
        => builder.AddTokenIDPServices(connectionStringName, audience, null);

    public static WebApplicationBuilder AddTokenIDPServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        string audience,
        Action<TokenOption>? configureToken)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException("Token audience is required.", nameof(audience));
        }

        ConfigureTokenOptions(builder.Services, builder.Configuration, options =>
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
                    "Token signing certificate is required in production. " +
                    "Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
            }

            if (string.Equals(options.Key, TokenKeyDefault.DevelopmentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Development signing key cannot be used in production.");
            }
        });

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

        builder.Services.AddScoped<LoadService>();

        builder.Services.AddIDPServices(builder.Configuration);

        builder.Services.AddAdminServices(builder.Configuration);

        builder.Services.AddInfrastructureServices(builder.Configuration, connectionStringName);

        builder.Services.AddAuthentication(builder.Configuration, builder.Environment);


        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        AddHealthChecks(builder, connectionStringName);

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, DynamicRolePolicyHandler>();

        return builder;
    }

    public static async Task<WebApplication> UseTokenIDPAsync(this WebApplication app, string connectionStringName = "")
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.RegisterIDPEndpoints();

        app.RegisterAdminEndpoints();

        await app.EnsureSystemBootstrap(connectionStringName);

        //app.MapHealthChecks("/health");

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }

    private static void AddHealthChecks(WebApplicationBuilder builder, string connectionStringName)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddCheck<AuthorizationEndpointHealthCheck>("authorization")
            .AddCheck<TokenEndpointHealthCheck>("token")
            .AddCheck<DatabaseHealthCheck>("database");
    }

    private static void ConfigureTokenOptions(
        IServiceCollection services,
        IConfiguration configuration,
        Action<TokenOption>? configureToken)
    {
        var tokenSection = configuration.GetSection("TokenOptions");

        if (tokenSection.Exists())
        {
            services.Configure<TokenOption>(tokenSection);
        }
        else
        {
            services.AddOptions<TokenOption>();
        }

        if (configureToken is not null)
        {
            services.PostConfigure(configureToken);
        }
    }
}


