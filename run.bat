@echo off
title SMARTPOS v1.0 Launcher
color 0B
echo ========================================================
echo        SMARTPOS v1.0 - All-In-One Enterprise Launcher
echo ========================================================
echo.
echo [1/3] Starting SmartPOS Backend Web API...
start "SmartPOS API" cmd /k "dotnet run --project src\SmartPOS.API\SmartPOS.API.csproj"

echo [2/3] Starting SmartPOS Executive Web Admin Portal...
start "SmartPOS Web Portal" cmd /k "dotnet run --project src\SmartPOS.Web\SmartPOS.Web.csproj"

echo.
echo Waiting 4 seconds for services to warm up...
timeout /t 4 /nobreak >nul

echo Opening Executive Dashboard in Browser...
start http://localhost:5084

echo.
echo [3/3] Starting SmartPOS Cashier Desktop Terminal...
start "SmartPOS Cashier POS" cmd /k "dotnet run --project src\SmartPOS.Desktop\SmartPOS.Desktop.csproj"

echo.
echo ========================================================
echo   All SMARTPOS services have been launched successfully!
echo   - Web Admin Dashboard: http://localhost:5084
echo   - Backend Web API:     http://localhost:5000/swagger
echo   - Cashier Desktop:     WPF Window Active
echo ========================================================
echo.
pause
