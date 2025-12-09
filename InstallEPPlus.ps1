# PowerShell Script to Install EPPlus Package for Excel Support
# Run this script in the solution directory

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  EPPlus Package Installation Script" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# Check if running in correct directory
if (-not (Test-Path "Coupon.sln")) {
    Write-Host "ERROR: Coupon.sln not found!" -ForegroundColor Red
    Write-Host "Please run this script from the solution root directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 1: Checking for NuGet.exe..." -ForegroundColor Yellow

# Download NuGet.exe if not exists
$nugetPath = ".\nuget.exe"
if (-not (Test-Path $nugetPath)) {
    Write-Host "  NuGet.exe not found. Downloading..." -ForegroundColor Gray
    try {
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
        Write-Host "  ✓ NuGet.exe downloaded successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to download NuGet.exe" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "  ✓ NuGet.exe found" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 2: Installing EPPlus package..." -ForegroundColor Yellow

try {
    # Install EPPlus 4.5.3.3 (last LGPL version before license change)
    & $nugetPath install EPPlus -Version 4.5.3.3 -OutputDirectory packages -NonInteractive

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ EPPlus package installed successfully" -ForegroundColor Green
    }
    else {
        throw "NuGet install returned exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host "  ✗ Failed to install EPPlus" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "MANUAL INSTALLATION OPTION:" -ForegroundColor Yellow
    Write-Host "1. Open the solution in Visual Studio" -ForegroundColor Gray
    Write-Host "2. Right-click on the Coupon project → Manage NuGet Packages" -ForegroundColor Gray
    Write-Host "3. Search for 'EPPlus'" -ForegroundColor Gray
    Write-Host "4. Install version 4.5.3.3" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "Step 3: Updating packages.config..." -ForegroundColor Yellow

$packagesConfig = ".\Coupon\packages.config"
[xml]$xml = Get-Content $packagesConfig

# Check if EPPlus already exists
$epplusExists = $xml.packages.package | Where-Object { $_.id -eq "EPPlus" }

if (-not $epplusExists) {
    # Create new package element
    $newPackage = $xml.CreateElement("package")
    $newPackage.SetAttribute("id", "EPPlus")
    $newPackage.SetAttribute("version", "4.5.3.3")
    $newPackage.SetAttribute("targetFramework", "net472")

    # Add to packages
    $xml.packages.AppendChild($newPackage) | Out-Null

    # Save
    $xml.Save((Resolve-Path $packagesConfig))
    Write-Host "  ✓ packages.config updated" -ForegroundColor Green
}
else {
    Write-Host "  ℹ EPPlus already exists in packages.config" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Step 4: Updating Coupon.csproj..." -ForegroundColor Yellow

$csprojPath = ".\Coupon\Coupon.csproj"
[xml]$proj = Get-Content $csprojPath

# Find the ItemGroup with References
$refItemGroup = $proj.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1

# Check if EPPlus reference already exists
$epplusRef = $refItemGroup.Reference | Where-Object { $_.Include -like "EPPlus*" }

if (-not $epplusRef) {
    # Create EPPlus reference
    $reference = $proj.CreateElement("Reference", $proj.Project.NamespaceURI)
    $reference.SetAttribute("Include", "EPPlus, Version=4.5.3.3, Culture=neutral, PublicKeyToken=ea159fdaa78159a1, processorArchitecture=MSIL")

    $hintPath = $proj.CreateElement("HintPath", $proj.Project.NamespaceURI)
    $hintPath.InnerText = "..\packages\EPPlus.4.5.3.3\lib\net40\EPPlus.dll"
    $reference.AppendChild($hintPath) | Out-Null

    # Add to ItemGroup
    $refItemGroup.AppendChild($reference) | Out-Null

    # Save
    $proj.Save((Resolve-Path $csprojPath))
    Write-Host "  ✓ Coupon.csproj updated" -ForegroundColor Green
}
else {
    Write-Host "  ℹ EPPlus reference already exists in Coupon.csproj" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "  ✓ EPPlus Installation Complete!" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Close and reopen your Visual Studio solution" -ForegroundColor Gray
Write-Host "2. Build the solution (Ctrl+Shift+B)" -ForegroundColor Gray
Write-Host "3. The Excel import feature is now ready to use!" -ForegroundColor Gray
Write-Host ""
Write-Host "TESTING:" -ForegroundColor Yellow
Write-Host "- Navigate to: /Coupon/Admin/ImportCoupons.aspx" -ForegroundColor Gray
Write-Host "- Upload a .xlsx file to test Excel support" -ForegroundColor Gray
Write-Host ""
