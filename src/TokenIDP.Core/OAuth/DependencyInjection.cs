using TokenIDP.Core.OAuth.Endpoints;
using TokenIDP.Core.OAuth.GrantHandlers;
using TokenIDP.Core.OAuth.Policies;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Core.Foundation.Validation;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Core.OAuth.ExternalProviders;
using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth;

public static class DependencyInjection
{
    public static void AddIDPServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAssemblyValidators(typeof(DependencyInjection).Assembly);

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<TokenSecretGenerator>();

        AddUseCases(services);
        AddGrantHandlers(services);

        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<IExternalAuthUseCase, ExternalAuthUseCase>();
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<GrantTypeValidatorUseCase>();
        services.AddScoped<TokenEndpointClientAuthService>();
        services.AddScoped<UserInfoUseCase>();
        services.AddScoped<TokenIssuerUseCase>();
        services.AddScoped<RevokeTokenUseCase>();
        services.AddScoped<IntrospectionUseCase>();
        services.AddScoped<TokenContextUseCase>();
        services.AddScoped<TenantUserMfaPolicy>();
        services.AddScoped<DeviceAuthorizationUseCase>();
        services.AddScoped<IDeviceAuthenticationUseCase, DeviceAuthenticationUseCase>();

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
                sp.GetRequiredService<IAuthorizationRepository>(),
                sp.GetRequiredService<TokenContextUseCase>(),
                sp.GetRequiredService<TenantUserMfaPolicy>(),
                sp.GetRequiredService<IClientRepository>(),
                sp.GetRequiredService<IUserSignInService>()));

        services.AddScoped<IAuthorizationRequestValidator, AuthorizationRequestValidator>();
        services.AddScoped<IAuthorizationPageUiUseCase, AuthorizationPageUiUseCase>();
    }

    private static void AddMfaService(this IServiceCollection services)
    {
        services.AddScoped<TenantUserMfaPolicy>();

        services.AddScoped<IMfaUseCase>(sp =>
            new MfaUseCase(sp.GetRequiredService<IUserRepository>(),
                sp.GetRequiredService<IAuthorizationRepository>(),
                sp.GetRequiredService<IAppLogger<MfaUseCase>>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetRequiredService<IEmailQueueRepository>()));
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
        services.AddScoped<DeviceFlowGrantHandler>();
        services.AddScoped<PasswordGrantHandler>();

        services.AddTransient<Func<GrantTypes, ITokenGrantHandler>>(serviceProvider => key =>
        {
            return key switch
            {
                GrantTypes.authorization_code => serviceProvider.GetRequiredService<AuthorizationCodeGrantHandler>(),
                GrantTypes.refresh_token => serviceProvider.GetRequiredService<RefreshTokenGrantHandler>(),
                GrantTypes.client_credentials => serviceProvider.GetRequiredService<ClientCredentialGrantHandler>(),
                GrantTypes.device_code => serviceProvider.GetRequiredService<DeviceFlowGrantHandler>(),
                GrantTypes.password => serviceProvider.GetRequiredService<PasswordGrantHandler>(),
                _ => throw new TokenRequestValidationException("unsupported_grant_type",
                    $"Grant type '{key}' is not supported.")
            };
        });
    }
}


