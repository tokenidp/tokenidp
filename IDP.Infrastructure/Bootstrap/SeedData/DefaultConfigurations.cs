using IDP.Domain.AggregateRoots.Configurations;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultConfigurations
{
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
}

internal sealed record SeedConfiguration(
    string Key,
    string Value,
    ValueTypes ValueType,
    ConfigurationScopes Scope,
    bool isEditable
);
