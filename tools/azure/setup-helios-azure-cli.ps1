<#
.SYNOPSIS
    Bootstrap Azure CLI tooling for HELIOS Hermes XCore development and deployment.

.DESCRIPTION
    Verifies Azure CLI availability, installs required extensions, configures defaults,
    and optionally creates the HELIOS resource group used by integration environments.
    The script is intentionally idempotent and does not store credentials or secrets.

.EXAMPLE
    ./tools/azure/setup-helios-azure-cli.ps1 -SubscriptionId '<subscription-id>' -TenantId '<tenant-id>' -CreateResourceGroup
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SubscriptionId = $env:AZURE_SUBSCRIPTION_ID,
    [string]$TenantId = $env:AZURE_TENANT_ID,
    [string]$Location = $(if ($env:HELIOS_AZURE_LOCATION) { $env:HELIOS_AZURE_LOCATION } else { 'eastus2' }),
    [string]$ResourceGroupName = $(if ($env:HELIOS_RESOURCE_GROUP) { $env:HELIOS_RESOURCE_GROUP } else { 'helios-hermes-xcore-rg' }),
    [string]$EnvironmentName = $(if ($env:HELIOS_ENVIRONMENT) { $env:HELIOS_ENVIRONMENT } else { 'integration' }),
    [switch]$CreateResourceGroup,
    [switch]$SkipLogin
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RequiredExtensions = @(
    'azure-devops',
    'containerapp',
    'ml',
    'resource-graph'
)

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install Azure CLI first: https://learn.microsoft.com/cli/azure/install-azure-cli"
    }
}

function Invoke-AzCli {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments -join ' ')"
    }
}

Write-Step 'Checking Azure CLI installation'
Require-Command az
$AzVersion = az version --query '"azure-cli"' -o tsv
Write-Ok "Azure CLI $AzVersion found"

Write-Step 'Configuring Azure CLI runtime preferences'
Invoke-AzCli config set extension.use_dynamic_install=yes_without_prompt core.only_show_errors=true
Invoke-AzCli config set defaults.location=$Location defaults.group=$ResourceGroupName
Write-Ok "Defaults set for location '$Location' and resource group '$ResourceGroupName'"

Write-Step 'Installing or updating HELIOS-required Azure CLI extensions'
foreach ($ExtensionName in $RequiredExtensions) {
    $Installed = az extension show --name $ExtensionName --query name -o tsv 2>$null
    if ($Installed) {
        Invoke-AzCli extension update --name $ExtensionName
        Write-Ok "Updated extension '$ExtensionName'"
    }
    else {
        Invoke-AzCli extension add --name $ExtensionName
        Write-Ok "Installed extension '$ExtensionName'"
    }
}

if (-not $SkipLogin) {
    Write-Step 'Validating Azure sign-in context'
    $SignedIn = az account show --query id -o tsv 2>$null
    if (-not $SignedIn) {
        $LoginArgs = @('login')
        if ($TenantId) {
            $LoginArgs += @('--tenant', $TenantId)
        }
        Invoke-AzCli @LoginArgs
    }
}

if ($SubscriptionId) {
    Write-Step "Selecting subscription '$SubscriptionId'"
    Invoke-AzCli account set --subscription $SubscriptionId
}

$AccountJson = az account show -o json | ConvertFrom-Json
if (-not $AccountJson) {
    throw 'No active Azure account is selected. Run az login or rerun without -SkipLogin.'
}

if ($CreateResourceGroup) {
    Write-Step "Ensuring resource group '$ResourceGroupName' exists"
    if ($PSCmdlet.ShouldProcess($ResourceGroupName, 'Create or update HELIOS resource group')) {
        Invoke-AzCli group create --name $ResourceGroupName --location $Location --tags `
            workload=helios `
            component=hermes-xcore `
            environment=$EnvironmentName
    }
}

Write-Step 'Exporting HELIOS Azure environment variables for this session'
$env:HELIOS_AZURE_SUBSCRIPTION_ID = $AccountJson.id
$env:HELIOS_AZURE_TENANT_ID = $AccountJson.tenantId
$env:HELIOS_AZURE_LOCATION = $Location
$env:HELIOS_RESOURCE_GROUP = $ResourceGroupName
$env:HELIOS_ENVIRONMENT = $EnvironmentName
$env:HELIOS_HERMES_XCORE_ENABLED = 'true'

Write-Ok 'HELIOS Hermes XCore Azure CLI setup complete'
Write-Host ''
Write-Host 'Session variables:' -ForegroundColor Cyan
Write-Host "  HELIOS_AZURE_SUBSCRIPTION_ID=$($env:HELIOS_AZURE_SUBSCRIPTION_ID)"
Write-Host "  HELIOS_AZURE_TENANT_ID=$($env:HELIOS_AZURE_TENANT_ID)"
Write-Host "  HELIOS_AZURE_LOCATION=$($env:HELIOS_AZURE_LOCATION)"
Write-Host "  HELIOS_RESOURCE_GROUP=$($env:HELIOS_RESOURCE_GROUP)"
Write-Host "  HELIOS_ENVIRONMENT=$($env:HELIOS_ENVIRONMENT)"
Write-Host "  HELIOS_HERMES_XCORE_ENABLED=$($env:HELIOS_HERMES_XCORE_ENABLED)"
