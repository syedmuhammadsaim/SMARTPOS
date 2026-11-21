@echo off
title SMARTPOS v1.0 Enterprise Launcher
color 0B
echo ========================================================
echo        SMARTPOS v1.0 - Enterprise All-In-One Launcher
echo ========================================================
echo.
echo [1/3] Compiling solution projects...
dotnet build SmartPOS.slnx --verbosity quiet
if %errorlevel% neq 0 (
    echo [ERROR] Build failed! Please review errors above.
    pause
    exit /b
)
echo [OK] Solution compiled successfully!
echo.

echo [2/3] Starting Backend Web API & Web Admin Portal...
start "SmartPOS Backend API (Port 5000)" cmd /k "dotnet run --no-build --project src\SmartPOS.API\SmartPOS.API.csproj"
start "SmartPOS Web Portal (Port 5084)" cmd /k "dotnet run --no-build --project src\SmartPOS.Web\SmartPOS.Web.csproj"

echo.
echo Waiting for Web Portal to become ready on port 5084...
:wait_port
powershell -NoProfile -Command "try { $c = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 5084); $c.Close(); exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% neq 0 (
    timeout /t 1 /nobreak >nul
    goto wait_port
)

echo.
echo [OK] Web Portal is LIVE! Opening Chrome / Default Browser...
start http://localhost:5084

echo.
echo [3/3] Launching Cashier Desktop Terminal...
start "SmartPOS Cashier Terminal (WPF)" cmd /k "dotnet run --no-build --project src\SmartPOS.Desktop\SmartPOS.Desktop.csproj"

echo.
echo ========================================================
echo   All SMARTPOS Enterprise modules are active!
echo   - Web Admin Dashboard: http://localhost:5084
echo   - Backend REST API:     http://localhost:5000/swagger
echo   - Cashier Desktop:     WPF Billing Window Active
echo ========================================================
echo.
pause
