@echo off
REM Replace these paths with your actual project folders

set SERVICE_PROJECT="D:\Case Studies\0-IdentityProvider\TenantBased\IDP.Service\IDP.Service.csproj"
set SERVICE_PROFILE="Identity.Service"

start "Background Service" cmd /k "dotnet run --project %SERVICE_PROJECT% --launch-profile %SERVICE_PROFILE%"