[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ReportDirectory = 'reports/powershell-syntax',
    [string]$BaselinePath = 'config/powershell-syntax-baseline.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$reportPath = Join-Path $Root $ReportDirectory
New-Item -ItemType Directory -Path $reportPath -Force | Out-Null

$baselineFullPath = Join-Path $Root $BaselinePath
if (-not (Test-Path -LiteralPath $baselineFullPath -PathType Leaf)) {
    throw "PowerShell syntax baseline was not found: $baselineFullPath"
}

$baselineDocument = Get-Content -LiteralPath $baselineFullPath -Raw -Encoding utf8 | ConvertFrom-Json
$baselineByPath = [System.Collections.Generic.Dictionary[string, object]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)

foreach ($entry in $baselineDocument.entries) {
    $normalizedPath = ([string]$entry.path).Replace('\', '/')
    if ($baselineByPath.ContainsKey($normalizedPath)) {
        throw "Duplicate PowerShell syntax baseline entry: $normalizedPath"
    }
    $baselineByPath.Add($normalizedPath, $entry)
}

$excludedSegments = @(
    '/.git/',
    '/bin/',
    '/obj/',
    '/.tools/',
    '/.venv/',
    '/node_modules/',
    '/reports/'
)

$files = Get-ChildItem -LiteralPath $Root -Filter '*.ps1' -File -Recurse -ErrorAction Stop |
    Where-Object {
        $relative = [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
        $normalized = "/$relative"
        -not ($excludedSegments | Where-Object { $normalized.Contains($_) })
    } |
    Sort-Object FullName

$results = [System.Collections.Generic.List[object]]::new()
$failedPathSet = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
$totalParseErrors = 0
$blockingFailureCount = 0
$knownDebtFailureCount = 0
$unbaselinedFailureCount = 0

foreach ($file in $files) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile(
        $file.FullName,
        [ref]$tokens,
        [ref]$parseErrors
    ) | Out-Null

    $relativePath = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')

    if ($parseErrors.Count -eq 0) {
        $results.Add([pscustomobject]@{
            path = $relativePath
            status = 'passed'
            classification = 'active-or-valid'
            blocking = $false
            reason = $null
            errors = @()
        })
        continue
    }

    $null = $failedPathSet.Add($relativePath)
    $totalParseErrors += $parseErrors.Count
    $details = @(
        foreach ($parseError in $parseErrors) {
            [pscustomobject]@{
                line = $parseError.Extent.StartLineNumber
                column = $parseError.Extent.StartColumnNumber
                endLine = $parseError.Extent.EndLineNumber
                endColumn = $parseError.Extent.EndColumnNumber
                errorId = $parseError.ErrorId
                message = $parseError.Message
                text = $parseError.Extent.Text
            }
        }
    )

    $baselineEntry = $null
    $isBaselined = $baselineByPath.TryGetValue($relativePath, [ref]$baselineEntry)
    $isBlocking = $true
    $classification = 'unbaselined-parser-failure'
    $reason = 'This parser failure is not present in the reviewed debt baseline.'
    $status = 'failed'

    if ($isBaselined) {
        $isBlocking = [bool]$baselineEntry.blocking
        $classification = [string]$baselineEntry.classification
        $reason = [string]$baselineEntry.reason
        if ($isBlocking) {
            $blockingFailureCount++
        } else {
            $knownDebtFailureCount++
            $status = 'known-debt'
        }
    } else {
        $unbaselinedFailureCount++
        $blockingFailureCount++
    }

    $results.Add([pscustomobject]@{
        path = $relativePath
        status = $status
        classification = $classification
        blocking = $isBlocking
        reason = $reason
        errors = $details
    })

    $first = $details[0]
    $prefix = if ($isBlocking) { 'BLOCK' } else { 'DEBT' }
    $color = if ($isBlocking) { 'Red' } else { 'Yellow' }
    Write-Host (
        '{0} {1}:{2}:{3} [{4}] {5} ({6} diagnostic(s), {7})' -f
        $prefix,
        $relativePath,
        $first.line,
        $first.column,
        $first.errorId,
        $first.message,
        $details.Count,
        $classification
    ) -ForegroundColor $color
}

$staleBaselineEntries = @(
    foreach ($entry in $baselineDocument.entries) {
        $path = ([string]$entry.path).Replace('\', '/')
        if (-not $failedPathSet.Contains($path)) {
            [pscustomobject]@{
                path = $path
                classification = [string]$entry.classification
                reason = 'The file no longer fails parsing. Remove this stale baseline entry in the same change that repaired or removed the file.'
            }
        }
    }
)

$summary = [pscustomobject]@{
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    root = $Root
    baselinePath = $BaselinePath
    filesChecked = $files.Count
    validFiles = @($results | Where-Object status -eq 'passed').Count
    knownDebtFiles = $knownDebtFailureCount
    blockingFailedFiles = $blockingFailureCount
    unbaselinedFailedFiles = $unbaselinedFailureCount
    parseErrors = $totalParseErrors
    staleBaselineEntries = $staleBaselineEntries.Count
    excludedSegments = $excludedSegments
    results = $results
    staleBaseline = $staleBaselineEntries
}

$jsonPath = Join-Path $reportPath 'powershell-syntax.json'
$markdownPath = Join-Path $reportPath 'powershell-syntax.md'
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add('# PowerShell Syntax Validation')
$markdown.Add('')
$markdown.Add("- Files checked: $($summary.filesChecked)")
$markdown.Add("- Valid files: $($summary.validFiles)")
$markdown.Add("- Known non-blocking debt files: $($summary.knownDebtFiles)")
$markdown.Add("- Blocking failed files: $($summary.blockingFailedFiles)")
$markdown.Add("- Unbaselined failed files: $($summary.unbaselinedFailedFiles)")
$markdown.Add("- Parser diagnostics: $($summary.parseErrors)")
$markdown.Add("- Stale baseline entries: $($summary.staleBaselineEntries)")
$markdown.Add('')
$markdown.Add('Every PowerShell file was parsed. Baseline entries suppress only known legacy debt; they do not skip files or authorize execution.')
$markdown.Add('')

foreach ($result in $results) {
    if ($result.status -eq 'passed') {
        continue
    }

    $markdown.Add(('## `{0}`' -f $result.path))
    $markdown.Add('')
    $markdown.Add("- Status: $($result.status)")
    $markdown.Add("- Classification: $($result.classification)")
    $markdown.Add("- Blocking: $($result.blocking)")
    $markdown.Add("- Rationale: $($result.reason)")
    foreach ($detail in $result.errors) {
        $message = $detail.message.Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
        $markdown.Add(('- Line {0}, column {1}, `{2}`: {3}' -f $detail.line, $detail.column, $detail.errorId, $message))
    }
    $markdown.Add('')
}

if ($staleBaselineEntries.Count -gt 0) {
    $markdown.Add('## Stale baseline entries')
    $markdown.Add('')
    foreach ($entry in $staleBaselineEntries) {
        $markdown.Add(('- `{0}` ({1})' -f $entry.path, $entry.classification))
    }
    $markdown.Add('')
}

$markdown | Set-Content -LiteralPath $markdownPath -Encoding utf8

Write-Host (
    'PowerShell scan: {0} checked, {1} valid, {2} known debt, {3} blocking, {4} unbaselined, {5} diagnostics.' -f
    $summary.filesChecked,
    $summary.validFiles,
    $summary.knownDebtFiles,
    $summary.blockingFailedFiles,
    $summary.unbaselinedFailedFiles,
    $summary.parseErrors
)

if ($blockingFailureCount -gt 0) {
    Write-Error "PowerShell syntax validation found $blockingFailureCount blocking failed file(s), including $unbaselinedFailureCount unbaselined failure(s)."
    exit 1
}

Write-Host 'PowerShell syntax policy passed. All parser failures are explicitly reviewed non-blocking legacy debt.' -ForegroundColor Green
