# HELIOS Full-Access Azure CLI Session Bootstrap
# Purpose: prepare a local HELIOS operator session for full read/write repo work and Azure CLI automation.
# Notes:
# - This script does not merge branches automatically. It validates tools, authenticates Azure CLI,
#   selects a subscription, and exports HELIOS_* session variables used by integration scripts.
# - Run from any HELIOS repository root or pass -WorkspaceRoot to scan sibling repositories.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$WorkspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$SubscriptionId = $env:HELIOS_AZURE_SUBSCRIPTION_ID,
    [string]$TenantId = $env:HELIOS_AZURE_TENANT_ID,
    [string]$Location = $(if ($env:HELIOS_LOCATION) { $env:HELIOS_LOCATION } else { "eastus2" }),
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = $(if ($env:HELIOS_ENVIRONMENT) { $env:HELIOS_ENVIRONMENT } else { "production" }),
    [switch]$UseDeviceCode,
    [switch]$PersistToUserProfile,
    [switch]$SkipLogin
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[HELIOS] $Message" -ForegroundColor Cyan
}

function Require-Command {
    param([string]$Name, [string]$InstallHint)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. $InstallHint"
    }
}

function Set-SessionVariable {
    param([string]$Name, [string]$Value)
    Set-Item -Path "Env:$Name" -Value $Value
    if ($PersistToUserProfile) {
        [Environment]::SetEnvironmentVariable($Name, $Value, "User")
    }
}

Write-Step "Validating tooling"
Require-Command git "Install Git for your platform, then rerun this script."
Require-Command az "Install Azure CLI from https://learn.microsoft.com/cli/azure/install-azure-cli, then rerun this script."

Write-Step "Configuring full read/write HELIOS session variables"
Set-SessionVariable -Name "HELIOS_SESSION_ACCESS_MODE" -Value "full-read-write"
Set-SessionVariable -Name "HELIOS_REPO_WRITE_ENABLED" -Value "true"
Set-SessionVariable -Name "HELIOS_BRANCH_INTEGRATION_MODE" -Value "merge-with-review"
Set-SessionVariable -Name "HELIOS_ENVIRONMENT" -Value $Environment
Set-SessionVariable -Name "HELIOS_LOCATION" -Value $Location

if (-not $SkipLogin) {
    Write-Step "Authenticating Azure CLI"
    $loginArgs = @("login")
    if ($UseDeviceCode) { $loginArgs += "--use-device-code" }
    if ($TenantId) { $loginArgs += @("--tenant", $TenantId) }
    & az @loginArgs | Out-Null
}

if ($SubscriptionId) {
    Write-Step "Selecting Azure subscription $SubscriptionId"
    & az account set --subscription $SubscriptionId
} else {
    Write-Step "No subscription supplied; using current Azure CLI account context"
}

$account = (& az account show --output json | ConvertFrom-Json)
Set-SessionVariable -Name "HELIOS_AZURE_SUBSCRIPTION_ID" -Value $account.id
Set-SessionVariable -Name "HELIOS_AZURE_TENANT_ID" -Value $account.tenantId

Write-Step "Discovering Git repositories under $WorkspaceRoot"
$repositories = Get-ChildItem -Path $WorkspaceRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { Test-Path (Join-Path $_.FullName ".git") } |
    ForEach-Object { $_.FullName } |
    Sort-Object -Unique

if ((Test-Path (Join-Path $WorkspaceRoot ".git")) -and ($repositories -notcontains $WorkspaceRoot)) {
    $repositories = @($WorkspaceRoot) + $repositories
}

foreach ($repo in $repositories) {
    Write-Host "`nRepository: $repo" -ForegroundColor Green
    git -C $repo status --short
    git -C $repo branch --all --no-color
}

Write-Host "`nHELIOS full read/write Azure CLI session is ready." -ForegroundColor Green
Write-Host "Review branch status above before merging. Use the integration runbook for safe merge order." -ForegroundColor Yellow
