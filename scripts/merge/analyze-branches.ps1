param(
    [string]$Out = "docs/planning/branch-inventory.md"
)

New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
$timestamp = Get-Date -Format o
$remotes = git remote -v
$branches = git branch --all --verbose --no-abbrev
$status = git status --short --branch

$required = @('helios-control', 'hermes-fleet-production', 'hermes-core', 'xcore-agent')
$remoteNames = git remote

"# Branch Inventory`n" | Set-Content $Out
"Generated: $timestamp`n" | Add-Content $Out
"## Current status`n" | Add-Content $Out
'```' | Add-Content $Out
$status | Add-Content $Out
'```' + "`n" | Add-Content $Out
"## Configured remotes`n" | Add-Content $Out
if ($remotes) { '```' | Add-Content $Out; $remotes | Add-Content $Out; '```' + "`n" | Add-Content $Out } else { "No remotes configured.`n" | Add-Content $Out }
"## Required source availability`n" | Add-Content $Out
"| Source | Status |" | Add-Content $Out
"|---|---|" | Add-Content $Out
foreach ($source in $required) {
    $state = if ($remoteNames -contains $source) { 'configured' } else { 'missing' }
    "| `$source` | $state |" | Add-Content $Out
}
"`n## Branches`n" | Add-Content $Out
'```' | Add-Content $Out
$branches | Add-Content $Out
'```' | Add-Content $Out
