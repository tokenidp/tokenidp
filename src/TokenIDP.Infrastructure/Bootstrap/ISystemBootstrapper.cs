namespace TokenIDP.Infrastructure.Bootstrap;

internal interface ISystemBootstrapper
{
    Task BootstrapAsync(CancellationToken ct, string databaseProvider, string connectionString);
}

