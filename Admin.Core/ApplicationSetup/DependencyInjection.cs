using Admin.Core.Clients;
using Admin.Core.Configurations;
using Admin.Core.Lookups;
using Admin.Core.Roles;
using Admin.Core.Tenants;
using Admin.Core.Users;
using IDP.Common.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Admin.Core.ApplicationSetup;

internal static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services,
        IConfiguration configuration,
        Action<TokenOption>? configureToken)
    {
        AddInfrastructure(services);
        AddAdminServices(services);
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
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddAdminServices(IServiceCollection services)
    {
        services.AddScoped<RoleService>();
        services.AddScoped<ClientService>();
        services.AddScoped<TenantService>();
        services.AddScoped<AuditService>();
        services.AddScoped<ConfigurationService>();
        services.AddScoped<UserService>();
        services.AddScoped<LookupService>();
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
        }).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<TokenOption>, IHttpContextAccessor>((options, tokenOptions, httpContextAccessor) =>
            {
                var token = tokenOptions.Value;
                var keyMaterial = ResolveKeyMaterial(token);
                var signingKey = CreateSigningKey(keyMaterial);

                options.SaveToken = false;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
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
}