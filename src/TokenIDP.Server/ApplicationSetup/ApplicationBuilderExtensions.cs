using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Reflection;
using System.Text.Json.Serialization;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Telemetry;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Endpoints;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.Endpoints;
using TokenIDP.Core.OAuth.RateLimiting;
using TokenIDP.Infrastructure;
using TokenIDP.Server.Components;
using TokenIDP.Server.HealthChecks;
using TokenIDP.Server.Middlewares;
using TokenIDP.Server.Multitenancy;
using TokenIDP.Server.Security;
using TokenIDP.Server.Telemetry;
using TokenIDP.Workers;

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
        Action<TokenOptions>? configureToken)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException("Token audience is required.", nameof(audience));
        }

        var tokenOptions = ResolveTokenOptions(
            builder.Configuration,
            builder.Environment,
            audience,
            configureToken);

        builder.Services.AddSingleton<IOptions<TokenOptions>>(Options.Create(tokenOptions));

        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<TenantResolutionOptions>(
            builder.Configuration.GetSection(TenantResolutionOptions.SectionName));
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.RequireHeaderSymmetry = false;
        });

        builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
        builder.Services.AddScoped<ITenantRequestResolver, TenantRequestResolver>();
        builder.Services.AddScoped<ITenantResolver, TenantResolver>();

        builder.Services.AddScoped<LoadService>();

        builder.Services.AddIDPServices(builder.Configuration);

        builder.Services.AddAdminServices(builder.Configuration);

        builder.Services.AddInfrastructureServices(builder.Configuration, connectionStringName);

        builder.Services.AddProjectionServices(builder.Configuration);

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(SystemTenantRequirement.PolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new SystemTenantRequirement());
            });
        });

        builder.Services.AddAuthentication(tokenOptions, builder.Environment, builder.Configuration);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = new OAuthClientRateLimiter();
            options.OnRejected = async (context, cancellationToken) =>
            {
                var handler = context.HttpContext.RequestServices
                    .GetRequiredService<OAuthRateLimitRejectionHandler>();

                await handler.HandleAsync(context, cancellationToken);
            };
        });


        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        AddHealthChecks(builder, connectionStringName);
        AddOpenTelemetry(builder);

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, DynamicRolePolicyHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, SystemTenantAuthorizationHandler>();

        builder.Services.AddAuthorization();

        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        // Bind to Azure-injected port (Linux App Service uses 8080)
        //var port = Environment.GetEnvironmentVariable("WEBSITES_PORT") ?? "8080";
        //builder.WebHost.UseUrls($"http://*:{port}");

        return builder;
    }

    public static async Task<WebApplication> UseTokenIDPAsync(this WebApplication app,
        string[] allowedOrigins,
        string connectionStringName = "")
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseMiddleware<RequestLatencyTelemetryMiddleware>();

        app.UseMiddleware<TenantResolutionMiddleware>();

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseCors(policy => policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins(allowedOrigins)
            .AllowCredentials()
        );

        app.UseRateLimiter();

        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        app.RegisterIDPEndpoints();

        app.RegisterAdminEndpoints();

        await app.EnsureSystemBootstrap(connectionStringName);

        app.MapGet("/", () => "TokenIDP is running.");

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion =
            entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? entryAssembly.GetName().Version?.ToString()
            ?? "unknown";
        var productVersion = informationalVersion.Split('+', 2)[0];

        //var buildCommitSha = builder.Configuration["Build:CommitSha"] ?? "unknown";
        //var buildRunId = builder.Configuration["Build:RunId"] ?? "unknown";

        app.MapGet("/health/version", () => Results.Ok(new
        {
            environment = app.Environment.EnvironmentName,
            version = productVersion,
            informationalVersion
        }));

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

    private static void AddOpenTelemetry(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IRequestLatencyTelemetryStore, RequestLatencyTelemetryStore>();
        builder.Services.AddScoped<ClientTenantResolver>();

        var serviceName = builder.Environment.ApplicationName;
        var otlpEndpoint =
            builder.Configuration["OpenTelemetry:OtlpEndpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(RequestLatencyMetrics.MeterName);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint, UriKind.Absolute);
                    });
                }
            });
    }

    internal static TokenOptions ResolveTokenOptions(
        IConfiguration configuration,
        IHostEnvironment environment,
        string audience,
        Action<TokenOptions>? configureToken)
    {
        var tokenOptions = configuration
            .GetSection("TokenOptions")
            .Get<TokenOptions>() ?? new TokenOptions();

        tokenOptions.Audience = audience;
        configureToken?.Invoke(tokenOptions);

        if (!environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(tokenOptions.Key) &&
                string.IsNullOrWhiteSpace(tokenOptions.KeyPath) &&
                string.IsNullOrWhiteSpace(tokenOptions.CertificateThumbprint) &&
                string.IsNullOrWhiteSpace(tokenOptions.CertificateSubjectName))
            {
                tokenOptions.Key = TokenKeyDefault.DevelopmentKey;
            }
        }
        else if (string.IsNullOrWhiteSpace(tokenOptions.CertificateThumbprint) &&
                 string.IsNullOrWhiteSpace(tokenOptions.CertificateSubjectName))
        {
            throw new InvalidOperationException(
                "Token signing certificate is required in production. " +
                "Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
        }

        if (string.Equals(tokenOptions.Key, TokenKeyDefault.DevelopmentKey, StringComparison.Ordinal) &&
            environment.IsProduction())
        {
            throw new InvalidOperationException("Development signing key cannot be used in production.");
        }

        _ = TokenOptionsResolver.ResolveIssuer(tokenOptions);
        _ = TokenOptionsResolver.ResolveAudience(tokenOptions);

        return tokenOptions;
    }
}