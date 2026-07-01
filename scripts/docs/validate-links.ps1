$missing = @()
Get-ChildItem docs -Recurse -File -Include *.md | ForEach-Object {
    Select-String -Path $_.FullName -Pattern '\]\(([^)]+)\)' | ForEach-Object {
        $target = $_.Matches.Groups[1].Value
        if ($target -notmatch '^(https?:|#)' -and $target -notmatch '^mailto:') {
            $path = Join-Path (Split-Path $_.Path) $target
            if (-not (Test-Path $path)) { $missing += "$($_.Path): $target" }
        }
    }
}
if ($missing.Count -gt 0) { $missing | ForEach-Object { Write-Error $_ }; exit 1 }
