[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Hostname,
    [Parameter(Mandatory)] [string]$SitePath,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ApplicationId = '',
    [switch]$GrantSitesSelectedWrite,
    [switch]$Apply,
    [string]$Confirm = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI is required for Microsoft Graph calls.' }
$contract = Get-Content (Join-Path $Root 'config\sharepoint\lists-and-libraries.json') -Raw | ConvertFrom-Json
$siteReference = "$Hostname`:$SitePath"
Write-Host "SharePoint site: $siteReference"
Write-Host 'Libraries/lists to provision:'
$contract.libraries.name + $contract.lists.name | ForEach-Object { Write-Host "  $_" }
if (-not $Apply) { Write-Host 'Preview only. No SharePoint or Graph state was changed.'; exit 0 }
if ($Confirm -ne 'PROVISION HELIOS SHAREPOINT') { throw 'Apply requires -Confirm "PROVISION HELIOS SHAREPOINT".' }

$site = & az rest --method GET --url "https://graph.microsoft.com/v1.0/sites/$siteReference" --resource 'https://graph.microsoft.com' | ConvertFrom-Json
if (-not $site.id) { throw 'Unable to resolve SharePoint site.' }
$lists = & az rest --method GET --url "https://graph.microsoft.com/v1.0/sites/$($site.id)/lists?`$select=id,displayName,list" --resource 'https://graph.microsoft.com' | ConvertFrom-Json

foreach ($library in $contract.libraries) {
    $existing = $lists.value | Where-Object displayName -eq $library.name | Select-Object -First 1
    if (-not $existing) {
        $body = @{ displayName=$library.name; list=@{ template='documentLibrary' } } | ConvertTo-Json -Depth 6
        $existing = $body | & az rest --method POST --url "https://graph.microsoft.com/v1.0/sites/$($site.id)/lists" --resource 'https://graph.microsoft.com' --headers 'Content-Type=application/json' --body '@-' | ConvertFrom-Json
    }
    foreach ($column in $library.columns) {
        $columnBody = @{ name=$column.name; displayName=$column.name; required=[bool]$column.required }
        switch ($column.type) {
            'text' { $columnBody.text = @{} }
            'choice' { $columnBody.choice = @{ choices=$column.choices; displayAs='dropDownMenu' } }
            'hyperlink' { $columnBody.text = @{} }
        }
        $json = $columnBody | ConvertTo-Json -Depth 8
        try {
            $json | & az rest --method POST --url "https://graph.microsoft.com/v1.0/sites/$($site.id)/lists/$($existing.id)/columns" --resource 'https://graph.microsoft.com' --headers 'Content-Type=application/json' --body '@-' | Out-Null
        } catch {
            Write-Verbose "Column $($column.name) may already exist: $_"
        }
    }
}

foreach ($listContract in $contract.lists) {
    if (-not ($lists.value | Where-Object displayName -eq $listContract.name)) {
        $body = @{ displayName=$listContract.name; list=@{ template='genericList' } } | ConvertTo-Json -Depth 6
        $body | & az rest --method POST --url "https://graph.microsoft.com/v1.0/sites/$($site.id)/lists" --resource 'https://graph.microsoft.com' --headers 'Content-Type=application/json' --body '@-' | Out-Null
    }
}

if ($GrantSitesSelectedWrite) {
    if (-not $ApplicationId) { throw 'ApplicationId is required for a Sites.Selected grant.' }
    $permissionBody = @{ roles=@('write'); grantedToIdentities=@(@{ application=@{ id=$ApplicationId; displayName='HELIOS Automation Fabric' } }) } | ConvertTo-Json -Depth 8
    $permissionBody | & az rest --method POST --url "https://graph.microsoft.com/v1.0/sites/$($site.id)/permissions" --resource 'https://graph.microsoft.com' --headers 'Content-Type=application/json' --body '@-' | Out-Null
}
Write-Host "SharePoint provisioning completed for site ID $($site.id). Purview retention label publication remains a compliance-admin action."
