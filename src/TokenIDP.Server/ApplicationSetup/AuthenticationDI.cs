using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Server.ApplicationSetup;

internal static class AuthenticationDI
{
    internal static void AddAuthentication(this IServiceCollection services,
        TokenOptions tokenOptions,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var signingKey = ResolveSigningKey(tokenOptions, environment);
        var audience = TokenOptionsResolver.ResolveAudience(tokenOptions);
        var tenantResolutionOptions = configuration
            .GetSection(TenantResolutionOptions.SectionName)
            .Get<TenantResolutionOptions>() ?? new TenantResolutionOptions();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    IssuerValidator = (issuer, _, _) =>
                        ValidateIssuer(issuer, tokenOptions, tenantResolutionOptions, environment),
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddCookie("idp_session", options =>
            {
                options.Cookie.Name = "TokenTresor.Session";
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";

                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(30);

                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        //services.AddHttpClient("IDPClient", (serviceProvider, client) =>
        //{
        //    var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenOptions>>().Value;
        //    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        //    var issuer = ResolveIssuer(tokenOptions, httpContextAccessor);
        //    client.BaseAddress = new Uri(issuer);
        //});

        services.AddAntiforgery(options =>
        {
            // This tells the server to check this header name for the token
            options.HeaderName = "X-XSRF-TOKEN";
        });
    }

    private static SecurityKey ResolveSigningKey(TokenOptions settings, IHostEnvironment environment)
    {
        if (TokenSigningMaterialResolver.HasCertificateConfiguration(settings))
        {
            var certificate = TokenSigningMaterialResolver.LoadCertificate(settings);
            return new X509SecurityKey(certificate);
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Token signing certificate is required in production. Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
        }

        var keyMaterial = TokenSigningMaterialResolver.ResolveKeyMaterial(settings);
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

    private static string ValidateIssuer(
        string issuer,
        TokenOptions tokenOptions,
        TenantResolutionOptions tenantResolutionOptions,
        IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new SecurityTokenInvalidIssuerException("Token issuer is missing.");
        }

        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var parsedIssuer))
        {
            throw new SecurityTokenInvalidIssuerException("Token issuer is invalid.");
        }

        var normalizedIssuer = parsedIssuer.AbsoluteUri.TrimEnd('/');
        var configuredIssuer = TokenOptionsResolver.ResolveIssuer(tokenOptions);
        if (string.Equals(normalizedIssuer, configuredIssuer, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedIssuer;
        }

        if (environment.IsDevelopment() &&
            tenantResolutionOptions.AllowedDevelopmentHosts.Any(host =>
                string.Equals(parsedIssuer.Host, host, StringComparison.OrdinalIgnoreCase) ||
                parsedIssuer.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase)))
        {
            return normalizedIssuer;
        }

        if (tenantResolutionOptions.AllowedRootDomains.Any(domain =>
                parsedIssuer.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase)))
        {
            return normalizedIssuer;
        }

        throw new SecurityTokenInvalidIssuerException("Token issuer is not allowed.");
    }

}
