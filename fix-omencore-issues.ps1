# Fix OmenCore Issues - Run as Administrator
# This script stops conflicting services and provides diagnostics

Write-Host "================================" -ForegroundColor Cyan
Write-Host "OmenCore Diagnostic & Fix Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click this script and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit
}

Write-Host "[1/4] Checking for OMEN Gaming Hub services..." -ForegroundColor Yellow
$oghServices = Get-Service -Name "HPOmenCap","HPOmenCommandCenter" -ErrorAction SilentlyContinue
foreach ($svc in $oghServices) {
    Write-Host "   Found: $($svc.Name) - Status: $($svc.Status)" -ForegroundColor White
    if ($svc.Status -eq 'Running') {
        Write-Host "   Stopping $($svc.Name)..." -ForegroundColor Yellow
        Stop-Service -Name $svc.Name -Force -ErrorAction SilentlyContinue
        Set-Service -Name $svc.Name -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Host "   ✓ Stopped and disabled $($svc.Name)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "[2/4] Checking for Intel XTU services..." -ForegroundColor Yellow
$xtuServices = Get-Service -Name "XTU3SERVICE","XtuService","IntelXtuService" -ErrorAction SilentlyContinue
foreach ($svc in $xtuServices) {
    Write-Host "   Found: $($svc.Name) - Status: $($svc.Status)" -ForegroundColor White
    Write-Host "   Display Name: $($svc.DisplayName)" -ForegroundColor Gray
    if ($svc.Status -eq 'Running') {
        Write-Host "   Stopping $($svc.Name)..." -ForegroundColor Yellow
        Stop-Service -Name $svc.Name -Force -ErrorAction SilentlyContinue
        Set-Service -Name $svc.Name -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Host "   ✓ Stopped and disabled $($svc.Name)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "[3/4] Checking OGH processes..." -ForegroundColor Yellow
$oghProcs = Get-Process -Name "OmenCommandCenterBackground","OmenCap","omenmqtt" -ErrorAction SilentlyContinue
if ($oghProcs) {
    foreach ($proc in $oghProcs) {
        Write-Host "   Killing process: $($proc.Name) (PID: $($proc.Id))" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "   ✓ OGH processes terminated" -ForegroundColor Green
} else {
    Write-Host "   ✓ No OGH processes running" -ForegroundColor Green
}

Write-Host ""
Write-Host "[4/4] Final Status Check..." -ForegroundColor Yellow
Write-Host "Services:" -ForegroundColor Cyan
Get-Service -Name "HPOmenCap","HPOmenCommandCenter","XTU3SERVICE","XtuService","IntelXtuService" -ErrorAction SilentlyContinue | 
    Select-Object Name, Status, StartType | Format-Table -AutoSize

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "FIXES APPLIED!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Restart OmenCore application" -ForegroundColor White
Write-Host "2. For Keyboard RGB: Enable 'Experimental EC Keyboard' in Settings > Features" -ForegroundColor White
Write-Host "3. For Undervolting: Should now work without XTU conflict" -ForegroundColor White
Write-Host "4. For OGH Detection: Run cleanup again in Settings if needed" -ForegroundColor White
Write-Host ""
Write-Host "If keyboard RGB still doesn't work, check logs at:" -ForegroundColor Yellow
Write-Host "%LOCALAPPDATA%\OmenCore\logs\" -ForegroundColor Gray
Write-Host ""
pause
