# SetupLogFolders.ps1 - สคริปต์สำหรับตั้งค่า Log Folders และ Permissions
# ใช้สำหรับ production deployment

param(
    [Parameter(Mandatory=$false)]
    [string]$WebSitePath = "D:\Web Sites\wwwroot\Hondacx\Coupon",

    [Parameter(Mandatory=$false)]
    [switch]$Force
)

Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Coupon System - Log Folders Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ตรวจสอบว่า path มีอยู่จริงหรือไม่
if (-not (Test-Path $WebSitePath)) {
    Write-Host "❌ ERROR: Website path not found: $WebSitePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "กรุณาระบุ path ที่ถูกต้อง เช่น:" -ForegroundColor Yellow
    Write-Host ".\SetupLogFolders.ps1 -WebSitePath 'D:\Web Sites\wwwroot\Hondacx\Coupon'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Website path found: $WebSitePath" -ForegroundColor Green
Write-Host ""

# กำหนด folders ที่ต้องสร้าง
$folders = @{
    "App_Data" = "$WebSitePath\App_Data"
    "Logs" = "$WebSitePath\App_Data\Logs"
    "AuditLogs" = "$WebSitePath\App_Data\AuditLogs"
    "Barcode" = "$WebSitePath\Barcode"
}

# สร้าง folders
Write-Host "📁 Creating folders..." -ForegroundColor Cyan
Write-Host ""

foreach ($folder in $folders.GetEnumerator()) {
    $name = $folder.Key
    $path = $folder.Value

    if (Test-Path $path) {
        Write-Host "  ✓ $name already exists" -ForegroundColor Green
    } else {
        try {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
            Write-Host "  ✓ $name created successfully" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ Failed to create $name : $_" -ForegroundColor Red
        }
    }
}

Write-Host ""

# ตั้งค่า permissions
Write-Host "🔐 Setting permissions..." -ForegroundColor Cyan
Write-Host ""

$identities = @(
    "IIS_IUSRS",
    "NETWORK SERVICE",
    "IIS AppPool\DefaultAppPool"  # เปลี่ยนเป็น AppPool ที่ใช้งานจริง
)

foreach ($folder in $folders.GetEnumerator()) {
    $name = $folder.Key
    $path = $folder.Value

    if (-not (Test-Path $path)) {
        Write-Host "  ⚠ Skipping $name (folder not found)" -ForegroundColor Yellow
        continue
    }

    Write-Host "  Setting permissions for $name..." -ForegroundColor White

    foreach ($identity in $identities) {
        try {
            # ลบ permission เก่าถ้ามี (ถ้าใช้ -Force)
            if ($Force) {
                icacls $path /remove $identity 2>$null | Out-Null
            }

            # ให้สิทธิ์ Modify (M) แบบ inheritance
            $result = icacls $path /grant "${identity}:(OI)(CI)M" /T 2>&1

            if ($LASTEXITCODE -eq 0) {
                Write-Host "    ✓ $identity - OK" -ForegroundColor Green
            } else {
                Write-Host "    ⚠ $identity - WARNING (may not exist)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "    ❌ $identity - ERROR: $_" -ForegroundColor Red
        }
    }

    Write-Host ""
}

# ทดสอบการเขียนไฟล์
Write-Host "📝 Testing write permissions..." -ForegroundColor Cyan
Write-Host ""

$testResults = @()

foreach ($folder in @("Logs", "AuditLogs", "Barcode")) {
    $path = $folders[$folder]

    if (-not (Test-Path $path)) {
        Write-Host "  ⚠ Skipping $folder (folder not found)" -ForegroundColor Yellow
        continue
    }

    $testFile = Join-Path $path "test_write_$(Get-Date -Format 'yyyyMMddHHmmss').txt"

    try {
        "Test write at $(Get-Date)" | Out-File -FilePath $testFile -Encoding UTF8

        if (Test-Path $testFile) {
            Remove-Item $testFile -Force
            Write-Host "  ✓ $folder is WRITABLE" -ForegroundColor Green
            $testResults += @{ Folder = $folder; Status = "OK" }
        } else {
            Write-Host "  ❌ $folder is NOT WRITABLE" -ForegroundColor Red
            $testResults += @{ Folder = $folder; Status = "FAIL" }
        }
    } catch {
        Write-Host "  ❌ $folder is NOT WRITABLE: $_" -ForegroundColor Red
        $testResults += @{ Folder = $folder; Status = "FAIL" }
    }
}

Write-Host ""

# สร้าง test log files
Write-Host "📄 Creating sample log files..." -ForegroundColor Cyan
Write-Host ""

$today = Get-Date -Format "yyyyMMdd"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Error log
$errorLogPath = Join-Path $folders["Logs"] "Error_$today.log"
try {
    @"
================================================================================
ERROR LOG - Created by SetupLogFolders.ps1
================================================================================
Timestamp: $timestamp
Server: $env:COMPUTERNAME
User: $env:USERNAME

This is a sample error log file.
The system will append error messages to this file automatically.

================================================================================
"@ | Out-File -FilePath $errorLogPath -Encoding UTF8 -Append
    Write-Host "  ✓ Created: Error_$today.log" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Failed to create Error log: $_" -ForegroundColor Red
}

# Audit log
$auditLogPath = Join-Path $folders["AuditLogs"] "Audit_$today.log"
try {
    @"
================================================================================
AUDIT LOG - Created by SetupLogFolders.ps1
================================================================================
Timestamp: $timestamp
Server: $env:COMPUTERNAME
User: $env:USERNAME

This is a sample audit log file.
The system will log security events to this file automatically.

================================================================================
"@ | Out-File -FilePath $auditLogPath -Encoding UTF8 -Append
    Write-Host "  ✓ Created: Audit_$today.log" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Failed to create Audit log: $_" -ForegroundColor Red
}

Write-Host ""

# สรุปผลลัพธ์
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " SUMMARY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$allOK = $true
foreach ($result in $testResults) {
    if ($result.Status -eq "OK") {
        Write-Host "  ✓ $($result.Folder): READY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $($result.Folder): NOT READY" -ForegroundColor Red
        $allOK = $false
    }
}

Write-Host ""

if ($allOK) {
    Write-Host "✅ All folders are configured correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Access http://hondacx.me/Coupon/TestLogs.aspx to test logging" -ForegroundColor White
    Write-Host "  2. Try accessing a coupon page to verify system works" -ForegroundColor White
    Write-Host "  3. Check log files after each test" -ForegroundColor White
} else {
    Write-Host "⚠️ Some folders are not configured correctly." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please check:" -ForegroundColor Yellow
    Write-Host "  1. IIS Application Pool identity" -ForegroundColor White
    Write-Host "  2. Folder permissions" -ForegroundColor White
    Write-Host "  3. Run this script as Administrator" -ForegroundColor White
}

Write-Host ""
Write-Host "Log files location:" -ForegroundColor Cyan
Write-Host "  - Error logs: $($folders['Logs'])\Error_YYYYMMDD.log" -ForegroundColor White
Write-Host "  - Audit logs: $($folders['AuditLogs'])\Audit_YYYYMMDD.log" -ForegroundColor White
Write-Host ""

# แสดงคำสั่งสำหรับดู logs
Write-Host "Commands to view logs:" -ForegroundColor Cyan
Write-Host "  notepad `"$($folders['Logs'])\Error_$today.log`"" -ForegroundColor White
Write-Host "  notepad `"$($folders['AuditLogs'])\Audit_$today.log`"" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
