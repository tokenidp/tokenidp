using Admin.Core.Clients;
using Admin.Core.Configurations;
using Admin.Core.Lookups;
using Admin.Core.Roles;
using Admin.Core.Tenants;
using Admin.Core.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.Core;

public static class DependencyInjection
{
    public static void AddAdminServices(this IServiceCollection services,
         IConfiguration configuration)
    {
        services.AddScoped<RoleService>();
        services.AddScoped<ClientService>();
        services.AddScoped<TenantService>();
        services.AddScoped<ConfigurationService>();
        services.AddScoped<UserService>();
        services.AddScoped<LookupService>();
    }
}