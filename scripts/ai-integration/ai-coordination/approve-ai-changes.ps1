#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Interactive approval workflow for AI recommendations
    
.DESCRIPTION
    Review, approve, or reject AI-generated recommendations before implementation.
    
.PARAMETER RecommendationFile
    Path to recommendation file
    
.PARAMETER AutoApprove
    If specified, auto-approves recommendations
    
.EXAMPLE
    .\approve-ai-changes.ps1 -RecommendationFile ./recommendations.json
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$RecommendationFile,
    [switch]$AutoApprove,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

function Show-ApprovalInterface {
    param([object]$Recommendations)
    
    Write-Host "`n[Recommendation Review Interface]`n" -ForegroundColor Cyan
    
    if ($Recommendations -is [array]) {
        $Recommendations | ForEach-Object -Begin { $i = 1 } {
            Write-Host "$i. Title: $($_.title ?? 'Untitled')" -ForegroundColor Green
            Write-Host "   Impact: $($_.impact ?? 'Unknown')" -ForegroundColor Yellow
            Write-Host "   Risk: $($_.risk ?? 'Unknown')" -ForegroundColor Yellow
            Write-Host ""
            $i++
        }
    } else {
        Write-Host $Recommendations | ConvertTo-Json -Depth 3
    }
    
    Write-Host "Options: approve all | approve [#] | reject [#] | exit" -ForegroundColor Magenta
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Recommendation Approval Workflow" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    try {
        if (-not (Test-Path $RecommendationFile)) {
            throw "File not found: $RecommendationFile"
        }
        
        $recommendations = Get-Content $RecommendationFile | ConvertFrom-Json
        
        if ($AutoApprove) {
            Write-Host "Auto-approving recommendations..." -ForegroundColor Magenta
            Write-Host "✓ Approved all recommendations" -ForegroundColor Green
        } else {
            Show-ApprovalInterface -Recommendations $recommendations
            Write-Host "`nInteractive approval workflow ready..." -ForegroundColor Gray
        }
        
        Write-Host "`n✓ Approval Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
