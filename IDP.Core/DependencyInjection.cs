using IDP.Core.GrantHandlers;
using IDP.Core.Policies;
using IDP.Core.UseCases;
using IDP.Domain.AggregateRoots.Clients;
using IDP.Foundation.Abstractions.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Core;

public static class DependencyInjection
{
    public static void AddIDPServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<TokenSecretGenerator>();

        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();

        AddUseCases(services);
        AddGrantHandlers(services);
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<GrantTypeValidatorUseCase>();
        services.AddScoped<UserInfoUseCase>();
        services.AddScoped<TokenIssuerUseCase>();
        services.AddScoped<RevokeTokenUseCase>();
        services.AddScoped<IntrospectionUseCase>();
        services.AddScoped<TokenContextUseCase>();
        services.AddScoped<TokenContextUseCase>();
        services.AddScoped<TenantUserMfaPolicy>();

        services.AddMfaService();
        services.AddAuthorizationUseCase();
    }

    private static void AddAuthorizationUseCase(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationCodeUseCase>(sp =>
            new AuthorizationCodeUseCase(
                sp.GetRequiredService<IAuthenticationService>(),
                sp.GetRequiredService<IAppLogger<AuthorizationCodeUseCase>>(),
                sp.GetRequiredService<IMfaUseCase>(),
                sp.GetRequiredService<IAuthorizationStore>(),
                sp.GetRequiredService<TokenContextUseCase>(),
                sp.GetRequiredService<TenantUserMfaPolicy>(),
                sp.GetRequiredService<IClientStore>(),
                sp.GetRequiredService<ITenantContextAccessor>()));

        services.AddScoped<IAuthorizationRequestValidator, AuthorizationRequestValidator>();
        services.AddScoped<IAuthorizationPageUiUseCase, AuthorizationPageUiUseCase>();
    }

    private static void AddMfaService(this IServiceCollection services)
    {
        services.AddScoped<TenantUserMfaPolicy>();

        services.AddScoped<IMfaUseCase>(sp =>
            new MfaUseCase(sp.GetRequiredService<IUserStore>(),
                sp.GetRequiredService<IAuthorizationStore>(),
                sp.GetRequiredService<IAppLogger<MfaUseCase>>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetRequiredService<IEmailQueueStore>()));
    }

    private static void AddGrantHandlers(IServiceCollection services)
    {
        services.AddScoped<TokenGrantFactory>();

        services.AddScoped<ITokenGrantUseCase>(sp =>
           new TokenGrantPipeline(sp.GetRequiredService<TokenGrantFactory>(),
               sp.GetRequiredService<IAppLogger<TokenGrantPipeline>>(),
               sp.GetRequiredService<GrantTypeValidatorUseCase>()));

        services.AddScoped<RefreshTokenGrantHandler>();
        services.AddScoped<AuthorizationCodeGrantHandler>();
        services.AddScoped<ClientCredentialGrantHandler>();

        services.AddTransient<Func<GrantTypes, ITokenGrantHandler>>(serviceProvider => key =>
        {
            return key switch
            {
                GrantTypes.authorization_code => serviceProvider.GetRequiredService<AuthorizationCodeGrantHandler>(),
                GrantTypes.refresh_token => serviceProvider.GetRequiredService<RefreshTokenGrantHandler>(),
                GrantTypes.client_credentials => serviceProvider.GetRequiredService<ClientCredentialGrantHandler>(),
                _ => serviceProvider.GetRequiredService<AuthorizationCodeGrantHandler>()
            };
        });
    }
}
