using Admin.Core;
using Admin.Core.Endpoints;
using HealthChecks.UI.Client;
using IDP.Core;
using IDP.Core.Endpoints;
using IDP.Foundation.Abstractions;
using IDP.Foundation.Options;
using IDP.Foundation.Primitives;
using IDP.Infrastructure;
using IDP.Projection;
using IDP.Projection.HealthChecks;
using IDP.Server.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace IDP.Server.ApplicationSetup;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTokenTresorServices(
        this WebApplicationBuilder builder,
        string connectionStringName,
        string audience)
        => builder.AddTokenTresorServices(connectionStringName, audience, null);

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

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        builder.Services.AddScoped<LoadService>();

        //builder.Services.AddHttpClient("IDPClient", (serviceProvider, client) =>
        //{
        //    var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenOption>>().Value;
        //    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        //    var issuer = ResolveIssuer(tokenOptions, httpContextAccessor);
        //    client.BaseAddress = new Uri(issuer);
        //});
        //services.AddScoped<AuthenticationService>();

        builder.Services.AddIDPServices(builder.Configuration);

        builder.Services.AddAdminServices(builder.Configuration);

        builder.Services.AddInfrastructureServices(builder.Configuration, connectionStringName);

        builder.Services.AddAuthentication(builder.Configuration, builder.Environment);

        builder.Services.AddProjectionServices(builder.Configuration);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        AddHealthChecks(builder, connectionStringName);

        return builder;
    }

    public static WebApplication UseTokenTresor(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.RegisterIDPEndpoints();

        app.RegisterAdminEndpoints();

        app.MapHealthChecks("/health");

        //app.MapHealthChecks("/health", new HealthCheckOptions
        //{
        //    Predicate = _ => true,
        //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        //});

        return app;
    }

    private static void AddHealthChecks(WebApplicationBuilder builder, string connectionStringName)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddCheck<AuthorizationEndpointHealthCheck>("authorization")
            .AddCheck<TokenEndpointHealthCheck>("token")
            .AddCheck<TokenWorkerHealthCheck>("token_worker")
            .AddCheck<ActivityWorkerHealthCheck>("activity_worker")
            .AddSqlServer(connectionString: builder.Configuration.GetConnectionString(connectionStringName)!,
             name: "sql",
             failureStatus: HealthStatus.Unhealthy);
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