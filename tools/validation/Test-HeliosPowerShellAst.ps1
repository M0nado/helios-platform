#Requires -Version 7.0
<#
.SYNOPSIS
    Parses active HELIOS PowerShell source through the native PowerShell AST.

.DESCRIPTION
    This validator is read-only. It excludes inert legacy/reference trees and
    generated dependency/cache directories by default, then emits a deterministic
    JSON report. Any parser error fails the process.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$OutputPath = (Join-Path $RepositoryRoot 'artifacts\validation\powershell-ast.json'),
    [switch]$IncludeLegacy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$excludedSegments = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.git', '.venv', 'node_modules', 'packages', 'bin', 'obj',
    'artifacts', '.tox', '.pytest_cache', '__pycache__'
) | ForEach-Object { [void]$excludedSegments.Add($_) }

if (-not $IncludeLegacy) {
    @('legacy', 'reference', 'references', 'archive', 'archived', 'raw-disabled') |
        ForEach-Object { [void]$excludedSegments.Add($_) }
}

function Test-ExcludedPath {
    param([Parameter(Mandatory)][string]$Path)

    $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $Path)
    foreach ($segment in ($relative -split '[\\/]')) {
        if ($excludedSegments.Contains($segment)) {
            return $true
        }
    }
    return $false
}

$files = @(
    Get-ChildItem -LiteralPath $RepositoryRoot -Filter '*.ps1' -File -Recurse |
        Where-Object { -not (Test-ExcludedPath -Path $_.FullName) } |
        Sort-Object FullName
)

$records = [Collections.Generic.List[object]]::new()
$totalErrors = 0
foreach ($file in $files) {
    $tokens = $null
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile(
        $file.FullName,
        [ref]$tokens,
        [ref]$errors
    )

    $errorRecords = @(
        foreach ($parseError in @($errors)) {
            [ordered]@{
                message = $parseError.Message
                errorId = $parseError.ErrorId
                startLine = $parseError.Extent.StartLineNumber
                startColumn = $parseError.Extent.StartColumnNumber
                endLine = $parseError.Extent.EndLineNumber
                endColumn = $parseError.Extent.EndColumnNumber
                text = $parseError.Extent.Text
            }
        }
    )
    $totalErrors += $errorRecords.Count

    $records.Add([ordered]@{
        path = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\\', '/')
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        parserErrors = $errorRecords
    })
}

$result = [ordered]@{
    schemaVersion = 1
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    repositoryRoot = $RepositoryRoot
    includeLegacy = [bool]$IncludeLegacy
    filesParsed = $files.Count
    parserErrorCount = $totalErrors
    status = if ($totalErrors -eq 0) { 'passed' } else { 'failed' }
    files = @($records)
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
$result | ConvertTo-Json -Depth 10

if ($totalErrors -gt 0) {
    exit 1
}
