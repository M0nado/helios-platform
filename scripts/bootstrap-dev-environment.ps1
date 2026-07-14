<#
.SYNOPSIS
  Bootstraps HELIOS developer prerequisites for .NET, Azure CLI, GitHub CLI, Python, and optional AI tooling.
.DESCRIPTION
  Idempotent Windows setup script for the mixed C#/WinUI/C++/F#/Python HELIOS workspace. It prefers winget when
  available, validates installed tools, and can optionally log in to Azure and select a subscription without storing secrets.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$AzureSubscriptionId,
    [switch]$LoginAzure,
    [switch]$IncludeAiHubTools,
    [switch]$SkipWingetInstalls
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-Command {
    param([Parameter(Mandatory)][string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-LoggedCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    Write-Host "→ $FilePath $($Arguments -join ' ')" -ForegroundColor Cyan
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE: $FilePath $($Arguments -join ' ')"
    }
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$DisplayName
    )

    if ($SkipWingetInstalls) {
        Write-Warning "Skipping $DisplayName install because -SkipWingetInstalls was supplied."
        return
    }

    if (-not (Test-Command winget)) {
        throw "winget is required to install $DisplayName automatically. Install App Installer or rerun with -SkipWingetInstalls."
    }

    if ($PSCmdlet.ShouldProcess($DisplayName, "Install or upgrade via winget")) {
        Invoke-LoggedCommand winget @('install', '--id', $Id, '--exact', '--accept-package-agreements', '--accept-source-agreements', '--silent')
    }
}

$requiredTools = @(
    @{ Command = 'dotnet'; PackageId = 'Microsoft.DotNet.SDK.8'; Name = '.NET 8 SDK' },
    @{ Command = 'az'; PackageId = 'Microsoft.AzureCLI'; Name = 'Azure CLI' },
    @{ Command = 'git'; PackageId = 'Git.Git'; Name = 'Git' },
    @{ Command = 'gh'; PackageId = 'GitHub.cli'; Name = 'GitHub CLI' },
    @{ Command = 'python'; PackageId = 'Python.Python.3.12'; Name = 'Python 3.12' }
)

foreach ($tool in $requiredTools) {
    if (Test-Command $tool.Command) {
        Write-Host "✓ $($tool.Name) is already available." -ForegroundColor Green
        continue
    }

    Install-WingetPackage -Id $tool.PackageId -DisplayName $tool.Name
}

if ($IncludeAiHubTools) {
    if (-not (Test-Command pip)) {
        throw "pip is required for AI Hub Python tooling. Reopen the shell after Python installation and rerun this script."
    }

    Invoke-LoggedCommand python @('-m', 'pip', 'install', '--upgrade', 'pip')
    Invoke-LoggedCommand python @('-m', 'pip', 'install', '--upgrade', 'openai', 'azure-identity', 'azure-mgmt-resource')
}

if ($LoginAzure) {
    if (-not (Test-Command az)) {
        throw "Azure CLI is not available after bootstrap. Reopen the shell and rerun this script."
    }

    Invoke-LoggedCommand az @('login')

    if (-not [string]::IsNullOrWhiteSpace($AzureSubscriptionId)) {
        Invoke-LoggedCommand az @('account', 'set', '--subscription', $AzureSubscriptionId)
    }

    Invoke-LoggedCommand az @('account', 'show', '--output', 'table')
}

Write-Host 'HELIOS developer bootstrap completed.' -ForegroundColor Green
