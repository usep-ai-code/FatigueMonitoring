@echo off
REM Build script for Azure App Service deployment
REM This script builds the React frontend and copies it to the ASP.NET Core wwwroot folder

echo ==========================================
echo Building Fatigue Monitoring for Azure
echo ==========================================

REM Step 1: Build React Frontend
echo.
echo [1/3] Building React Frontend...
cd fatigue-monitoring.react
call npm install
call npm run build
cd ..

REM Step 2: Create wwwroot folder in backend if it doesn't exist
echo.
echo [2/3] Copying React build to ASP.NET Core wwwroot...
if not exist "FatigueMonitoring.Web.Api\wwwroot" mkdir "FatigueMonitoring.Web.Api\wwwroot"

REM Step 3: Copy React build output to wwwroot
xcopy /E /Y /I "fatigue-monitoring.react\dist\*" "FatigueMonitoring.Web.Api\wwwroot\"

echo.
echo [3/3] Build complete!
echo ==========================================
echo React frontend has been copied to:
echo   FatigueMonitoring.Web.Api\wwwroot\
echo.
echo Next steps for Azure deployment:
echo 1. Publish the ASP.NET Core project to Azure App Service
echo 2. Or use: dotnet publish -c Release
echo ==========================================
