[CmdletBinding()]
param(
    [ValidateSet('Status', 'Interactive')]
    [string] $Mode = 'Status',
    [string] $Repository = 'M0nado/helios-platform',
    [string] $SubscriptionId
)

$ErrorActionPreference = 'Stop'

function Write-State([string] $Name, [string] $State) {
    '{0,-27} {1}' -f $Name, $State
}

if ($Mode -eq 'Interactive') {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI is not installed.' }
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI is not installed.' }
    gh auth status --hostname github.com 2>$null
    if ($LASTEXITCODE -ne 0) { gh auth login --hostname github.com --web --git-protocol https }
    gh repo set-default $Repository
    az account show --only-show-errors 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { az login --use-device-code --output none }
    if ($SubscriptionId) { az account set --subscription $SubscriptionId }
}

$failed = $false
if (Get-Command gh -ErrorAction SilentlyContinue) {
    gh auth status --hostname github.com 2>$null
    if ($LASTEXITCODE -eq 0) { Write-State 'GitHub CLI' 'ready' } else { Write-State 'GitHub CLI' 'authorization required'; $failed = $true }
} else {
    Write-State 'GitHub CLI' 'not installed'
    $failed = $true
}
if (Get-Command az -ErrorAction SilentlyContinue) {
    az account show --only-show-errors 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-State 'Azure CLI' 'ready' } else { Write-State 'Azure CLI' 'authorization required'; $failed = $true }
} else {
    Write-State 'Azure CLI' 'not installed'
    $failed = $true
}
if ($failed) { exit 1 }
