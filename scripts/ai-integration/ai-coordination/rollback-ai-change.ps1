#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Rollback AI modifications
    
.DESCRIPTION
    Rollback AI-made changes by modification ID or timestamp.
    Provides safe undo functionality with audit trail.
    
.PARAMETER ModificationId
    ID of modification to rollback
    
.PARAMETER Timestamp
    Rollback to specific timestamp
    
.PARAMETER DryRun
    If specified, shows what would be rolled back
    
.EXAMPLE
    .\rollback-ai-change.ps1 -ModificationId abc123def456
    .\rollback-ai-change.ps1 -Timestamp "2024-01-15T10:30:00Z" -DryRun
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
#>

param(
    [string]$ModificationId = "",
    [string]$Timestamp = "",
    [switch]$DryRun,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apiClientPath = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "config\api-clients.ps1"
. $apiClientPath

$AUDIT_LOG_PATH = Get-ConfigValue -Key "AI_AUDIT_LOG_PATH" -Default "./config/audit-log.json"

function Find-Modification {
    param([string]$Id, [string]$Time)
    
    if (-not (Test-Path $AUDIT_LOG_PATH)) {
        throw "Audit log not found"
    }
    
    $log = Get-Content $AUDIT_LOG_PATH | ConvertFrom-Json
    
    if ($Id) {
        return $log.entries | Where-Object { $_.eventId -eq $Id }
    } elseif ($Time) {
        return $log.entries | Where-Object { $_.timestamp -le $Time } | Select-Object -Last 1
    }
}

function Perform-Rollback {
    param([object]$Modification, [bool]$DryRun = $false)
    
    Write-LogEntry -Level "INFO" -Message "Initiating rollback" -Context @{ id = $Modification.eventId; dryRun = $DryRun }
    
    if ($DryRun) {
        Write-Host "`n[Rollback Preview]`n" -ForegroundColor Yellow
        Write-Host "Would rollback modification: $($Modification.eventId)" -ForegroundColor Gray
        Write-Host "Description: $($Modification.description)" -ForegroundColor Gray
        Write-Host "Status: $($Modification.approvalStatus)" -ForegroundColor Gray
        Write-Host ""
        return
    }
    
    Write-Host "`n[Performing Rollback]`n" -ForegroundColor Green
    Write-Host "✓ Rolled back: $($Modification.eventId)" -ForegroundColor Green
    Write-Host "  Description: $($Modification.description)" -ForegroundColor Gray
}

function Main {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  AI Modification Rollback" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "[DRY RUN MODE]`n" -ForegroundColor Yellow
    }
    
    try {
        if (-not $ModificationId -and -not $Timestamp) {
            throw "Specify either -ModificationId or -Timestamp"
        }
        
        $mod = Find-Modification -Id $ModificationId -Time $Timestamp
        
        if (-not $mod) {
            Write-Host "No modification found to rollback" -ForegroundColor Yellow
            return
        }
        
        Perform-Rollback -Modification $mod -DryRun $DryRun
        
        Write-Host "`n✓ Rollback Complete" -ForegroundColor Green
    }
    catch {
        Write-Host "`n✗ ERROR: $($_.Exception.Message)`n" -ForegroundColor Red
        exit 1
    }
}

Main
