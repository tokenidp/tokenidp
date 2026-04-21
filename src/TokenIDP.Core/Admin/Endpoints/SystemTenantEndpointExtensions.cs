namespace TokenIDP.Core.Admin.Endpoints;

internal static class SystemTenantEndpointExtensions
{
    private const string SystemTenantPolicy = "system-tenant";

    public static TBuilder RequireSystemTenant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(SystemTenantPolicy);
        return builder;
    }
}
