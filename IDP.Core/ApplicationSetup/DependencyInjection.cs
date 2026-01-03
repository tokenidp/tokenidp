using IDP.Common.Notifications;
using IDP.Common.Options;
using IDP.Core.OAuth;
using IDP.Core.OAuth.DomainServices;
using IDP.Core.OAuth.TokenHandlers;
using IDP.Core.TokenHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IDP.Core.ApplicationSetup;

internal static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services,
        IConfiguration configuration,
        Action<TokenOption>? configureToken)
    {
        ConfigureTokenOptions(services, configuration, configureToken);
        AddInfrastructure(services);
        AddDomainServices(services);
        AddTokenHandlers(services);
        AddEmailServices(services);
    }

    public static void AddPersistence(this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
                  options.UseSqlServer(
                      configuration.GetConnectionString(connectionStringName)));

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }

    private static void AddInfrastructure(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddCors();

        services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<JsonHelper>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        services.AddScoped<AuditService>();
        services.AddScoped<LookupService>();
        services.AddScoped<ClientService>();
        services.AddScoped<TenantService>();
        services.AddScoped<RoleService>();
        services.AddScoped<UserService>();
    }

    private static void AddAuthorizationUseCases(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationCodeUseCase>(sp =>
            new AuthorizationCodeUseCase(
                sp.GetRequiredService<AuthenticationService>(),
                sp.GetRequiredService<IAppLogger<AuthorizationCodeUseCase>>(),
                sp.GetRequiredService<MfaService>(),
                sp.GetRequiredService<AuthorizationCodeService>(),
                sp.GetRequiredService<ClientService>()));

    }


    private static void AddTokenHandlers(IServiceCollection services)
    {
        services.AddScoped<TokenValidatorService>();
        services.AddScoped<TokenUseCase>();
        services.AddScoped<TokenService>();
        services.AddAuthorizationUseCases();
        services.AddScoped<RevokeTokenService>();
        services.AddScoped<MfaService>();
        services.AddScoped<TokenGrantFactory>();
        services.AddScoped<RefreshTokenGrantHandler>();
        services.AddScoped<AuthorizationCodeGrantHandler>();
        services.AddScoped<ClientCredentialGrantHandler>();
        services.AddScoped<IntrospectionValidatorService>();
        services.AddScoped<AuthorizationCodeService>();
        services.AddScoped<PreAuthorizationService>();

        services.AddTransient<Func<GrantType, ITokenGrantHandler>>(serviceProvider => key =>
        {
            return key switch
            {
                GrantType.authorization_code => serviceProvider.GetRequiredService<AuthorizationCodeGrantHandler>(),
                GrantType.refresh_token => serviceProvider.GetRequiredService<RefreshTokenGrantHandler>(),
                GrantType.client_credentials => serviceProvider.GetRequiredService<ClientCredentialGrantHandler>(),
                _ => serviceProvider.GetRequiredService<AuthorizationCodeGrantHandler>()
            };
        });
    }

    private static void AddEmailServices(IServiceCollection services)
    {
        services.AddScoped<SmtpEmail>();
        services.AddScoped<SendGridEmail>();
        services.AddScoped<EmailProviderFactory>();
        services.AddScoped<IEmailSetting, EmailSetting>();

        services.AddTransient<Func<EmailProviderType, IEmailNotification>>(serviceProvider => key =>
        {
            return key switch
            {
                EmailProviderType.SendGrid => serviceProvider.GetRequiredService<SendGridEmail>(),
                _ => serviceProvider.GetRequiredService<SmtpEmail>()
            };
        });
    }

    public static void AddAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureTokenOptions(services, configuration, null);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
       .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<TokenOption>, IHttpContextAccessor, IHostEnvironment>(
            (options, tokenOptions, httpContextAccessor, environment) =>
            {
                var token = tokenOptions.Value;
                var signingKey = ResolveSigningKey(token, environment);
                var audience = ResolveAudience(token, configuration);

                options.SaveToken = false;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero,
                    IssuerValidator = (issuer, securityToken, validationParameters) =>
                    {
                        var expected = ResolveIssuer(token, httpContextAccessor);
                        if (!string.Equals(issuer, expected, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new SecurityTokenInvalidIssuerException(
                                $"Issuer '{issuer}' is invalid. Expected '{expected}'.");
                        }

                        return issuer;
                    }
                };

            });
    }

    private static void ConfigureTokenOptions(
        IServiceCollection services,
        IConfiguration configuration,
        Action<TokenOption>? configureToken)
    {
        var tokenSection = configuration.GetSection("TokenOptions");
        if (tokenSection.Exists())
        {
            services.Configure<TokenOption>(tokenSection);
        }
        else
        {
            services.AddOptions<TokenOption>();
        }

        if (configureToken is not null)
        {
            services.PostConfigure(configureToken);
        }
    }

    private static string ResolveKeyMaterial(TokenOption settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.KeyPath))
        {
            if (!File.Exists(settings.KeyPath))
            {
                throw new FileNotFoundException("Token signing key file was not found.", settings.KeyPath);
            }

            return File.ReadAllText(settings.KeyPath);
        }

        if (!string.IsNullOrWhiteSpace(settings.Key))
        {
            return settings.Key;
        }

        throw new InvalidOperationException("Token signing key is missing.");
    }

    private static SecurityKey ResolveSigningKey(TokenOption settings, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(settings.CertificateThumbprint) ||
            !string.IsNullOrWhiteSpace(settings.CertificateSubjectName))
        {
            var certificate = LoadCertificate(settings);
            return new X509SecurityKey(certificate);
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Token signing certificate is required in production. Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
        }

        var keyMaterial = ResolveKeyMaterial(settings);
        return CreateSigningKey(keyMaterial);
    }

    private static SecurityKey CreateSigningKey(string keyMaterial)
    {
        var rsa = RSA.Create();

        if (keyMaterial.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsa.ImportFromPem(keyMaterial);
            return new RsaSecurityKey(rsa);
        }

        try
        {
            var keyBytes = Convert.FromBase64String(keyMaterial);
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return new RsaSecurityKey(rsa);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Token signing key must be PEM or base64-encoded RSA private key.", ex);
        }
    }

    private static X509Certificate2 LoadCertificate(TokenOption settings)
    {
        var storeName = Enum.TryParse(settings.CertificateStoreName, true, out StoreName parsedStore)
            ? parsedStore
            : StoreName.My;

        var storeLocation = Enum.TryParse(settings.CertificateStoreLocation, true, out StoreLocation parsedLocation)
            ? parsedLocation
            : StoreLocation.CurrentUser;

        using var store = new X509Store(storeName, storeLocation);

        store.Open(OpenFlags.ReadOnly);

        X509Certificate2Collection matches;

        var thumbprint = settings.CertificateThumbprint?.Replace(" ", string.Empty);

        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            matches = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' was not found.");
            }

            return matches[0];
        }

        var subjectName = settings.CertificateSubjectName?.Trim();

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new InvalidOperationException("Certificate thumbprint or subject name is required.");
        }

        matches = store.Certificates
            .Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"Certificate with subject name '{subjectName}' was not found.");
        }

        var candidate = matches
            .OfType<X509Certificate2>()
            .Where(cert => cert.HasPrivateKey)
            .OrderByDescending(cert => cert.NotAfter)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new InvalidOperationException($"No certificate with subject name '{subjectName}' has a private key.");
        }

        return candidate;
    }

    private static string ResolveIssuer(TokenOption settings, IHttpContextAccessor httpContextAccessor)
    {
        if (!string.IsNullOrWhiteSpace(settings.Issuer))
        {
            return settings.Issuer.TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request;

        if (request is null)
        {
            throw new InvalidOperationException("Token issuer is missing and no HTTP request is available to infer it.");
        }

        var baseUrl = $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase}";

        return baseUrl.TrimEnd('/');
    }

    private static string ResolveAudience(TokenOption settings, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(settings.Audience))
        {
            return settings.Audience;
        }

        var configuredAudience = configuration["TokenOptions:Audience"];

        if (!string.IsNullOrWhiteSpace(configuredAudience))
        {
            return configuredAudience;
        }

        throw new InvalidOperationException("Token audience is required.");
    }
}
