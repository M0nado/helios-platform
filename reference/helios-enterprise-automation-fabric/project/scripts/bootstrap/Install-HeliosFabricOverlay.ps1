[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackRoot,

    [Parameter(Mandatory)]
    [string]$RepositoryRoot,

    [switch]$Apply,

    [string]$Confirm = ''
)

$ErrorActionPreference = 'Stop'
$pack = (Resolve-Path -LiteralPath $PackRoot).Path
$repository = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$excludedSegments = @('.git', '.venv', 'artifacts', 'bin', 'obj', '__pycache__')

function Get-RelativePath {
    param([string]$Base, [string]$Path)
    return [System.IO.Path]::GetRelativePath($Base, $Path)
}

function Get-FileHashValue {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$entries = [System.Collections.Generic.List[object]]::new()
Get-ChildItem -LiteralPath $pack -Recurse -File |
    Where-Object {
        $relative = Get-RelativePath -Base $pack -Path $_.FullName
        $parts = $relative -split '[\\/]'
        -not ($parts | Where-Object { $_ -in $excludedSegments })
    } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = Get-RelativePath -Base $pack -Path $_.FullName
        $target = Join-Path $repository $relative
        $sourceHash = Get-FileHashValue -Path $_.FullName
        $status = 'add'
        $targetHash = $null
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            $targetHash = Get-FileHashValue -Path $target
            $status = if ($targetHash -eq $sourceHash) { 'same' } else { 'conflict' }
        }
        $entries.Add([pscustomobject]@{
            path = $relative.Replace('\', '/')
            status = $status
            sourceSha256 = $sourceHash
            targetSha256 = $targetHash
        })
    }

$conflicts = @($entries | Where-Object status -eq 'conflict')
$report = [ordered]@{
    specVersion = '1.0'
    mode = if ($Apply) { 'apply' } else { 'preview' }
    packRoot = $pack
    repositoryRoot = $repository
    add = @($entries | Where-Object status -eq 'add').Count
    same = @($entries | Where-Object status -eq 'same').Count
    conflicts = $conflicts.Count
    entries = $entries
}

$artifactRoot = Join-Path $pack 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$reportPath = Join-Path $artifactRoot 'overlay-preview.json'
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM

if (-not $Apply) {
    Write-Host "Preview complete: $reportPath"
    if ($conflicts.Count -gt 0) {
        Write-Warning "$($conflicts.Count) conflict(s) require manual reconciliation before apply."
    }
    exit 0
}

if ($Confirm -ne 'APPLY HELIOS AUTOMATION FABRIC') {
    throw "-Apply requires -Confirm 'APPLY HELIOS AUTOMATION FABRIC'."
}
if ($conflicts.Count -gt 0) {
    throw "Apply refused because $($conflicts.Count) destination file(s) differ. Review $reportPath and reconcile them manually."
}

foreach ($entry in $entries | Where-Object status -eq 'add') {
    $source = Join-Path $pack $entry.path
    $target = Join-Path $repository $entry.path
    $parent = Split-Path -Parent $target
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
}

$report.mode = 'applied'
$report.applied = @($entries | Where-Object status -eq 'add').Count
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $artifactRoot 'overlay-result.json') -Encoding utf8NoBOM
Write-Host "Applied $($report.applied) new file(s). No existing file was overwritten."
