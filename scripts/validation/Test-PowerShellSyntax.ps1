[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ReportDirectory = 'reports/powershell-syntax'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$reportPath = Join-Path $Root $ReportDirectory
New-Item -ItemType Directory -Path $reportPath -Force | Out-Null

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
$errorCount = 0

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
            errors = @()
        })
        Write-Host "PASS $relativePath" -ForegroundColor Green
        continue
    }

    $errorCount += $parseErrors.Count
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

    $results.Add([pscustomobject]@{
        path = $relativePath
        status = 'failed'
        errors = $details
    })

    foreach ($detail in $details) {
        Write-Host (
            'FAIL {0}:{1}:{2} [{3}] {4}' -f
            $relativePath,
            $detail.line,
            $detail.column,
            $detail.errorId,
            $detail.message
        ) -ForegroundColor Red
    }
}

$summary = [pscustomobject]@{
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    root = $Root
    filesChecked = $files.Count
    failedFiles = @($results | Where-Object status -eq 'failed').Count
    parseErrors = $errorCount
    excludedSegments = $excludedSegments
    results = $results
}

$jsonPath = Join-Path $reportPath 'powershell-syntax.json'
$markdownPath = Join-Path $reportPath 'powershell-syntax.md'
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add('# PowerShell Syntax Validation')
$markdown.Add('')
$markdown.Add("- Files checked: $($summary.filesChecked)")
$markdown.Add("- Failed files: $($summary.failedFiles)")
$markdown.Add("- Parse errors: $($summary.parseErrors)")
$markdown.Add('')

foreach ($result in $results) {
    if ($result.status -eq 'passed') {
        continue
    }

    $markdown.Add("## `$($result.path)`")
    $markdown.Add('')
    foreach ($detail in $result.errors) {
        $message = $detail.message.Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ')
        $markdown.Add("- Line $($detail.line), column $($detail.column), `$($detail.errorId)`: $message")
    }
    $markdown.Add('')
}

$markdown | Set-Content -LiteralPath $markdownPath -Encoding utf8

if ($errorCount -gt 0) {
    Write-Error "PowerShell syntax validation failed with $errorCount parse error(s) across $($summary.failedFiles) file(s)."
    exit 1
}

Write-Host "PowerShell syntax validation passed for $($files.Count) file(s)." -ForegroundColor Green
