using NLog;
using NLog.Extensions.Hosting;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Server.ApplicationSetup;

// Bootstrap NLog early
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config", optional: false)
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Remove default providers
    builder.Logging.ClearProviders();

    builder.Host.UseNLog();

    builder.AddTokenIDPServices("Identity_DB", "tokenidp.admin.api");

    //builder.AddTokenIDPServices(
    //    connectionStringName: "DefaultConnection",
    //    audience: "idp-api",
    //    configureToken: options =>
    //    {
    //        options.Issuer = "https://idp.example.com";
    //        options.KeyPath = "C:\\secrets\\signing-key.pem";
    //    });

    var corsOptions = builder.Configuration
        .GetSection(CorsOptions.SectionName)
        .Get<CorsOptions>() ?? new CorsOptions();

    var app = builder.Build();
    
    app.UseExceptionHandler("/error");
   
    await app.UseTokenIDPAsync(corsOptions.AllowedOrigins, "Identity_DB");

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
