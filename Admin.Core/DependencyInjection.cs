using Admin.Core.Activities.UseCases;
using Admin.Core.Clients.UseCases;
using Admin.Core.Common;
using Admin.Core.Configurations;
using Admin.Core.Dashboard;
using Admin.Core.Permissions.UseCases;
using Admin.Core.Roles.UseCases;
using Admin.Core.Settings.UseCases;
using Admin.Core.Tenants.UseCases;
using Admin.Core.Tokens.UseCases;
using Admin.Core.Users.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.Core;

public static class DependencyInjection
{
    public static void AddAdminServices(this IServiceCollection services,
         IConfiguration configuration)
    {
        services.AddScoped<ClientCommandUseCase>();
        services.AddScoped<ClientLookupsUseCase>();
        services.AddScoped<ClientQueryUseCase>();

        services.AddScoped<RoleCommandUseCase>();
        services.AddScoped<RoleQueryUseCase>();

        services.AddScoped<UserQueryUseCase>();
        services.AddScoped<UserCommandUseCase>();
        services.AddScoped<UserLookupsUseCase>();
        services.AddScoped<PasswordResetUseCase>();

        services.AddScoped<UserPermissionsUseCase>();
        services.AddScoped<PermissionCommandUseCase>();
        services.AddScoped<PermissionQueryUseCase>();
        services.AddScoped<PermissionLookupsUseCase>();

        services.AddScoped<TenantQueryUseCase>();
        services.AddScoped<TenantCommandUseCase>();
        services.AddScoped<TenantLookupsUseCase>();

        services.AddScoped<ITenantConfigurationRepository, TenantConfigurationRepository>();
        services.AddScoped<ConfigurationsQueryUseCase>();
        services.AddScoped<ConfigurationQueryByIdUseCase>();
        services.AddScoped<ConfigurationQueryByKeyUseCase>();
        services.AddScoped<ConfigurationCommandUseCase>();
        services.AddScoped<ConfigurationUpdateCommandUseCase>();
        services.AddScoped<ConfigurationDeleteCommandUseCase>();
        services.AddScoped<ConfigurationUpsertCommandUseCase>();
        services.AddScoped<ConfigurationsBulkCommandUseCase>();

        services.AddScoped<TokenQueryUseCase>();
        services.AddScoped<TokenLookupsUseCase>();
        services.AddScoped<TokenCommandUseCase>();

        services.AddScoped<ActivityQueryUseCase>();
        services.AddScoped<ActivityLookupsUseCase>();

        services.AddScoped<DashboardQueryUseCase>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();

        services.AddScoped<PasswordService>();
        services.AddScoped<UserNormalizationService>();
        services.AddScoped<LockoutPolicy>();
    }
}