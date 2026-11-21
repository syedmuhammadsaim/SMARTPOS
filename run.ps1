# ========================================================
# SMARTPOS v1.0 - All-In-One Enterprise PowerShell Launcher
# ========================================================

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       SMARTPOS v1.0 - All-In-One Enterprise Launcher   " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/3] Starting SmartPOS Backend Web API (Port 5000)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project src\SmartPOS.API\SmartPOS.API.csproj"

Write-Host "[2/3] Starting SmartPOS Executive Web Portal (Port 5084)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project src\SmartPOS.Web\SmartPOS.Web.csproj"

Write-Host "Waiting 4 seconds for web services to initialize..." -ForegroundColor DarkGray
Start-Sleep -Seconds 4

Write-Host "Opening Executive Dashboard in Chrome / Default Browser..." -ForegroundColor Cyan
Start-Process "http://localhost:5084"

Write-Host "[3/3] Starting SmartPOS Cashier Desktop Terminal (WPF)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project src\SmartPOS.Desktop\SmartPOS.Desktop.csproj"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  All SMARTPOS services have been launched successfully! " -ForegroundColor Green
Write-Host "  - Web Admin Dashboard: http://localhost:5084          " -ForegroundColor White
Write-Host "  - Backend Web API:     http://localhost:5000/swagger  " -ForegroundColor White
Write-Host "  - Cashier Desktop:     WPF Window Active              " -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Cyan
