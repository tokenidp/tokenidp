using Microsoft.Extensions.Configuration;

namespace TokenIDP.Infrastructure.Persistence;

public static class DatabaseProviderResolver
{
    public const string SqlServer = "sqlserver";
    public const string MySql = "mysql";
    public const string PostgreSql = "postgresql";

    public static string ResolveProvider(IConfiguration configuration)
    {
        return NormalizeProvider(configuration["Database:Provider"]);
    }

    public static string GetConnectionString(IConfiguration configuration, string connectionStringName)
    {
        return configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is not configured.");
    }

    public static void Configure(
        DbContextOptionsBuilder optionsBuilder,
        IConfiguration configuration,
        string connectionStringName)
    {
        var provider = ResolveProvider(configuration);
        var connectionString = GetConnectionString(configuration, connectionStringName);

        Configure(optionsBuilder, provider, connectionString);
    }

    public static void Configure(
        DbContextOptionsBuilder optionsBuilder,
        string provider,
        string connectionString)
    {
        switch (NormalizeProvider(provider))
        {
            case MySql:
                optionsBuilder.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString));
                break;

            case SqlServer:
                optionsBuilder.UseSqlServer(connectionString);
                break;

            case PostgreSql:
                optionsBuilder.UseNpgsql(connectionString);
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{provider}' is not supported.");
        }
    }

    public static DbContextOptionsBuilder UseApplicationDatabase(
        this DbContextOptionsBuilder optionsBuilder,
        string provider,
        string connectionString)
    {
        Configure(optionsBuilder, provider, connectionString);
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseApplicationDatabase<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string provider,
        string connectionString)
        where TContext : DbContext
    {
        Configure(optionsBuilder, provider, connectionString);
        return optionsBuilder;
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return SqlServer;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "sql_server" or "mssql" => SqlServer,
            "mysql" => MySql,
            "postgresql" or "postgres" or "pgsql" or "npgsql" or "postrgesql" => PostgreSql,
            _ => throw new InvalidOperationException(
                $"Database provider '{provider}' is not supported. Supported values: SqlServer, MySql, PostgreSql.")
        };
    }
}

