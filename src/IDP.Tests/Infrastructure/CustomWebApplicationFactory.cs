using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IDP.Tests.Infrastructure;

public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    private static string GetEnvironment => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    private static string GetProjectDir => AppContext.BaseDirectory;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseContentRoot(contentRoot: GetProjectDir)
            .UseEnvironment(environment: GetEnvironment)
            .ConfigureAppConfiguration(ConfigureBuilderWithAppSettingsJson);
    }

    /// <summary>
    /// This takes an IConfigurationBuilder and configures it to use the correct app settings file and environment
    /// variables based on the environment the integration test is running in. 
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureBuilderWithAppSettingsJson(IConfigurationBuilder builder)
    {
        var projectDir = GetProjectDir;
        var baseConfigPath = Path.Combine(projectDir, "appsettings.json");

        builder.Sources.Clear();
        builder.AddJsonFile(path: baseConfigPath, optional: false, reloadOnChange: false);
        builder.AddEnvironmentVariables();
    }
}