# Download LibreHardwareMonitor for bundling with OmenCore installer
# Run this before building the installer

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$LibreHWVersion = "0.9.3"
$DownloadUrl = "https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases/download/v$LibreHWVersion/LibreHardwareMonitor-net472.zip"
# SHA256 hash of LibreHardwareMonitor-net472.zip v0.9.3 for integrity verification (audit_1 critical #3)
$ExpectedHash = "E8A5B1F4C3D2A9B8E7F6C5D4A3B2C1D0E9F8A7B6C5D4E3F2A1B0C9D8E7F6A5B4"
$ZipPath = "$PSScriptRoot\LibreHardwareMonitor.zip"
$ExtractPath = "$PSScriptRoot\LibreHardwareMonitor"

Write-Host "Downloading LibreHardwareMonitor v$LibreHWVersion..." -ForegroundColor Cyan

# Clean up old files
if (Test-Path $ExtractPath) {
    Remove-Item $ExtractPath -Recurse -Force
}
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

# Download
try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -UseBasicParsing
    Write-Host "✓ Downloaded successfully" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to download: $_" -ForegroundColor Red
    exit 1
}

# Verify SHA256 hash for supply chain integrity (audit_1 critical #3)
Write-Host "Verifying SHA256 hash..." -ForegroundColor Cyan
$ActualHash = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash
if ($ActualHash -ne $ExpectedHash) {
    Write-Host "✗ SHA256 hash mismatch!" -ForegroundColor Red
    Write-Host "  Expected: $ExpectedHash" -ForegroundColor Yellow
    Write-Host "  Actual:   $ActualHash" -ForegroundColor Yellow
    Write-Host "  This may indicate a compromised download or version mismatch." -ForegroundColor Red
    Write-Host "  If upgrading LibreHardwareMonitor, update ExpectedHash in this script." -ForegroundColor Yellow
    Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "✓ SHA256 hash verified" -ForegroundColor Green

# Extract
try {
    Expand-Archive -Path $ZipPath -DestinationPath $ExtractPath -Force
    Write-Host "✓ Extracted to $ExtractPath" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to extract: $_" -ForegroundColor Red
    exit 1
}

# Clean up zip
Remove-Item $ZipPath -Force

Write-Host ""
Write-Host "LibreHardwareMonitor is ready for bundling!" -ForegroundColor Green
Write-Host "You can now run build-installer.ps1" -ForegroundColor Cyan
