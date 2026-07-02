#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds the MonadoBlade solution in various configurations.
.DESCRIPTION
    This script handles the full build process including restore, build, and optional cleanup.
.PARAMETER Configuration
    The build configuration: Debug or Release (default: Debug)
.PARAMETER Clean
    If specified, performs a clean build
.PARAMETER Verbose
    If specified, shows detailed build output
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"

$solutionFile = "MonadoBlade.sln"
$buildLog = "build-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

Write-Host "🔨 MonadoBlade Build Script" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Clean: $Clean"
Write-Host ""

if ($Clean) {
    Write-Host "🧹 Cleaning build artifacts..." -ForegroundColor Yellow
    dotnet clean $solutionFile --configuration $Configuration 2>&1 | Tee-Object -FilePath $buildLog -Append
    Remove-Item -Path "./bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "./obj" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $solutionFile 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ NuGet restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🏗️  Building solution..." -ForegroundColor Yellow
$buildArgs = @($solutionFile, "--configuration", $Configuration)
if ($Verbose) {
    $buildArgs += "--verbosity", "detailed"
}

dotnet build @buildArgs 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host "📋 See $buildLog for details" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host "📋 Log saved to: $buildLog" -ForegroundColor Gray
