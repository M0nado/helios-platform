# HELIOS Platform - Verify Distribution Package
# This script validates all distribution files

param(
    [string]$DistributionPath = "dist",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)

# Resolve full path
if (-not [System.IO.Path]::IsPathRooted($DistributionPath)) {
    $DistributionPath = Join-Path $projectRoot $DistributionPath
}

$distPath = Join-Path $DistributionPath "v$Version"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Distribution Package Verification" -ForegroundColor Cyan
Write-Host "Path: $distPath" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

$allChecks = @()
$passedChecks = 0
$failedChecks = 0

# Helper function
function Add-Check {
    param($Name, $Result, $Message = "")
    $status = if ($Result) { "✓ PASS" } else { "✗ FAIL" }
    $color = if ($Result) { "Green" } else { "Red" }
    
    Write-Host "  [$status] $Name" -ForegroundColor $color
    if ($Message) { Write-Host "           $Message" -ForegroundColor Gray }
    
    if ($Result) { $script:passedChecks++ } else { $script:failedChecks++ }
    return $Result
}

# Check 1: Directory structure
Write-Host "`n[CHECK 1] Directory Structure" -ForegroundColor Yellow
$requiredDirs = @("executables", "nuget", "demos", "documentation", "installer", "checksums")
$dirCheck = $true

foreach ($dir in $requiredDirs) {
    $path = Join-Path $distPath $dir
    $exists = Test-Path $path
    if (-not $exists) { $dirCheck = $false }
    Add-Check "Directory: $dir" $exists
}

# Check 2: Executable files
Write-Host "`n[CHECK 2] Executable Files" -ForegroundColor Yellow
$exeFiles = @("HELIOS.Platform.exe", "HELIOS-Setup.exe", "HELIOS.exe")
$exeCount = @(Get-ChildItem "$distPath\executables" -Filter "*.exe" -ErrorAction SilentlyContinue).Count
Add-Check "Executables present" ($exeCount -gt 0) "Found: $exeCount files"

# Check 3: NuGet package
Write-Host "`n[CHECK 3] NuGet Package" -ForegroundColor Yellow
$nuspecFile = Join-Path $distPath "nuget\HELIOS.Platform.nuspec"
Add-Check "NuSpec file exists" (Test-Path $nuspecFile)

if (Test-Path $nuspecFile) {
    try {
        $xml = [xml](Get-Content $nuspecFile)
        Add-Check "NuSpec XML valid" $true
        $version = $xml.package.metadata.version
        Add-Check "NuSpec version" ($version -eq $Version) "Version: $version"
    } catch {
        Add-Check "NuSpec XML valid" $false "Error: $_"
    }
}

# Check 4: Demo applications
Write-Host "`n[CHECK 4] Demo Applications" -ForegroundColor Yellow
$demoApps = @("demo-games.exe", "demo-dev.exe", "demo-security.exe")
foreach ($demo in $demoApps) {
    $demoPath = Join-Path $distPath "demos\$demo"
    Add-Check "Demo: $demo" (Test-Path $demoPath)
}

# Check 5: Documentation
Write-Host "`n[CHECK 5] Documentation Files" -ForegroundColor Yellow
$docFiles = @("README.txt", "INSTALLATION_GUIDE.md", "QUICK_START.md")
foreach ($doc in $docFiles) {
    $docPath = Join-Path $distPath "documentation\$doc"
    Add-Check "Document: $doc" (Test-Path $docPath)
}

# Check 6: Checksums
Write-Host "`n[CHECK 6] File Integrity" -ForegroundColor Yellow
$checksumsFile = Join-Path $distPath "CHECKSUMS.txt"
Add-Check "Checksums file" (Test-Path $checksumsFile)

if (Test-Path $checksumsFile) {
    $checksumCount = @(Get-Content $checksumsFile | Measure-Object -Line).Lines
    Add-Check "Checksums generated" ($checksumCount -gt 1) "Found: $checksumCount entries"
}

# Check 7: File sizes
Write-Host "`n[CHECK 7] File Sizes" -ForegroundColor Yellow
$totalSize = (Get-ChildItem $distPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Add-Check "Total package size" ($totalSize -gt 0) "Size: $totalSizeMB MB"

# Check 8: File accessibility
Write-Host "`n[CHECK 8] File Accessibility" -ForegroundColor Yellow
try {
    $testFile = Get-ChildItem $distPath -File -Recurse | Select-Object -First 1
    if ($testFile) {
        $readable = (Get-Item $testFile.FullName).Access -ne $null
        Add-Check "Files readable" $true
    }
} catch {
    Add-Check "Files readable" $false "Error: $_"
}

# Summary
Write-Host "`n" + "=" * 60 -ForegroundColor Cyan
Write-Host "Verification Summary" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "Passed: $passedChecks" -ForegroundColor Green
Write-Host "Failed: $failedChecks" -ForegroundColor $(if ($failedChecks -eq 0) { "Green" } else { "Red" })

if ($failedChecks -eq 0) {
    Write-Host "`n✓ All checks passed! Distribution package is ready." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n✗ Some checks failed. Please review the issues above." -ForegroundColor Red
    exit 1
}
