using TokenIDP.Core.Foundation.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Server.ApplicationSetup;

internal static class AuthenticationDI
{
    internal static void AddAuthentication(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var services = context.HttpContext.RequestServices;

                        var tokenOptions = services
                            .GetRequiredService<IOptions<TokenOption>>().Value;

                        var httpContextAccessor = services
                            .GetRequiredService<IHttpContextAccessor>();

                        var signingKey = ResolveSigningKey(tokenOptions, environment);
                        var audience = ResolveAudience(tokenOptions, configuration);

                        context.Options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = signingKey,
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidAudience = audience,
                            ClockSkew = TimeSpan.Zero,
                            IssuerValidator = (issuer, token, parameters) =>
                            {
                                var expected = ResolveIssuer(tokenOptions, httpContextAccessor);

                                if (!string.Equals(issuer, expected, StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new SecurityTokenInvalidIssuerException(
                                        $"Issuer '{issuer}' is invalid. Expected '{expected}'.");
                                }

                                return issuer;
                            }
                        };

                        return Task.CompletedTask;
                    }
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
        //    var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenOption>>().Value;
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

    private static SecurityKey ResolveSigningKey(TokenOption settings, IHostEnvironment environment)
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
