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
    Write-Host 'Preview complete. After review, use Connect-HeliosAzureInteractive.ps1 -Mode Configure; direct -Apply is retired.'
    exit 0
}

if ($env:HELIOS_CONFIRM_APPLY -ne 'YES') {
    throw 'Set HELIOS_CONFIRM_APPLY=YES before using -Apply.'
}


if ($ImageReference.EndsWith('@sha256:' + ('0' * 64), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Replace the all-zero preview placeholder with an approved immutable image reference before applying.'
}

Write-Host 'Applying the reviewed Helios Azure connector deployment.'
$deployment = az deployment group create `
    --name "helios-connector-$EnvironmentName" `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroup `
    --template-file $template `
    --parameters $parameters `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0 -or -not $deployment) {
    throw 'Azure connector infrastructure deployment failed.'
}

$connectorUrl = $deployment.properties.outputs.connectorUrl.value
$connectorClientId = $deployment.properties.outputs.connectorEntraClientId.value
$connectorTenantId = $deployment.properties.outputs.connectorEntraTenantId.value

Write-Host "Connector URL: $connectorUrl"
Write-Host "HELIOS_CONNECTOR_URL=$connectorUrl"
Write-Host "HELIOS_ENTRA_CLIENT_ID=$connectorClientId"
Write-Host "AZURE_TENANT_ID=$connectorTenantId"
Write-Host 'No OpenAI, Anthropic, Azure OpenAI, or provider key was created, retrieved, printed, or stored.'
