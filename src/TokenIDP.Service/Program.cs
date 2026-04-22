using NLog;
using NLog.Extensions.Hosting;
using TokenIDP.Server.ApplicationSetup;
using TokenIDP.Server.Components;
using TokenIDP.Server.Middlewares;
using TokenIDP.Core.Foundation.Options;

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
