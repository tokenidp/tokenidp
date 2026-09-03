using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TokenIDP.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configurationBuilder = new ConfigurationBuilder();

        foreach (var configurationDirectory in GetConfigurationDirectories())
        {
            configurationBuilder
                .AddJsonFile(Path.Combine(configurationDirectory, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(configurationDirectory, $"appsettings.{environmentName}.json"), optional: true);
        }

        var configuration = configurationBuilder
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

    private static IEnumerable<string> GetConfigurationDirectories()
    {
        var yieldedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in EnumerateCurrentAndParentDirectories(Directory.GetCurrentDirectory()))
        {
            foreach (var candidate in GetConfigurationDirectoryCandidates(directory))
            {
                if (Directory.Exists(candidate) && yieldedDirectories.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateCurrentAndParentDirectories(string startDirectory)
    {
        for (var current = new DirectoryInfo(startDirectory); current is not null; current = current.Parent)
        {
            yield return current.FullName;
        }
    }

    private static IEnumerable<string> GetConfigurationDirectoryCandidates(string directory)
    {
        yield return directory;
        yield return Path.Combine(directory, "TokenIDP.Host");
        yield return Path.Combine(directory, "IDP.Service");
        yield return Path.Combine(directory, "src", "TokenIDP.Host");
        yield return Path.Combine(directory, "src", "IDP.Service");
    }
}

