[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [string] $EnvironmentName = 'dev'
)

$ErrorActionPreference = 'Stop'
az account show --output none
az bicep build --file "$PSScriptRoot/../infra/main.bicep"
az deployment group what-if `
  --resource-group $ResourceGroup `
  --template-file "$PSScriptRoot/../infra/main.bicep" `
  --parameters environmentName=$EnvironmentName

Write-Host 'Preview complete. No Azure resources were changed.'
