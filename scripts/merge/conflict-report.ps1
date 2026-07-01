param(
    [string]$Out = "docs/planning/conflict-report.md"
)

New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
"# Conflict Report`n" | Set-Content $Out
"Generated: $(Get-Date -Format o)`n" | Add-Content $Out
"## Git status`n" | Add-Content $Out
'```' | Add-Content $Out
git status --short | Add-Content $Out
'```' + "`n" | Add-Content $Out
"## Unmerged paths`n" | Add-Content $Out
'```' | Add-Content $Out
git diff --name-only --diff-filter=U | Add-Content $Out
'```' | Add-Content $Out
