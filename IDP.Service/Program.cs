using IDP.Server.ApplicationSetup;
using IDP.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddTokenTresorServices("Identity_DB", "admin.api");

//builder.AddTokenTresorServices(
//    connectionStringName: "DefaultConnection",
//    audience: "idp-api",
//    configureToken: options =>
//    {
//        options.Issuer = "https://idp.example.com";
//        options.KeyPath = "C:\\secrets\\signing-key.pem";
//    });

builder.Services.AddAuthorization();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors(policy => policy
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins("http://localhost:3000") // replace with your actual client URL
    .AllowCredentials()
);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.UseTokenTresor();

await app.RunAsync();
