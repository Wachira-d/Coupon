# PowerShell Script to Fix Barcode Folder Permissions
# Run this as Administrator on the production server

param(
    [Parameter(Mandatory=$false)]
    [string]$WebSitePath = "C:\inetpub\wwwroot\Hondacx\Coupon"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Fix Barcode Folder Permissions Script" -ForegroundColor Cyan
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

# Barcode folder path
$barcodePath = Join-Path $WebSitePath "Barcode"
Write-Host "Barcode folder: $barcodePath" -ForegroundColor White
Write-Host ""

# Check if folder exists
if (-not (Test-Path $barcodePath)) {
    Write-Host "Barcode folder does not exist. Creating..." -ForegroundColor Yellow
    try {
        New-Item -ItemType Directory -Path $barcodePath -Force | Out-Null
        Write-Host "✓ Barcode folder created successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create Barcode folder: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✓ Barcode folder exists" -ForegroundColor Green
}

Write-Host ""
Write-Host "Setting permissions for IIS..." -ForegroundColor Cyan

# Set permissions for IIS_IUSRS
try {
    Write-Host "Setting permissions for IIS_IUSRS..." -ForegroundColor White
    icacls $barcodePath /grant "IIS_IUSRS:(OI)(CI)M" /T
    Write-Host "✓ IIS_IUSRS permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set IIS_IUSRS permissions" -ForegroundColor Yellow
}

# Set permissions for NETWORK SERVICE
try {
    Write-Host "Setting permissions for NETWORK SERVICE..." -ForegroundColor White
    icacls $barcodePath /grant "NETWORK SERVICE:(OI)(CI)M" /T
    Write-Host "✓ NETWORK SERVICE permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set NETWORK SERVICE permissions" -ForegroundColor Yellow
}

# Set permissions for IIS APPPOOL identity
$appPoolName = "DefaultAppPool"  # Change this if you use a different app pool
try {
    Write-Host "Setting permissions for IIS AppPool ($appPoolName)..." -ForegroundColor White
    icacls $barcodePath /grant "IIS APPPOOL\${appPoolName}:(OI)(CI)M" /T
    Write-Host "✓ AppPool permissions set" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Failed to set AppPool permissions (this is optional)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing write permissions..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Test write permission
$testFile = Join-Path $barcodePath "permission_test_$(Get-Date -Format 'yyyyMMddHHmmss').jpg"
try {
    # Create a simple test image
    [System.IO.File]::WriteAllBytes($testFile, [byte[]](0xFF, 0xD8, 0xFF))
    Write-Host "✓ Write test successful" -ForegroundColor Green

    # Clean up test file
    Remove-Item $testFile -Force
    Write-Host "✓ Delete test successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Write test failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "You may need to manually set permissions using File Explorer:" -ForegroundColor Yellow
    Write-Host "1. Right-click the folder: $barcodePath" -ForegroundColor Yellow
    Write-Host "2. Properties > Security > Edit" -ForegroundColor Yellow
    Write-Host "3. Add 'IIS_IUSRS' and 'NETWORK SERVICE' with Modify permissions" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Checking existing barcode files..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$barcodeFiles = Get-ChildItem -Path $barcodePath -Filter "*.jpg" -ErrorAction SilentlyContinue
if ($barcodeFiles) {
    Write-Host "Found $($barcodeFiles.Count) existing barcode files" -ForegroundColor White

    # Show most recent 5 files
    $recentFiles = $barcodeFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 5
    foreach ($file in $recentFiles) {
        Write-Host "  - $($file.Name) (Modified: $($file.LastWriteTime))" -ForegroundColor Gray
    }
} else {
    Write-Host "No existing barcode files found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ All permissions set successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Try redeeming a coupon" -ForegroundColor White
Write-Host "2. Check if barcode image is created in: $barcodePath" -ForegroundColor White
Write-Host "3. If errors occur, check logs at: http://hondacx.me/Coupon/ViewErrorLogs.aspx" -ForegroundColor White
Write-Host ""
