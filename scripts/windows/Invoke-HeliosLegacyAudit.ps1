#Requires -Version 5.1
<#
.SYNOPSIS
    Inventories absorbed XTier/WinRE source snapshots without executing them.

.DESCRIPTION
    Computes hashes, detects high-risk command families, and writes JSON/Markdown
    reports. The legacy files remain *.disabled.txt and cannot be dot-sourced by
    this adapter.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$OutputDirectory = "$env:ProgramData\HELIOS\Reports\LegacyAudit"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$legacyRoots = @(
    (Join-Path $RepositoryRoot 'legacy\xtier\raw'),
    (Join-Path $RepositoryRoot 'legacy\winre\raw')
)
$riskPatterns = [ordered]@{
    diskMutation = '(?i)\b(Clear-Disk|Remove-Partition|Format-Volume|diskpart|Initialize-Disk)\b'
    identityMutation = '(?i)\b(New-Mg|Update-Mg|Remove-Mg|New-AzAD|New-ConditionalAccessPolicy)\b'
    cloudDeployment = '(?i)\b(New-Az|Set-Az|Remove-Az|az\s+deployment|azd\s+up)\b'
    securityMutation = '(?i)\b(Set-MpPreference|Add-MpPreference|DisableRealtimeMonitoring|netsh\s+advfirewall)\b'
    secretMaterial = '(?i)\b(client[_-]?secret|api[_-]?key|password|recovery[_-]?key)\b'
    scheduledTask = '(?i)\b(Register-ScheduledTask|schtasks)\b'
}

$records = foreach ($root in $legacyRoots) {
    if (-not (Test-Path $root)) { continue }
    foreach ($file in Get-ChildItem -Path $root -File -Recurse) {
        $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
        $hits = [ordered]@{}
        foreach ($pattern in $riskPatterns.GetEnumerator()) {
            $hits[$pattern.Key] = [regex]::Matches($text, $pattern.Value).Count
        }
        [pscustomobject]@{
            path = $file.FullName.Substring($RepositoryRoot.Length).TrimStart('\\','/')
            bytes = $file.Length
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
            riskHits = $hits
            executable = $false
        }
    }
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$jsonPath = Join-Path $OutputDirectory "legacy-audit-$timestamp.json"
$mdPath = Join-Path $OutputDirectory "legacy-audit-$timestamp.md"
$payload = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    repositoryRoot = $RepositoryRoot
    fileCount = @($records).Count
    files = @($records)
}
$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8

$lines = @('# HELIOS Legacy XTier/WinRE Audit','',"Files: $(@($records).Count)",'', '| File | SHA-256 | Risk hits |','|---|---|---|')
foreach ($record in $records) {
    $total = 0
    foreach ($value in $record.riskHits.Values) { $total += [int]$value }
    $lines += "| ``$($record.path)`` | ``$($record.sha256)`` | $total |"
}
$lines += ''
$lines += '> Source snapshots are intentionally inert. Promote behavior only through reviewed HELIOS adapters.'
$lines | Set-Content -Path $mdPath -Encoding UTF8
Write-Host "JSON: $jsonPath" -ForegroundColor Green
Write-Host "Markdown: $mdPath" -ForegroundColor Green
