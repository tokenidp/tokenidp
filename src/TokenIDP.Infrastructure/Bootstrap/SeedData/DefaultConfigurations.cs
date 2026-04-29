using Microsoft.Extensions.Configuration;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultConfigurations
{
    public static IReadOnlyList<SeedConfiguration> GetSystem(IConfiguration configuration)
    {
        var region =
            configuration["Dashboard:Region"]
            ?? configuration["Region"]
            ?? Environment.GetEnvironmentVariable("WEBSITE_REGION")
            ?? Environment.GetEnvironmentVariable("REGION_NAME")
            ?? "local";

        var version =
            configuration["Dashboard:Version"]
            ?? GetApplicationVersion();

        return new List<SeedConfiguration>
        {
            new("dashboard.region", region, ValueTypes.String, ConfigurationScopes.System, isEditable: true),
            new("dashboard.version", version, ValueTypes.String, ConfigurationScopes.System, isEditable: true)
        };
    }

    public static readonly IReadOnlyList<SeedConfiguration> Notification = new List<SeedConfiguration>
    {
        new("smtpserver", "smtp.gmail.com", ValueTypes.String, ConfigurationScopes.Notification, isEditable: true),
        new("smtpport", "587", ValueTypes.Int, ConfigurationScopes.Notification, isEditable: true),
        new("smtpusername", "SMTP_USERNAME", ValueTypes.String, ConfigurationScopes.Notification, isEditable: true),
        new("smtppassword", "SMTP_PASSWORD", ValueTypes.String, ConfigurationScopes.Notification, isEditable: true),
        new("smtpusessl", "true", ValueTypes.Bool, ConfigurationScopes.Notification, isEditable: true),
        new("fromemail", "admin@mail.com", ValueTypes.String, ConfigurationScopes.Notification, isEditable: true),
        new("fromname", "Administrator", ValueTypes.String, ConfigurationScopes.Notification, isEditable: true),
        new("retryattempts", "2", ValueTypes.Int, ConfigurationScopes.Notification, isEditable: true),
        new("retrydelay", "5", ValueTypes.Int, ConfigurationScopes.Notification, isEditable: true),
        new("emailprovidertype", "SMTP", ValueTypes.String, ConfigurationScopes.Notification, isEditable: false)
    };

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        var version = assembly.GetName().Version;
        return version is null ? "v1.0.0" : $"v{version}";
    }
}

internal sealed record SeedConfiguration(
    string Key,
    string Value,
    ValueTypes ValueType,
    ConfigurationScopes Scope,
    bool isEditable
);

