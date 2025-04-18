using Identity.Infrastructure;
using Identity.Service.Security;

namespace Identity.Services;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplication();

        services.AddInfrastructure(Configuration);

        services.AddPersistence(Configuration);

        services.AddJwtAuthentication(Configuration);

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        // Register custom policy provider and handler
        services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, DynamicRolePolicyHandler>();

        services.AddControllers();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity.Services", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                  new OpenApiSecurityScheme
                  {
                    Reference = new OpenApiReference
                      {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                      },
                      Scheme = "oauth2",
                      Name = "Bearer",
                      In = ParameterLocation.Header
                  },
                    new List<string>()
                }
            });
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAppLogger<Startup> _loggrer)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.Services v1"));

        app.UseHttpsRedirection();

        LogAppConfiguration(env, _loggrer);

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseRouting();

        // global cors policy
        app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true) // allow any origin
            .AllowCredentials()); // allow credentials

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    private void LogAppConfiguration(IWebHostEnvironment env, IAppLogger<Startup> _loggrer)
    {
        var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json");

        var keys = builder.Build().AsEnumerable().ToList();

        _loggrer.LogInfo("Following are the system configuration: {0}", keys);
    }
}
