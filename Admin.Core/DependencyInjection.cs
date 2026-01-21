using Admin.Core.Clients;
using Admin.Core.Configurations;
using Admin.Core.Permissions;
using Admin.Core.Roles;
using Admin.Core.Tenants;
using Admin.Core.Tokens;
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
       
        services.AddScoped<GetUserUseCase>();
        services.AddScoped<CreateUpdateUserUseCase>();
        services.AddScoped<GetUserLookupsUseCase>();

        services.AddScoped<GetUserPermissionsUseCase>();
        services.AddScoped<CreateUpdatePermissionUseCase>();
        services.AddScoped<GetPermissionUseCase>();
        services.AddScoped<GetPermissionLookupsUseCase>();

        services.AddScoped<GetTenantUseCase>();
        services.AddScoped<CreateUpdateTenantUseCase>();
        services.AddScoped<GetTenantLookupsUseCase>();

        services.AddScoped<ITenantConfigurationRepository, TenantConfigurationRepository>();
        services.AddScoped<GetTenantConfigurationsUseCase>();
        services.AddScoped<GetTenantConfigurationByIdUseCase>();
        services.AddScoped<GetTenantConfigurationByKeyUseCase>();
        services.AddScoped<CreateTenantConfigurationUseCase>();
        services.AddScoped<UpdateTenantConfigurationUseCase>();
        services.AddScoped<DeleteTenantConfigurationUseCase>();
        services.AddScoped<UpsertTenantConfigurationUseCase>();
        services.AddScoped<BulkUpdateTenantConfigurationsUseCase>();

        services.AddScoped<GetTokenUseCase>();
        services.AddScoped<GetTokenLookupsUseCase>();
        services.AddScoped<TokenCommandUseCase>();
    }
}