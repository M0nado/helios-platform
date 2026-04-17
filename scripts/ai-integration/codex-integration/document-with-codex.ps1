#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Auto-generate code documentation using Codex
    
.DESCRIPTION
    Generates comprehensive documentation for code files.
    
.PARAMETER FilePath
    Path to code file to document
    
.PARAMETER DocFormat
    Documentation format: markdown, xml, doxygen
    
.PARAMETER DryRun
    If specified, shows documentation without saving
    
.EXAMPLE
    .\document-with-codex.ps1 -FilePath ./build.ps1
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,
    [ValidateSet("markdown", "xml", "doxygen")]
    [string]$DocFormat = "markdown",
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

if ($Verbose) { [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true") }

function Generate-Documentation {
    param([string]$FilePath, [string]$DocFormat, [bool]$DryRun = $false)
    
    if (-not (Test-Path $FilePath)) { throw "File not found: $FilePath" }
    
    $code = Get-Content $FilePath -Raw
    Write-LogEntry -Level "INFO" -Message "Generating documentation"
    
    $prompt = @"
Generate comprehensive $DocFormat documentation for this code:

File: $(Split-Path $FilePath -Leaf)

Code:
\`\`\`
$code
\`\`\`

Document all functions, parameters, returns, examples, and usage patterns using $DocFormat format.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = "Generate clear documentation." }
            @{ role = "user"; content = $prompt }
        )
        max_tokens = 3000
        temperature = 0.3
    }
    
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $request -MaxRetries 3 -DryRun $DryRun
    Log-APICall -Service "codex" -Action "Generate-Docs" -Status "success" -Duration 0 -DryRun $DryRun
    
    return $response
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Code Documentation Generator" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        Write-Host "Generating $DocFormat documentation for: $FilePath`n" -ForegroundColor Magenta
        
        $docs = Generate-Documentation -FilePath $FilePath -DocFormat $DocFormat -DryRun $DryRun
        
        if ($docs.success -or $docs.isDryRun) {
            $content = if ($docs.isDryRun) { "# Generated Documentation" } else { $docs.content.choices[0].message.content }
            Write-Host "`n[Generated Documentation]`n" -ForegroundColor Green
            Write-Host $content
        }
        
        Write-Host "`n✓ Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
