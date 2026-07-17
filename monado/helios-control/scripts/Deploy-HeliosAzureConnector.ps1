[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SubscriptionId,
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $EntraClientId,
    [Parameter(Mandatory)] [string] $EntraTenantId,
    [Parameter(Mandatory)] [string] $AllowedPrincipalObjectId,
    [string] $ImageReference = 'heliosplaceholderacr.azurecr.io/helios-connect@sha256:0000000000000000000000000000000000000000000000000000000000000000',
    [ValidateSet('dev', 'test', 'prod')] [string] $EnvironmentName = 'dev',
    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Apply) {
    throw 'Direct local apply is retired. Publish an immutable image with Connect-HeliosAzureInteractive.ps1, then use the reviewer-gated helios-cloud-deploy GitHub workflow.'
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required. Install it from Microsoft and authenticate with az login first.'
}

if ($ImageReference -notmatch '^(?<registry>[a-zA-Z0-9][a-zA-Z0-9-]{4,49})\.azurecr\.io/.+@sha256:[0-9a-fA-F]{64}$') {
    throw 'ImageReference must be an immutable Azure Container Registry reference.'
}
$containerRegistryName = $Matches.registry

az account show --output none
az account set --subscription $SubscriptionId
$exists = az group exists --name $ResourceGroup
if ($exists -ne 'true') {
    throw "Resource group '$ResourceGroup' does not exist. Create or select it in Azure Portal before running this deployment."
}

$root = Split-Path $PSScriptRoot -Parent
$template = Join-Path $root 'infra/connector.bicep'
$parameters = @(
    "environmentName=$EnvironmentName",
    "containerImage=$ImageReference",
    "containerRegistryName=$containerRegistryName",
    "allowPreviewPlaceholder=$($ImageReference.EndsWith('@sha256:' + ('0' * 64), [StringComparison]::OrdinalIgnoreCase).ToString().ToLowerInvariant())",
    "entraClientId=$EntraClientId",
    "entraTenantId=$EntraTenantId",
    "allowedPrincipalObjectId=$AllowedPrincipalObjectId"
)

Write-Host 'Running Azure Resource Manager what-if. No resources are changed by this step.'
az deployment group what-if `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroup `
    --template-file $template `
    --parameters $parameters

if (-not $Apply) {
    Write-Host 'Preview complete. Run Configure/Publish with Connect-HeliosAzureInteractive.ps1, then use the protected helios-cloud-deploy workflow; direct -Apply is retired.'
    exit 0
}
