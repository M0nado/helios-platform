#!/usr/bin/env pwsh
<#
.SYNOPSIS
    View AI change history and version tracking
    
.DESCRIPTION
    Display history of AI-made modifications with version tracking,
    change details, and rollback information.
    
.PARAMETER Filter
    Filter by: all, pending, approved, rejected
    
.PARAMETER Limit
    Number of entries to show
    
.EXAMPLE
    .\view-ai-version-history.ps1 -Filter approved -Limit 20
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [ValidateSet("all", "pending", "approved", "rejected")]
    [string]$Filter = "all",
    [int]$Limit = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

$AUDIT_LOG_PATH = Get-ConfigValue -Key "AI_AUDIT_LOG_PATH" -Default "./config/audit-log.json"

function Show-VersionHistory {
    param([string]$FilterType, [int]$MaxEntries)
    
    if (-not (Test-Path $AUDIT_LOG_PATH)) {
        Write-Host "No version history available" -ForegroundColor Yellow
        return
    }
    
    $log = Get-Content $AUDIT_LOG_PATH | ConvertFrom-Json
    
    Write-Host "`n[AI Modification History]`n" -ForegroundColor Cyan
    Write-Host "Total Entries: $($log.totalEntries)" -ForegroundColor Yellow
    Write-Host "Last Updated: $($log.lastModified)`n" -ForegroundColor Yellow
    
    $filtered = $log.entries
    if ($FilterType -ne "all") {
        $filtered = $filtered | Where-Object { $_.approvalStatus -eq $FilterType }
    }
    
    $filtered | Select-Object -Last $MaxEntries | ForEach-Object {
        Write-Host "[$($_.timestamp)] $($_.eventId)" -ForegroundColor Green
        Write-Host "  Description: $($_.description)" -ForegroundColor Gray
        Write-Host "  Status: $($_.approvalStatus)" -ForegroundColor Yellow
        Write-Host ""
    }
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Change History" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    
    try {
        Show-VersionHistory -FilterType $Filter -MaxEntries $Limit
        Write-Host "✓ Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
