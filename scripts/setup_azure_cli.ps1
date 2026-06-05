# Idempotent Azure CLI bootstrap for HELIOS/HERMES Windows operators.
[CmdletBinding()]
param(
    [switch]$DeviceCodeLogin
)

$ErrorActionPreference = 'Stop'
$extensions = @('azure-devops', 'ml', 'ssh', 'resource-graph')

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Install it from https://aka.ms/installazurecliwindows before rerunning."
}

az version --output table
foreach ($extension in $extensions) {
    az extension show --name $extension *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Azure CLI extension already installed: $extension"
    }
    else {
        Write-Host "Installing Azure CLI extension: $extension"
        az extension add --name $extension --only-show-errors
    }
}

az account show *> $null
if ($LASTEXITCODE -eq 0) {
    az account show --query "{name:name, tenantId:tenantId, user:user.name}" --output table
}
elseif ($DeviceCodeLogin) {
    az login --use-device-code
}
else {
    Write-Host "Azure CLI is ready. Run 'az login' or rerun with -DeviceCodeLogin to authenticate."
}
