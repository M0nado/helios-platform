param(
    [string]$ManifestPath = "docs/planning/source-repositories.yaml"
)

./scripts/merge/fetch-all-remotes.ps1 -ManifestPath $ManifestPath
./scripts/merge/analyze-branches.ps1
Write-Host "Review docs/planning/branch-inventory.md and subsystem inventory docs before importing upstream changes."
