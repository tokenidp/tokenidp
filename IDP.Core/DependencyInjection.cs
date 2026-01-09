using IDP.Core.OAuth;
using IDP.Core.TokenHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Core;

public static class DependencyInjection
{
    public static void AddIDPServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDomainServices(services);
        AddTokenHandlers(services);
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        services.AddScoped<ClientService>();
        services.AddScoped<UserInfoService>();
    }

    private static void AddAuthorizationUseCases(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationCodeUseCase>(sp =>
            new AuthorizationCodeUseCase(
                sp.GetRequiredService<IIdentityStore>(),
                sp.GetRequiredService<IAppLogger<AuthorizationCodeUseCase>>(),
                sp.GetRequiredService<IMfaService>(),
                sp.GetRequiredService<AuthorizationCodeService>(),
                sp.GetRequiredService<ClientService>()));

    }

    private static void AddMfaServices(this IServiceCollection services)
    {
        services.AddScoped<IMfaService>(sp =>
            new MfaService(
                sp.GetRequiredService<IAppLogger<MfaService>>(),
                sp.GetRequiredService<IEmailSetting>(),
                sp.GetRequiredService<IPreAuthorizationStore>(),
                sp.GetRequiredService<EmailProviderFactory>(),
                sp.GetRequiredService<IIdentityStore>()));

    }

    private static void AddTokenHandlers(IServiceCollection services)
    {
        services.AddScoped<JwtTokenGenerator>();
        services.AddAuthorizationUseCases();
        services.AddMfaServices();
        services.AddScoped<TokenValidatorService>();
        services.AddScoped<TokenGrantUseCase>();
        services.AddScoped<TokenService>();
        services.AddScoped<RevokeTokenService>();
        services.AddScoped<TokenGrantFactory>();
        services.AddScoped<RefreshTokenGrantHandler>();
        services.AddScoped<AuthorizationCodeGrantHandler>();
        services.AddScoped<ClientCredentialGrantHandler>();
        services.AddScoped<IntrospectionValidatorService>();
        services.AddScoped<AuthorizationCodeService>();

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
