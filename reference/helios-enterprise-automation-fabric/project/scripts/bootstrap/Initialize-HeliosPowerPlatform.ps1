[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Environment,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$Apply,
    [string]$Confirm = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Get-Command pac -ErrorAction SilentlyContinue)) { throw 'Power Platform CLI (pac) is required.' }
$definition = Join-Path $Root 'power-platform\custom-connectors\helios-fabric\apiDefinition.swagger.json'
$properties = Join-Path $Root 'power-platform\custom-connectors\helios-fabric\apiProperties.json'
Get-Content $definition -Raw | ConvertFrom-Json | Out-Null
Get-Content $properties -Raw | ConvertFrom-Json | Out-Null
Write-Host "Power Platform environment: $Environment"
Write-Host "Definition: $definition"
Write-Host "Properties: $properties"
if (-not $Apply) { Write-Host 'Preview only. The custom connector was not created.'; exit 0 }
if ($Confirm -ne 'CREATE HELIOS POWER CONNECTOR') { throw 'Apply requires -Confirm "CREATE HELIOS POWER CONNECTOR".' }
& pac connector create --api-definition-file $definition --api-properties-file $properties --environment $Environment
if ($LASTEXITCODE -ne 0) { throw 'Power Platform connector creation failed.' }
Write-Host 'Custom connector created. Create the Teams Workflows flow from config/teams/workflow-contracts.json, then stage its webhook URL in Key Vault.'
