[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$VaultName,
    [switch]$Apply,
    [string]$Confirm = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Get-Module -ListAvailable -Name Az.KeyVault)) { throw 'Az.KeyVault is required.' }
Write-Host 'Create the Teams Workflows flow using config/teams/workflow-contracts.json and the Adaptive Card templates.'
Write-Host 'The generated HTTP URL will be stored as teams-workflow-webhook-url in Key Vault.'
if (-not $Apply) { Write-Host 'Preview only. No URL was requested or stored.'; exit 0 }
if ($Confirm -ne 'REGISTER HELIOS TEAMS WORKFLOW') { throw 'Apply requires -Confirm "REGISTER HELIOS TEAMS WORKFLOW".' }
$url = Read-Host 'Paste the Teams Workflows webhook URL' -AsSecureString
Set-AzKeyVaultSecret -VaultName $VaultName -Name 'teams-workflow-webhook-url' -SecretValue $url -Tag @{ owner='HELIOS'; connector='teams'; readback='denied-by-policy' } | Out-Null
Write-Host 'Teams workflow URL stored. The value was not printed or committed.'
