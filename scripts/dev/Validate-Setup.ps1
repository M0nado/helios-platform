#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Non-mutating HELIOS/Hermes setup validation for Windows and Codespaces.
.DESCRIPTION
  Checks tool versions and login context only. It does not install packages,
  create resources, alter Azure state, or write repository configuration.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$failed = $false
$warned = $false

function Write-Pass($Message) { Write-Host "[PASS] $Message" -ForegroundColor Green }
function Write-Warn($Message) { $script:warned = $true; Write-Host "[WARN] $Message" -ForegroundColor Yellow }
function Write-Fail($Message) { $script:failed = $true; Write-Host "[FAIL] $Message" -ForegroundColor Red }
function Test-CommandVersion {
    param([string]$Label, [string]$Command, [scriptblock]$VersionCommand)
    if (Get-Command $Command -ErrorAction SilentlyContinue) {
        try {
            $value = (& $VersionCommand 2>&1 | Select-Object -First 3) -join ' '
            Write-Pass "${Label}: $value"
        } catch {
            Write-Warn "${Label} found, but version check failed: $($_.Exception.Message)"
        }
    } else {
        Write-Fail "$Label not found ($Command)"
    }
}

Write-Host "HELIOS/Hermes setup validation (non-mutating)" -ForegroundColor Cyan
Write-Host "Repository: $(Resolve-Path (Join-Path $PSScriptRoot '../..'))"
Write-Host ""

Test-CommandVersion '.NET SDK 8.x' 'dotnet' { dotnet --version }
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdks = dotnet --list-sdks 2>$null
    if ($sdks -match '^8\.') { Write-Pass '.NET 8 SDK is installed' } else { Write-Fail '.NET 8 SDK is required by HELIOS net8.0 and net8.0-windows projects' }
    try { dotnet fsi --help *> $null; Write-Pass 'F# interactive/tooling available through .NET SDK' } catch { Write-Warn "F# tooling not detected through 'dotnet fsi'" }
}

Test-CommandVersion 'PowerShell 7' 'pwsh' { $PSVersionTable.PSVersion.ToString() }
Test-CommandVersion 'Git' 'git' { git --version }
Test-CommandVersion 'GitHub CLI' 'gh' { gh --version }
Test-CommandVersion 'Azure CLI' 'az' { az version --output table }
if (Get-Command az -ErrorAction SilentlyContinue) {
    try {
        $extensions = az extension list --query '[].name' --output tsv 2>$null
        foreach ($extension in @('account', 'devops', 'ml', 'azure-iot', 'containerapp')) {
            if ($extensions -contains $extension) { Write-Pass "Azure CLI extension '$extension' installed" } else { Write-Warn "Recommended Azure CLI extension '$extension' is not installed" }
        }
    } catch { Write-Warn "Could not inspect Azure CLI extensions" }
    try { az account show *> $null; Write-Pass 'Azure CLI has an active login context' } catch { Write-Warn "Azure CLI is installed but not logged in; run 'az login' before deployment" }
}

Test-CommandVersion 'Docker' 'docker' { docker --version }
Test-CommandVersion 'Python 3.11+' 'python' { python --version }
if (Get-Command python -ErrorAction SilentlyContinue) {
    $pyOk = python -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 11) else 1)"
    if ($LASTEXITCODE -eq 0) { Write-Pass 'Python runtime is 3.11 or newer' } else { Write-Fail 'Python 3.11+ is recommended for AI Hub integration' }
}
Test-CommandVersion 'Node.js' 'node' { node --version }
Test-CommandVersion 'npm' 'npm' { npm --version }
Test-CommandVersion 'CMake' 'cmake' { cmake --version }
Test-CommandVersion 'Ninja' 'ninja' { ninja --version }
Test-CommandVersion 'MSBuild' 'msbuild' { msbuild -version }
Test-CommandVersion 'Visual Studio locator' 'vswhere' { vswhere -latest -property installationVersion }

if ($IsWindows) {
    $kitsRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\Lib'
    if (Test-Path $kitsRoot) { Write-Pass "Windows SDK detected at $kitsRoot" } else { Write-Fail 'Windows SDK not found; install Windows 11 SDK through Visual Studio Installer' }
    $windowsAppSdk = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk" -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
    if ($windowsAppSdk) { Write-Pass "Windows App SDK NuGet package cache detected: $($windowsAppSdk.Name)" } else { Write-Warn 'Windows App SDK package cache not found yet; restore WinUI projects before building UI components' }
} else {
    Write-Warn 'WinUI 3 and Windows App SDK builds require Windows 11/Server 2022 with Visual Studio Build Tools'
}

Write-Host ""
if ($failed) { Write-Fail 'Validation completed with missing required tools'; exit 1 }
if ($warned) { Write-Warn 'Validation completed with warnings'; exit 0 }
Write-Pass 'Validation completed successfully'
