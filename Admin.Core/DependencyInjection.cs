using Admin.Core.Clients;
using Admin.Core.Configurations;
using Admin.Core.Lookups;
using Admin.Core.Permissions;
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
        services.AddScoped<CreateUpdateClientUseCase>();
        services.AddScoped<GetClientLookupsUseCase>();
        services.AddScoped<GetClientUseCase>();
        services.AddScoped<CreateUpdateRoleUseCase>();
        services.AddScoped<GetRoleUseCase>();
        services.AddScoped<CreateUpdatePermissionUseCase>();
        services.AddScoped<GetPermissionUseCase>();
        services.AddScoped<GetPermissionLookupsUseCase>();
        services.AddScoped<GetUserUseCase>();
        services.AddScoped<CreateUpdateUserUseCase>();
        services.AddScoped<GetUserLookupsUseCase>();
        services.AddScoped<GetUserPermissionsUseCase>();
        services.AddScoped<LookupUseCases>();
        services.AddScoped<TenantUseCases>();
        services.AddScoped<ConfigurationUseCases>();
    }
}