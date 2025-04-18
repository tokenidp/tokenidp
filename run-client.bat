@echo off
REM Replace these paths with your actual project folders
set BLAZOR_PROJECT="D:\Case Studies\0-IdentityProvider\TenantBased\ClientWebApp\ClientWebApp.csproj"
set BLAZOR_PROFILE="https"

start "Blazor App" cmd /k "dotnet run --project %BLAZOR_PROJECT% --launch-profile %BLAZOR_PROFILE%"
