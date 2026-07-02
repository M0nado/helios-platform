#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publishes the MonadoBlade application.
.DESCRIPTION
    Builds and publishes the application in Release mode.
.PARAMETER OutputPath
    The output path for published files (default: ./publish)
.PARAMETER SelfContained
    If specified, publishes as self-contained
.PARAMETER Runtime
    The runtime identifier (default: win-x64)
#>

param(
    [string]$OutputPath = "./publish",
    [switch]$SelfContained,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "📦 MonadoBlade Publish Script" -ForegroundColor Cyan
Write-Host "Output: $OutputPath"
Write-Host "Runtime: $Runtime"
Write-Host "Self-Contained: $SelfContained"
Write-Host ""

Write-Host "🏗️  Building solution in Release mode..." -ForegroundColor Yellow
dotnet build MonadoBlade.sln --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📤 Publishing application..." -ForegroundColor Yellow

$publishArgs = @(
    "src/MonadoBlade.GUI/MonadoBlade.GUI.csproj",
    "--configuration", "Release",
    "--output", $OutputPath,
    "--runtime", $Runtime
)

if ($SelfContained) {
    $publishArgs += "--self-contained"
}

dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Publish completed successfully!" -ForegroundColor Green
Write-Host "📁 Output: $OutputPath" -ForegroundColor Green

$publishSize = (Get-ChildItem -Path $OutputPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "📊 Published size: $([Math]::Round($publishSize, 2)) MB" -ForegroundColor Gray
