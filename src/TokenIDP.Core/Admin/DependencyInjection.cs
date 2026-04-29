using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TokenIDP.Core.Admin.Activities.UseCases;
using TokenIDP.Core.Admin.ApiResources.UseCases;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Clients.UseCases;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Dashboard;
using TokenIDP.Core.Admin.Permissions.UseCases;
using TokenIDP.Core.Admin.Roles.UseCases;
using TokenIDP.Core.Admin.Settings.UseCases;
using TokenIDP.Core.Admin.Tenants.UseCases;
using TokenIDP.Core.Admin.Tokens.UseCases;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;
using TokenIDP.Core.Foundation.Validation;

namespace TokenIDP.Core.Admin;

public static class DependencyInjection
{
    public static void AddAdminServices(this IServiceCollection services,
         IConfiguration configuration)
    {
        services.AddAssemblyValidators(typeof(DependencyInjection).Assembly);

        services.AddScoped<ApiResourceCommandUseCase>();
        services.AddScoped<ApiResourceQueryUseCase>();

        services.AddScoped<ClientCommandValidator>();
        services.AddScoped<ClientCommandUseCase>();
        services.AddScoped<ClientLookupsUseCase>();
        services.AddScoped<ClientQueryUseCase>();

        services.AddScoped<RoleCommandUseCase>();
        services.AddScoped<RoleQueryUseCase>();

        services.AddScoped<UserQueryUseCase>();
        services.AddScoped<UserCommandUseCase>();
        services.AddScoped<CreateAccountUseCase>();
        services.AddScoped<UserLookupsUseCase>();
        services.AddScoped<PasswordResetUseCase>();
        services.AddScoped<EmailConfirmationUseCase>();

        services.AddScoped<UserPermissionsUseCase>();
        services.AddScoped<PermissionCommandUseCase>();
        services.AddScoped<PermissionQueryUseCase>();
        services.AddScoped<PermissionLookupsUseCase>();

        services.AddScoped<TenantQueryUseCase>();
        services.AddScoped<TenantCommandUseCase>();
        services.AddScoped<TenantLookupsUseCase>();

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
