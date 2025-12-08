# PowerShell Script to Fix Log File Permissions
# Run this as Administrator on the production server

param(
    [Parameter(Mandatory=$false)]
    [string]$LogPath = "C:\inetpub\wwwroot\Hondacx\Coupon\log.txt"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Fix Log File Permissions Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Running as Administrator" -ForegroundColor Green
Write-Host ""

# Get directory path
if ([System.IO.Path]::HasExtension($LogPath)) {
    $directory = Split-Path $LogPath -Parent
    $fileName = Split-Path $LogPath -Leaf
    Write-Host "Log File: $fileName" -ForegroundColor White
} else {
    $directory = $LogPath
    Write-Host "Log Folder: $directory" -ForegroundColor White
}

Write-Host "Directory: $directory" -ForegroundColor White
Write-Host ""

# Check if directory exists
if (-not (Test-Path $directory)) {
    Write-Host "Directory does not exist. Creating..." -ForegroundColor Yellow
    try {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        Write-Host "✓ Directory created successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create directory: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✓ Directory exists" -ForegroundColor Green
}

Write-Host ""
Write-Host "Setting permissions for IIS..." -ForegroundColor Cyan

# Set permissions for IIS_IUSRS
try {
    Write-Host "Setting permissions for IIS_IUSRS..." -ForegroundColor White
    icacls $directory /grant "IIS_IUSRS:(OI)(CI)M" /T
    Write-Host "✓ IIS_IUSRS permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set IIS_IUSRS permissions" -ForegroundColor Yellow
}

# Set permissions for NETWORK SERVICE
try {
    Write-Host "Setting permissions for NETWORK SERVICE..." -ForegroundColor White
    icacls $directory /grant "NETWORK SERVICE:(OI)(CI)M" /T
    Write-Host "✓ NETWORK SERVICE permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set NETWORK SERVICE permissions" -ForegroundColor Yellow
}

# Set permissions for IIS APPPOOL identity (if you know your app pool name)
$appPoolName = "DefaultAppPool"  # Change this if you use a different app pool
try {
    Write-Host "Setting permissions for IIS AppPool ($appPoolName)..." -ForegroundColor White
    icacls $directory /grant "IIS APPPOOL\${appPoolName}:(OI)(CI)M" /T
    Write-Host "✓ AppPool permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set AppPool permissions (this is optional)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing write permissions..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Test write permission
$testFile = Join-Path $directory "permission_test_$(Get-Date -Format 'yyyyMMddHHmmss').txt"
try {
    "Test write at $(Get-Date)" | Out-File -FilePath $testFile -Encoding UTF8
    Write-Host "✓ Write test successful" -ForegroundColor Green

    # Clean up test file
    Remove-Item $testFile -Force
    Write-Host "✓ Delete test successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Write test failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "You may need to manually set permissions using File Explorer:" -ForegroundColor Yellow
    Write-Host "1. Right-click the folder: $directory" -ForegroundColor Yellow
    Write-Host "2. Properties > Security > Edit" -ForegroundColor Yellow
    Write-Host "3. Add 'IIS_IUSRS' and 'NETWORK SERVICE' with Modify permissions" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ All permissions set successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open browser and navigate to: http://hondacx.me/Coupon/TestErrorLog.aspx" -ForegroundColor White
Write-Host "2. Click 'Check Config' to verify configuration" -ForegroundColor White
Write-Host "3. Click 'Test Write Log' to test ErrorLogger" -ForegroundColor White
Write-Host "4. Click 'Test File Write Permission' to verify permissions" -ForegroundColor White
Write-Host "5. Try redeeming a coupon to test real error logging" -ForegroundColor White
Write-Host ""
