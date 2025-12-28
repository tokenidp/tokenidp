using IDP.Core.ApplicationSetup;

var builder = WebApplication.CreateBuilder(args);

builder.AddTokenTresorServices("Identity_DB");

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseHttpsRedirection();

app.UseTokenTresor();

app.UseRouting();

app.UseCors(policy => policy
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins("https://localhost:7217") // replace with your actual client URL
    .AllowCredentials()
);

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();
