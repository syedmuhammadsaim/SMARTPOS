# ========================================================
# SMARTPOS v1.0 - All-In-One Enterprise PowerShell Launcher
# ========================================================

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       SMARTPOS v1.0 - All-In-One Enterprise Launcher   " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/3] Compiling solution projects..." -ForegroundColor Green
dotnet build SmartPOS.slnx --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed! Please review errors above." -ForegroundColor Red
    pause
    exit
}
Write-Host "[OK] Solution compiled successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "[2/3] Starting SmartPOS Backend API & Web Portal..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --no-build --project src\SmartPOS.API\SmartPOS.API.csproj"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --no-build --project src\SmartPOS.Web\SmartPOS.Web.csproj"

Write-Host "Waiting for Web Portal to become active on port 5084..." -ForegroundColor DarkGray
while ($true) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 5084)
        $client.Close()
        break
    } catch {
        Start-Sleep -Milliseconds 800
    }
}

Write-Host "[OK] Web Portal is LIVE! Opening Chrome / Default Browser..." -ForegroundColor Cyan
Start-Process "http://localhost:5084"

Write-Host "[3/3] Starting SmartPOS Cashier Desktop Terminal (WPF)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --no-build --project src\SmartPOS.Desktop\SmartPOS.Desktop.csproj"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  All SMARTPOS services have been launched successfully! " -ForegroundColor Green
Write-Host "  - Web Admin Dashboard: http://localhost:5084          " -ForegroundColor White
Write-Host "  - Backend Web API:     http://localhost:5000/swagger  " -ForegroundColor White
Write-Host "  - Cashier Desktop:     WPF Window Active              " -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Cyan
