using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IDP.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile(Path.Combine("..", "IDP.Service", "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine("..", "IDP.Service", $"appsettings.{environmentName}.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionStringName = ResolveConnectionStringName(configuration, args);
        var connectionString = DatabaseProviderResolver.GetConnectionString(
            configuration,
            connectionStringName);
        var provider = DatabaseProviderResolver.ResolveProvider(configuration);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseApplicationDatabase(provider, connectionString)
            .Options;

        var systemUser = new SystemCurrentUserService();

        return new ApplicationDbContext(options, systemUser);
    }

    private static string ResolveConnectionStringName(IConfiguration configuration, string[] args)
    {
        var connectionStringName = TryGetArgumentValue(args, "--connection-string-name");
        if (!string.IsNullOrWhiteSpace(connectionStringName))
        {
            return connectionStringName;
        }

        var envConnectionStringName = Environment.GetEnvironmentVariable("IDP_CONNECTION_STRING_NAME");
        if (!string.IsNullOrWhiteSpace(envConnectionStringName))
        {
            return envConnectionStringName;
        }

        var configuredNames = configuration.GetSection("ConnectionStrings")
            .GetChildren()
            .Select(child => child.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();

        if (configuredNames.Count == 1)
        {
            return configuredNames[0];
        }

        throw new InvalidOperationException(
            "Unable to resolve the design-time connection string name. " +
            "Provide --connection-string-name=<name>, set IDP_CONNECTION_STRING_NAME, or configure exactly one ConnectionStrings entry.");
    }

    private static string? TryGetArgumentValue(string[] args, string optionName)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                return arg[(optionName.Length + 1)..].Trim();
            }

            if (string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length &&
                !string.IsNullOrWhiteSpace(args[i + 1]))
            {
                return args[i + 1].Trim();
            }
        }

        return null;
    }
}
