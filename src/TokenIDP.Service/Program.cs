using NLog;
using NLog.Extensions.Hosting;
using System.Reflection;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Server.ApplicationSetup;
using TokenIDP.Server.Components;
using TokenIDP.Server.Middlewares;

// Bootstrap NLog early
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config", optional: false)
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Remove default providers
    builder.Logging.ClearProviders();

    // Add NLog as the ONLY provider
    builder.Host.UseNLog();

    builder.AddTokenIDPServices("Identity_DB", "tokenidp.admin.api");
    var corsOptions = builder.Configuration
        .GetSection(CorsOptions.SectionName)
        .Get<CorsOptions>() ?? new CorsOptions();

    //builder.AddTokenIDPServices(
    //    connectionStringName: "DefaultConnection",
    //    audience: "idp-api",
    //    configureToken: options =>
    //    {
    //        options.Issuer = "https://idp.example.com";
    //        options.KeyPath = "C:\\secrets\\signing-key.pem";
    //    });

    builder.Services.AddAuthorization();

    builder.Services
        .AddRazorComponents()
        .AddInteractiveServerComponents();

    // Bind to Azure-injected port (Linux App Service uses 8080)
    //var port = Environment.GetEnvironmentVariable("WEBSITES_PORT") ?? "8080";
    //builder.WebHost.UseUrls($"http://*:{port}");

    var app = builder.Build();
    var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    var informationalVersion =
        entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? entryAssembly.GetName().Version?.ToString()
        ?? "unknown";
    var productVersion = informationalVersion.Split('+', 2)[0];
    var buildCommitSha = builder.Configuration["Build:CommitSha"] ?? "unknown";
    var buildRunId = builder.Configuration["Build:RunId"] ?? "unknown";

    logger.Info(
        "Application startup. Environment={Environment}, Version={Version}, InformationalVersion={InformationalVersion}, CommitSha={CommitSha}, RunId={RunId}",
        app.Environment.EnvironmentName,
        productVersion,
        informationalVersion,
        buildCommitSha,
        buildRunId);

    app.UseExceptionHandler("/error");
    app.UseForwardedHeaders();
    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseRouting();

    app.UseCors(policy => policy
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithOrigins(corsOptions.AllowedOrigins)
        .AllowCredentials()
    );

    app.UseRateLimiter();

    app.UseAntiforgery();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode();

    await app.UseTokenIDPAsync("Identity_DB");

    app.MapGet("/", () => "TokenIDP is running.");
    app.MapGet("/health/version", () => Results.Ok(new
    {
        environment = app.Environment.EnvironmentName,
        version = productVersion,
        informationalVersion,
        commitSha = buildCommitSha,
        runId = buildRunId
    }));

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, "Unhandled exception during logging test");
}
finally
{
    LogManager.Shutdown();
}
