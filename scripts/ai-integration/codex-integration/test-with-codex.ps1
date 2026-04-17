#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate test cases using Codex AI
    
.DESCRIPTION
    Automatically generates comprehensive test cases and test utilities for HELIOS Platform code.
    
.PARAMETER CodeFile
    Path to code file to generate tests for
    
.PARAMETER TestFramework
    Test framework: xunit, nunit, pester, pytest
    
.PARAMETER Coverage
    Test coverage focus: unit, integration, performance, all
    
.PARAMETER DryRun
    If specified, shows tests without execution
    
.EXAMPLE
    .\test-with-codex.ps1 -CodeFile ./build.ps1
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$CodeFile,
    [ValidateSet("xunit", "nunit", "pester", "pytest")]
    [string]$TestFramework = "pester",
    [ValidateSet("unit", "integration", "performance", "all")]
    [string]$Coverage = "unit",
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

if ($Verbose) { [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true") }

function Generate-TestCases {
    param([string]$CodeFile, [string]$TestFramework, [string]$Coverage, [bool]$DryRun = $false)
    
    if (-not (Test-Path $CodeFile)) { throw "File not found: $CodeFile" }
    
    $code = Get-Content $CodeFile -Raw
    Write-LogEntry -Level "INFO" -Message "Generating test cases" -Context @{ file = $CodeFile; framework = $TestFramework }
    
    $prompt = @"
Generate comprehensive $TestFramework test cases for this code with $Coverage coverage:

File: $(Split-Path $CodeFile -Leaf)

Code:
\`\`\`
$code
\`\`\`

Generate tests covering:
- All functions and methods
- Happy path scenarios
- Error conditions
- Edge cases
- Performance benchmarks (if applicable)

Use $TestFramework best practices and conventions.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = "Expert test engineer. Generate production-ready tests." }
            @{ role = "user"; content = $prompt }
        )
        max_tokens = 4096
        temperature = 0.3
    }
    
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $request -MaxRetries 3 -DryRun $DryRun
    Log-APICall -Service "codex" -Action "Generate-Tests" -Status "success" -Duration 0 -DryRun $DryRun
    
    return $response
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Test Case Generator" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        Write-Host "Generating test cases...`nSource: $CodeFile`nFramework: $TestFramework`nCoverage: $Coverage`n" -ForegroundColor Magenta
        
        $tests = Generate-TestCases -CodeFile $CodeFile -TestFramework $TestFramework -Coverage $Coverage -DryRun $DryRun
        
        if ($tests.success -or $tests.isDryRun) {
            $content = if ($tests.isDryRun) { "# Generated test cases" } else { $tests.content.choices[0].message.content }
            Write-Host "[Generated Test Cases]`n" -ForegroundColor Green
            Write-Host $content
        }
        
        Write-Host "`n✓ Generation Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
