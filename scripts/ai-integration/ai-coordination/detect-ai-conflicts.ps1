#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Detect conflicts between AI recommendations
    
.DESCRIPTION
    Analyzes multiple AI recommendations to find conflicts and provides
    resolution suggestions.
    
.PARAMETER RecommendationFiles
    Paths to recommendation files to analyze
    
.PARAMETER DryRun
    If specified, shows analysis without changes
    
.EXAMPLE
    .\detect-ai-conflicts.ps1 -RecommendationFiles @('./rec1.json', './rec2.json')
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$true)]
    [string[]]$RecommendationFiles,
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

if ($Verbose) { [Environment]::SetEnvironmentVariable("DEBUG_MODE", "true") }

function Analyze-Conflicts {
    param([string[]]$Files, [bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Analyzing for conflicts"
    
    $recommendations = @()
    foreach ($file in $Files) {
        if (Test-Path $file) {
            $recommendations += Get-Content $file -Raw
        }
    }
    
    $prompt = @"
Analyze these HELIOS AI recommendations for conflicts, resource contention, and timing issues:

Recommendations:
$($recommendations -join "`n---`n")

Identify and report:
1. Direct contradictions
2. Resource conflicts
3. Execution order dependencies
4. Severity assessment
5. Resolution suggestions

Format results as structured conflict analysis.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = "Expert conflict analyst for build recommendations." }
            @{ role = "user"; content = $prompt }
        )
        max_tokens = 2048
        temperature = 0.3
    }
    
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $request -MaxRetries 3 -DryRun $DryRun
    Log-APICall -Service "ai-coordination" -Action "Detect-Conflicts" -Status "success" -Duration 0 -DryRun $DryRun
    
    return $response
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Conflict Detection" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        Write-Host "Analyzing $($RecommendationFiles.Count) recommendation(s)...`n" -ForegroundColor Magenta
        
        $analysis = Analyze-Conflicts -Files $RecommendationFiles -DryRun $DryRun
        
        if ($analysis.success -or $analysis.isDryRun) {
            $content = if ($analysis.isDryRun) { "# Conflict Analysis Results" } else { $analysis.content.choices[0].message.content }
            Write-Host "`n[Analysis Results]`n" -ForegroundColor Green
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
