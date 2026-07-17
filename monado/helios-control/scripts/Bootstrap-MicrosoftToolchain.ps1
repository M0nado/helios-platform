[CmdletBinding()]
param(
    [ValidateSet('Verify', 'Plan')]
    [string] $Mode = 'Verify'
)

$ErrorActionPreference = 'Stop'
$required = @('az', 'azd', 'dotnet', 'gh', 'pwsh', 'docker', 'node', 'npm', 'jq')
$optional = @('func', 'pac', 'atk', 'claude', 'code-insiders')
$report = [ordered]@{ mode = $Mode; required = @(); optional = @(); ready = $true }

foreach ($name in $required) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    $report.required += [ordered]@{ name = $name; found = [bool]$command; path = $command.Source }
    if (-not $command) { $report.ready = $false }
}
foreach ($name in $optional) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    $report.optional += [ordered]@{ name = $name; found = [bool]$command; path = $command.Source }
}

if (Get-Command az -ErrorAction SilentlyContinue) {
    az bicep version | Out-Null
    if ($LASTEXITCODE -ne 0) { $report.ready = $false }
}

$report | ConvertTo-Json -Depth 5
if ($Mode -eq 'Verify' -and -not $report.ready) { exit 2 }

# Installation is deliberately performed by the devcontainer features or an
# approved enterprise software-management policy. This script never downloads
# executables, changes the tenant, or logs in with a secret.
