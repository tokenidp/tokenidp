using Admin.Core;
using Identity.Application.Identity;
using Identity.Application.PowerBI;
using Identity.Infrastructure.Identity;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PowerBISetting>(configuration.GetSection("PowerBISettings"));

        services.AddMemoryCache();

        services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));

        services.AddHttpClient<IRestClient, HttpRestClient>("HttpRestClient")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Automatic,
            AutomaticDecompression = DecompressionMethods.GZip
                                        | DecompressionMethods.Deflate,
        });

        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<JwtTokenGenerator>();
        services.AddSingleton<JsonHelper>();

        services.AddScoped<IPowerBIService, PowerBIService>();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   configuration.GetConnectionString("DefaultConnection")));

        // For Identity
        services.AddIdentity<AppUser, AppRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSetting>(configuration.GetSection("JwtSettings"));

        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IAuthorization, AuthorizationService>();

        // Adding Authentication with Jwt Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = false;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidIssuer = configuration["JwtSettings:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]))
            };
            options.Events = new JwtBearerEvents()
            {
                OnAuthenticationFailed = c =>
                {
                    c.NoResult();
                    c.Response.StatusCode = 401;
                    c.Response.ContentType = "text/plain";
                    var result = JsonConvert.SerializeObject(new { error = "Authentication failed" });
                    if (!c.Response.HasStarted)
                    {
                        return c.Response.WriteAsync(result);
                    }
                    return Task.CompletedTask;
                },
                OnForbidden = c =>
                {
                    c.Response.StatusCode = 403; // Forbidden
                    c.Response.ContentType = "application/json";
                    var result = JsonConvert.SerializeObject(new { error = "Access denied" });
                    if (!c.Response.HasStarted)
                    {
                        return c.Response.WriteAsync(result);
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}