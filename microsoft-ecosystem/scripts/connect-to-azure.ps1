# PowerShell: Connect to Azure
# Purpose: Authenticate to Azure with Azure CLI and PowerShell Az validation, select a subscription, and persist HELIOS context.
# Version: 2.0.0

[CmdletBinding()]
param(
    [ValidateSet("interactive", "device-code", "managed-identity", "service-principal")]
    [string]$AuthMethod = "interactive",
    [string]$EnvironmentName = "AzureCloud",
    [string]$TenantId,
    [string]$SubscriptionId,
    [string]$SubscriptionName,
    [string]$ClientId = $env:AZURE_CLIENT_ID,
    [string]$ClientSecret = $env:AZURE_CLIENT_SECRET,
    [string]$ConfigPath = (Join-Path $HOME ".helios/azure.json"),
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"
$ErrorColor = "Red"; $SuccessColor = "Green"; $InfoColor = "Cyan"; $WarningColor = "Yellow"
function Write-ColorOutput { param([string]$Message, [string]$Color = "White") Write-Host $Message -ForegroundColor $Color }
function Fail-WithRemediation { param([string]$Message) Write-ColorOutput $Message $ErrorColor; exit 1 }

function Test-AzurePrerequisites {
    Write-ColorOutput "Checking Azure CLI ('az')..." $InfoColor
    $az = Get-Command az -ErrorAction SilentlyContinue
    if (-not $az) {
        Fail-WithRemediation "Azure CLI 'az' was not found. Install it from https://learn.microsoft.com/cli/azure/install-azure-cli, restart your shell, then run 'az --version'."
    }
    & az --version | Select-Object -First 1 | ForEach-Object { Write-ColorOutput "Found $_" $SuccessColor }

    Write-ColorOutput "Checking PowerShell Az.Accounts module..." $InfoColor
    if (-not (Get-Module -ListAvailable -Name Az.Accounts)) {
        Fail-WithRemediation "PowerShell Az.Accounts module was not found. Install with: Install-Module Az.Accounts -Scope CurrentUser -Force -AllowClobber"
    }
    Import-Module Az.Accounts -ErrorAction Stop
    Write-ColorOutput "PowerShell Az.Accounts module found." $SuccessColor
}

Test-AzurePrerequisites
if ($ValidateOnly) { Write-ColorOutput "Azure prerequisites validated." $SuccessColor; exit 0 }

Write-ColorOutput "Selecting Azure cloud: $EnvironmentName" $InfoColor
& az cloud set --name $EnvironmentName | Out-Null

Write-ColorOutput "Authenticating to Azure using '$AuthMethod'..." $InfoColor
switch ($AuthMethod) {
    "interactive" { if ($TenantId) { & az login --tenant $TenantId | Out-Null } else { & az login | Out-Null } }
    "device-code" { if ($TenantId) { & az login --use-device-code --tenant $TenantId | Out-Null } else { & az login --use-device-code | Out-Null } }
    "managed-identity" { & az login --identity | Out-Null }
    "service-principal" {
        if (-not $ClientId -or -not $ClientSecret -or -not $TenantId) { Fail-WithRemediation "Service principal auth requires -ClientId, -ClientSecret, and -TenantId (or AZURE_CLIENT_ID/AZURE_CLIENT_SECRET)." }
        & az login --service-principal --username $ClientId --password $ClientSecret --tenant $TenantId | Out-Null
    }
}
if ($LASTEXITCODE -ne 0) { Fail-WithRemediation "Azure CLI login failed. Re-run with -AuthMethod device-code on headless systems or verify credentials." }

$subscriptionSelector = if ($SubscriptionId) { $SubscriptionId } elseif ($SubscriptionName) { $SubscriptionName } else { $null }
if ($subscriptionSelector) {
    Write-ColorOutput "Selecting subscription '$subscriptionSelector' with az account set..." $InfoColor
    & az account set --subscription $subscriptionSelector
    if ($LASTEXITCODE -ne 0) { Fail-WithRemediation "Unable to set subscription '$subscriptionSelector'. Use 'az account list --output table' to verify available subscriptions." }
}

$accountJson = & az account show --output json
if ($LASTEXITCODE -ne 0 -or -not $accountJson) { Fail-WithRemediation "Unable to validate Azure account. Run 'az account show --output table' for details." }
$account = $accountJson | ConvertFrom-Json
if ($TenantId -and $account.tenantId -ne $TenantId) { Fail-WithRemediation "Active tenant '$($account.tenantId)' does not match requested tenant '$TenantId'." }

$env:HELIOS_AZURE_SUBSCRIPTION_ID = $account.id
$env:HELIOS_AZURE_TENANT_ID = $account.tenantId
$env:HELIOS_ENVIRONMENT = "production"
$env:HELIOS_LOCATION = "eastus2"

$config = [ordered]@{
    TenantId = $account.tenantId
    SubscriptionId = $account.id
    SubscriptionName = $account.name
    AccountName = $account.user.name
    EnvironmentName = $EnvironmentName
    AuthMethod = switch ($AuthMethod) { "device-code" { "DeviceFlow" } "interactive" { "Interactive" } "managed-identity" { "ManagedIdentity" } "service-principal" { "ServicePrincipal" } }
}
$configDirectory = Split-Path -Parent $ConfigPath
if ($configDirectory) { New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null }
$config | ConvertTo-Json -Depth 4 | Set-Content -Path $ConfigPath -Encoding UTF8

Write-ColorOutput "`nCurrent Azure CLI Context:" $InfoColor
& az account show --output table
Write-ColorOutput "`nCurrent PowerShell Az Context:" $InfoColor
Connect-AzAccount -Tenant $account.tenantId -Subscription $account.id -Environment $EnvironmentName | Out-Null
Get-AzContext | Format-List Account, SubscriptionName, SubscriptionId, Tenant
Write-ColorOutput "`nAzure connection established and HELIOS configuration saved to $ConfigPath" $SuccessColor
