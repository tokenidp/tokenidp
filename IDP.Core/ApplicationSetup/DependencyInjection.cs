using IDP.Core.Admin;
using IDP.Core.Admin.Clients;
using IDP.Core.Admin.Configurations;
using IDP.Core.Admin.Roles;
using IDP.Core.Admin.Tenants;
using IDP.Core.Admin.Users;
using IDP.Core.Application;
using IDP.Core.Common.Interfaces;
using IDP.Core.Common.Notifications;
using IDP.Core.Domain.AggregateRoots.Roles;
using IDP.Core.Options;
using IDP.Core.TokenServices;
using IDP.Core.TokenServices.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDP.Core.ApplicationSetup;

internal static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services,
        IConfiguration configuration,
        Action<TokenOption>? configureToken)
    {
        services.Configure<ConfigurationOption>(configuration.GetSection("ConfigurationSettings"));
        var tokenSection = configuration.GetSection("TokenSettings");
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

        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddCors();

        services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<JwtTokenGenerator>();
        services.AddSingleton<JsonHelper>();

        services.AddScoped<RoleService>();
        services.AddScoped<ClientService>();
        services.AddScoped<TenantService>();
        services.AddScoped<AuditService>();
        services.AddScoped<ConfigurationService>();
        services.AddScoped<UserService>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IdentityService>();
        services.AddScoped<TokenValidatorService>();   
        services.AddScoped<AccessTokenUseCase>();
        services.AddScoped<AuthenticationUseCase>();
        services.AddScoped<MfaService>();
        services.AddScoped<TokenServiceFactory>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<AccessTokenService>();

        services.AddScoped<IReferenceTokenValidator, ReferenceTokenService>();

        services.AddTransient<Func<TokenType, ITokenService>>(serviceProvider => key =>
        {
            #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
            #pragma warning disable CS8604 // Possible null reference argument
            ITokenService service = key switch
            {
                TokenType.ReferenceToken => serviceProvider.GetService<ReferenceTokenService>(),
                TokenType.JWT => serviceProvider.GetService<AccessTokenService>(),
                _ => serviceProvider.GetService<AccessTokenService>()
            };

            if (service == null)
            {
                throw new InvalidOperationException($"Service for key '{key}' is not registered.");
            }

            return service;
        });

        services.AddScoped<SmtpEmail>();
        services.AddScoped<SendGridEmail>();
        services.AddScoped<EmailProviderFactory>();
        services.AddScoped<IEmailSetting, EmailSetting>();

        services.AddTransient<Func<EmailProviderType, IEmailNotification>>(serviceProvider => key =>
        {
            #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
            #pragma warning disable CS8604 // Possible null reference argument
            IEmailNotification service = key switch
            {
                EmailProviderType.SendGrid => serviceProvider.GetService<SendGridEmail>(),
                _ => serviceProvider.GetService<SmtpEmail>()
            };

            if (service == null)
            {
                throw new InvalidOperationException($"Service for key '{key}' is not registered.");
            }

            return service;
        });
    }

    public static void AddPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
                  options.UseSqlServer(
                      configuration.GetConnectionString("Identity_DB")));

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<AuthorizationRepo>();
        services.AddScoped<PreAuthorizationRepo>();
        services.AddScoped<LookupRepo>();
    }

    //public static void AddAuthentication(this IServiceCollection services,
    //    IConfiguration configuration)
    //{
    //    services.Configure<TokenOption>(configuration.GetSection("TokenSettings"));

    //    services.AddAuthentication(options =>
    //    {
    //        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    //    })
    //   .AddJwtBearer(options =>
    //   {
    //       options.SaveToken = false;
    //       options.RequireHttpsMetadata = false;
    //       options.TokenValidationParameters = new TokenValidationParameters()
    //       {
    //           ValidateIssuerSigningKey = true,
    //           ValidateIssuer = true,
    //           ValidateAudience = true,
    //           ValidateLifetime = true,
    //           ClockSkew = TimeSpan.Zero,
    //           ValidAudience = configuration["TokenSettings:Audience"],
    //           ValidIssuer = configuration["TokenSettings:Issuer"],
    //           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["TokenSettings:Key"]))
    //       };
    //   });

    //    // services.AddAuthentication("Bearer")
    //    //.AddOAuth2Introspection("Bearer", options =>
    //    //{
    //    //    options.Authority = "https://authserver.com";
    //    //    options.ClientId = "your-client-id";
    //    //    options.ClientSecret = "your-client-secret";
    //    //});

    //    //services.AddAuthorization(options =>
    //    //{
    //    //    options.AddPolicy("Profile", policy =>
    //    //        policy.RequireClaim("scope", "Profile"));
    //    //});
    //}
}