[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ResourceGroup,

    [ValidateSet('dev', 'test', 'preview', 'prod')]
    [string] $EnvironmentName = 'dev',

    [ValidatePattern('^[a-zA-Z0-9-]{2,48}[a-zA-Z0-9]$')]
    [string] $ServiceName = 'helios-connector',

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9]{5,50}$')]
    [string] $ContainerRegistryName,

    [ValidatePattern('^[^\s]+\.azurecr\.io/[^\s]+@sha256:[0-9a-fA-F]{64}$')]
    [string] $ContainerImage,

    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string] $EntraClientId,

    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string] $EntraTenantId,

    [string] $AllowedClientIds = $env:HELIOS_ALLOWED_CLIENT_IDS,

    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string] $AllowedPrincipalObjectId
)

$ErrorActionPreference = 'Stop'
$templateFile = Join-Path $PSScriptRoot '../infra/main.bicep'
$previewDigest = '0' * 64

if ([string]::IsNullOrWhiteSpace($ContainerImage)) {
    # This value is intentionally undeployable unless the template's preview
    # escape hatch is explicitly enabled. The protected deployment never does so.
    $ContainerImage = "$ContainerRegistryName.azurecr.io/helios-connect@sha256:$previewDigest"
}

$expectedRegistryPrefix = "$ContainerRegistryName.azurecr.io/"
if (-not $ContainerImage.StartsWith($expectedRegistryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ContainerImage must be hosted by the selected registry '$ContainerRegistryName'."
}

$digestMatch = [regex]::Match($ContainerImage, '(?i)@sha256:([0-9a-f]{64})$')
if (-not $digestMatch.Success) {
    throw 'ContainerImage must be an immutable OCI image reference ending in @sha256:<64 hex characters>.'
}

$allowPreviewPlaceholder = $digestMatch.Groups[1].Value -eq $previewDigest
$allowPreviewPlaceholderValue = if ($allowPreviewPlaceholder) { 'true' } else { 'false' }

if ([string]::IsNullOrWhiteSpace($AllowedClientIds)) {
    throw 'AllowedClientIds is required and must contain one or more GUID values.'
}
$allowedClientIdSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($candidate in $AllowedClientIds.Split([char[]] @(',', ';', ' ', "`t", "`r", "`n"), [StringSplitOptions]::RemoveEmptyEntries)) {
    $parsed = [guid]::Empty
    if (-not [guid]::TryParse($candidate, [ref] $parsed)) {
        throw "AllowedClientIds contains an invalid GUID value '$candidate'."
    }
    [void] $allowedClientIdSet.Add($parsed.ToString().ToLowerInvariant())
}
if ($allowedClientIdSet.Count -eq 0) {
    throw 'AllowedClientIds is required and must contain one or more GUID values.'
}
$resolvedAllowedClientIds = [string]::Join(',', (@($allowedClientIdSet) | Sort-Object))

az account show --output none
if ($LASTEXITCODE -ne 0) { throw 'Azure CLI is not authenticated.' }

az bicep build --file $templateFile
if ($LASTEXITCODE -ne 0) { throw 'Bicep build failed.' }

$deploymentParameters = @(
    "environmentName=$EnvironmentName"
    "serviceName=$ServiceName"
    "containerImage=$ContainerImage"
    "containerRegistryName=$ContainerRegistryName"
    "allowPreviewPlaceholder=$allowPreviewPlaceholderValue"
    "entraClientId=$EntraClientId"
    "entraTenantId=$EntraTenantId"
    "allowedClientIds=$resolvedAllowedClientIds"
    "allowedPrincipalObjectId=$AllowedPrincipalObjectId"
)

az deployment group what-if `
  --resource-group $ResourceGroup `
  --template-file $templateFile `
  --parameters @deploymentParameters
if ($LASTEXITCODE -ne 0) { throw 'Azure Resource Manager what-if failed.' }

if ($allowPreviewPlaceholder) {
    Write-Host 'Preview complete with the documented all-zero immutable-image placeholder. No Azure resources were changed; this image cannot be used by the protected deployment.'
} else {
    Write-Host 'Preview complete with an explicit immutable image digest. No Azure resources were changed.'
}
