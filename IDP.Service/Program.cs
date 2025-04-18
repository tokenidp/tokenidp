using IDP.Service.Middlewares;
using System.Text.Json.Serialization;

namespace IDP.Service;

public class Program
{
    protected Program() { }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationCore();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddServices(builder.Configuration);
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddAuthentication(builder.Configuration);

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            // Optional: make JSON output more readable in development
            options.JsonSerializerOptions.WriteIndented = true;
        });
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseHttpsRedirection();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseRouting();

        app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins("https://localhost:7217") // Replace with actual client URL
            .AllowCredentials());

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }
}