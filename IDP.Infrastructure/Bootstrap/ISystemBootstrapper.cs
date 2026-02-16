namespace IDP.Infrastructure.Bootstrap;

internal interface ISystemBootstrapper
{
    Task BootstrapAsync(CancellationToken ct, string connectionStringName);
}
