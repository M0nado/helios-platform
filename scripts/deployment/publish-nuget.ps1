# HELIOS Platform - Publish to NuGet
# This script publishes the NuGet package to NuGet.org

param(
    [string]$NuSpecPath,
    [string]$NuGetApiKey = $env:NUGET_API_KEY,
    [string]$Version = "1.0.0",
    [switch]$PreRelease,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "NuGet Package Publishing" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

# Validate inputs
if (-not $NuSpecPath) {
    $NuSpecPath = Join-Path $projectRoot "dist\v$Version\nuget\HELIOS.Platform.nuspec"
}

if (-not (Test-Path $NuSpecPath)) {
    Write-Host "✗ Error: NuSpec file not found at $NuSpecPath" -ForegroundColor Red
    exit 1
}

Write-Host "NuSpec: $NuSpecPath" -ForegroundColor Green

# Check for NuGet CLI
$nugetPath = (Get-Command nuget -ErrorAction SilentlyContinue).Source
if (-not $nugetPath) {
    Write-Host "! Installing NuGet CLI..." -ForegroundColor Yellow
    $nugetPath = Join-Path $env:TEMP "nuget.exe"
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
}

Write-Host "NuGet CLI: $nugetPath" -ForegroundColor Green

# Step 1: Validate NuSpec
Write-Host "`n[1/4] Validating NuSpec file..." -ForegroundColor Yellow
try {
    $xml = [xml](Get-Content $NuSpecPath)
    Write-Host "  ✓ NuSpec XML is valid"
    
    $id = $xml.package.metadata.id
    $pkgVersion = $xml.package.metadata.version
    Write-Host "  ✓ Package ID: $id"
    Write-Host "  ✓ Package Version: $pkgVersion"
} catch {
    Write-Host "  ✗ Error reading NuSpec: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Pack the NuGet package
Write-Host "`n[2/4] Creating NuGet package..." -ForegroundColor Yellow
$packageDir = Split-Path -Parent $NuSpecPath
$packageName = "$packageDir\HELIOS.Platform.$Version.nupkg"

$packArgs = @(
    "pack",
    $NuSpecPath,
    "-Version", $Version,
    "-OutputDirectory", $packageDir,
    "-NoPackageAnalysis"
)

if ($PreRelease) {
    $packArgs += "-Suffix", "pre"
    Write-Host "  ! Creating pre-release package"
}

if ($DryRun) {
    Write-Host "  [DRY-RUN] Would execute: nuget $($packArgs -join ' ')"
} else {
    & $nugetPath $packArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Package created: $packageName"
    } else {
        Write-Host "  ✗ Error creating package" -ForegroundColor Red
        exit 1
    }
}

# Step 3: Validate API key
Write-Host "`n[3/4] Validating NuGet API key..." -ForegroundColor Yellow
if (-not $NuGetApiKey) {
    Write-Host "  ✗ Error: NUGET_API_KEY environment variable not set" -ForegroundColor Red
    Write-Host "  ! Set the variable: `$env:NUGET_API_KEY = 'your-api-key'" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ API key found (length: $($NuGetApiKey.Length) chars)"

# Step 4: Push to NuGet
Write-Host "`n[4/4] Publishing to NuGet.org..." -ForegroundColor Yellow

if (Test-Path $packageName) {
    $pushArgs = @(
        "push",
        $packageName,
        $NuGetApiKey,
        "-Source", "https://api.nuget.org/v3/index.json"
    )
    
    if ($DryRun) {
        Write-Host "  [DRY-RUN] Would execute: nuget push $packageName [API-KEY]"
    } else {
        & $nugetPath $pushArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Package published successfully!"
        } else {
            Write-Host "  ✗ Error publishing package" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "  ✗ Package file not found: $packageName" -ForegroundColor Red
    exit 1
}

# Final summary
Write-Host "`n" + "=" * 60 -ForegroundColor Green
Write-Host "Publishing Complete!" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Green
Write-Host "Package: HELIOS.Platform v$Version" -ForegroundColor Green
Write-Host "Status: Published to NuGet.org" -ForegroundColor Green
Write-Host "`nInstall with:"
Write-Host "  nuget install HELIOS.Platform -Version $Version" -ForegroundColor Cyan

exit 0
