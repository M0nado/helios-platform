param(
    [string]$ManifestPath = "docs/planning/source-repositories.yaml"
)

if (-not (Test-Path $ManifestPath)) {
    throw "Missing source repository manifest: $ManifestPath"
}

Write-Host "Configured remotes before changes:"
git remote -v

$content = Get-Content $ManifestPath -Raw
$entries = [regex]::Matches($content, '(?ms)^  - name: (?<name>[^\r\n]+).*?remote_name: (?<remote>[^\r\n]+).*?remote_url: (?<url>[^\r\n]+).*?local_path: (?<path>[^\r\n]+)')

foreach ($entry in $entries) {
    $name = $entry.Groups['name'].Value.Trim()
    $remoteName = $entry.Groups['remote'].Value.Trim()
    $remoteUrl = $entry.Groups['url'].Value.Trim()
    $localPath = $entry.Groups['path'].Value.Trim()
    $candidate = $null

    if ($remoteUrl -and $remoteUrl -ne 'null') {
        $candidate = $remoteUrl
    } elseif ($localPath -and $localPath -ne 'null') {
        if (-not (Test-Path $localPath)) {
            Write-Warning "Skipping $name because local_path does not exist: $localPath"
            continue
        }
        $candidate = $localPath
    } else {
        Write-Warning "Skipping $name because neither remote_url nor local_path is configured."
        continue
    }

    $existing = git remote get-url $remoteName 2>$null
    if ($LASTEXITCODE -eq 0) {
        if ($existing -ne $candidate) {
            Write-Host "Updating remote $remoteName -> $candidate"
            git remote set-url $remoteName $candidate
        } else {
            Write-Host "Remote $remoteName already configured."
        }
    } else {
        Write-Host "Adding remote $remoteName -> $candidate"
        git remote add $remoteName $candidate
    }
}

Write-Host "Fetching all configured remotes..."
git fetch --all --prune
