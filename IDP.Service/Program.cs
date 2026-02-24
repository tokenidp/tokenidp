using IDP.Server.ApplicationSetup;
using IDP.Server.Components;
using NLog;
using NLog.Extensions.Hosting;

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

    builder.AddTresorAuthServices("Identity_DB", "tresor.admin.api");

    //builder.AddTokenTresorServices(
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
    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    app.UseCors(policy => policy
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithOrigins("http://localhost:3000", "https://tresorauth-admin-cpdyhza4cadhbsfq.canadacentral-01.azurewebsites.net") // replace with your actual client URL
        .AllowCredentials()
    );

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode();

    await app.UseTresorAuthAsync("Identity_DB");

    app.MapGet("/", () => "IDP is running.");

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
