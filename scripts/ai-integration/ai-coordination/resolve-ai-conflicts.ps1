#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Resolve conflicts between AI recommendations
    
.DESCRIPTION
    Resolves conflicting AI recommendations through analysis and prioritization.
    
.PARAMETER ConflictFile
    Path to conflicts file
    
.PARAMETER ResolutionStrategy
    Strategy: priority, weighted, manual
    
.EXAMPLE
    .\resolve-ai-conflicts.ps1 -ConflictFile ./conflicts.json -ResolutionStrategy priority
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ConflictFile,
    [ValidateSet("priority", "weighted", "manual")]
    [string]$ResolutionStrategy = "priority",
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

function Resolve-Conflicts {
    param([string]$File, [string]$Strategy)
    
    if (-not (Test-Path $File)) { throw "File not found: $File" }
    
    $conflicts = Get-Content $File | ConvertFrom-Json
    Write-LogEntry -Level "INFO" -Message "Resolving conflicts"
    
    $prompt = @"
Resolve these AI recommendation conflicts using the $Strategy strategy:

Conflicts:
$($conflicts | ConvertTo-Json -Depth 5)

Provide resolution with:
1. Recommended priority order
2. Rationale for each decision
3. Implementation sequence
4. Risk mitigation
5. Success metrics

Use $Strategy prioritization approach.
"@
    
    $request = @{
        model = Get-ConfigValue -Key "OPENAI_MODEL" -Default "gpt-4"
        messages = @(
            @{ role = "system"; content = "Conflict resolution expert for build recommendations." }
            @{ role = "user"; content = $prompt }
        )
        max_tokens = 2048
        temperature = 0.3
    }
    
    $response = Invoke-OpenAIRequest -Endpoint "/chat/completions" -Body $request -MaxRetries 3 -DryRun $false
    Log-APICall -Service "ai-coordination" -Action "Resolve-Conflicts" -Status "success" -Duration 0
    
    return $response
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Conflict Resolution" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        Write-Host "Resolving conflicts using $ResolutionStrategy strategy...`n" -ForegroundColor Magenta
        
        $resolution = Resolve-Conflicts -File $ConflictFile -Strategy $ResolutionStrategy
        
        if ($resolution.success) {
            Write-Host "[Resolution Plan]`n" -ForegroundColor Green
            Write-Host $resolution.content.choices[0].message.content
        }
        
        Write-Host "`n✓ Resolution Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
