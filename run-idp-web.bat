@echo off
REM Replace these paths with your actual project folders

set API_PROJECT="D:\Case Studies\0-IdentityProvider\TenantBased\IDP.Web\IDP.Web.csproj"
set API_PROFILE="https"

start "API Server" cmd /k "dotnet run --project %API_PROJECT% --launch-profile %API_PROFILE%"