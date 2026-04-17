<#
.SYNOPSIS
View and manage build agent logs

.DESCRIPTION
Display, tail, search, and manage logs from build agent executions.
Supports real-time log following and advanced filtering.

.PARAMETER List
List all available agent logs

.PARAMETER AgentId
Agent number (1-11) to view logs for

.PARAMETER Follow
Follow log in real-time (tail -f)

.PARAMETER Lines
Number of lines to display (default: 50)

.PARAMETER Search
Search for text in logs

.PARAMETER Level
Filter by log level (INFO, WARN, ERROR, SUCCESS)

.PARAMETER Since
Show logs since a specific time (e.g., "30 minutes ago")

.PARAMETER Clean
Delete old logs (older than specified days, default: 30)

.EXAMPLE
.\view-agent-logs.ps1 -List
.\view-agent-logs.ps1 -AgentId 3
.\view-agent-logs.ps1 -AgentId 5 -Follow
.\view-agent-logs.ps1 -AgentId 2 -Search "error"
.\view-agent-logs.ps1 -Clean -CleanDays 7

.NOTES
Logs are stored in the logs/ directory
Each agent and orchestration run has its own log file
#>

param(
    [switch]$List,
    [int]$AgentId = 0,
    [switch]$Follow,
    [int]$Lines = 50,
    [string]$Search = "",
    [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS")]
    [string]$Level = "",
    [string]$Since = "",
    [switch]$Clean,
    [int]$CleanDays = 30
)

$ErrorActionPreference = "Continue"

# Define paths
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommandPath
$BaseDir = Split-Path -Parent $ScriptRoot
$LogsPath = Join-Path $BaseDir "logs"

# Ensure logs directory exists
if (-not (Test-Path $LogsPath)) {
    Write-Host "Logs directory not found: $LogsPath" -ForegroundColor Red
    exit 1
}

# List all logs
function Show-LogsList {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ AVAILABLE AGENT LOGS                                         ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    $logs = Get-ChildItem $LogsPath -Filter "*.log" | Sort-Object LastWriteTime -Descending
    
    if ($logs.Count -eq 0) {
        Write-Host "No logs found" -ForegroundColor Yellow
        return
    }
    
    $groups = $logs | Group-Object { $_.Name -replace '_\d{4}-\d{2}-\d{2}.*', '' }
    
    foreach ($group in $groups) {
        Write-Host "`n$($group.Name):" -ForegroundColor Green
        
        $group.Group | Select-Object -First 5 | ForEach-Object {
            $size = "{0:N0}" -f $_.Length
            Write-Host "  • $($_.Name) ($size bytes, $($_.LastWriteTime))" -ForegroundColor Gray
        }
        
        if ($group.Group.Count -gt 5) {
            Write-Host "  ... and $($group.Group.Count - 5) more" -ForegroundColor Gray
        }
    }
    
    Write-Host "`nTotal logs: $($logs.Count)" -ForegroundColor White
}

# Get agent log
function Get-AgentLogPath {
    param([int]$AgentId)
    
    if ($AgentId -lt 1 -or $AgentId -gt 11) {
        Write-Host "Invalid agent ID. Must be between 1 and 11." -ForegroundColor Red
        return $null
    }
    
    $agentPattern = "agent-$AgentId*"
    $logs = Get-ChildItem $LogsPath -Filter "$agentPattern.log" | Sort-Object LastWriteTime -Descending
    
    if ($logs.Count -eq 0) {
        Write-Host "No logs found for agent-$AgentId" -ForegroundColor Yellow
        return $null
    }
    
    return $logs[0].FullName
}

# Display agent logs
function Show-AgentLogs {
    param(
        [int]$AgentId,
        [int]$Lines,
        [string]$SearchText,
        [string]$LogLevel,
        [string]$SinceTime
    )
    
    $logPath = Get-AgentLogPath -AgentId $AgentId
    
    if ($null -eq $logPath) {
        return
    }
    
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ AGENT-$($AgentId.ToString().PadLeft(2)) LOG                                              ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host "File: $logPath`n" -ForegroundColor Gray
    
    $content = Get-Content $logPath
    
    # Filter by level
    if ($LogLevel) {
        $content = $content | Where-Object { $_ -match "\[$LogLevel\]" }
    }
    
    # Filter by search text
    if ($SearchText) {
        $content = $content | Where-Object { $_ -match $SearchText }
    }
    
    # Filter by time
    if ($SinceTime) {
        $sinceDate = Get-Date $SinceTime
        $content = $content | Where-Object {
            $match = [regex]::Match($_, '\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\]')
            if ($match.Success) {
                $logDate = Get-Date $match.Groups[1].Value
                $logDate -ge $sinceDate
            }
        }
    }
    
    # Display with color coding
    if ($null -eq $content) {
        $content = @()
    }
    elseif ($content -is [string]) {
        $content = @($content)
    }
    
    $content | Select-Object -Last $Lines | ForEach-Object {
        $line = $_
        
        if ($line -match "\[ERROR\]") {
            Write-Host $line -ForegroundColor Red
        }
        elseif ($line -match "\[WARN\]") {
            Write-Host $line -ForegroundColor Yellow
        }
        elseif ($line -match "\[SUCCESS\]") {
            Write-Host $line -ForegroundColor Green
        }
        else {
            Write-Host $line -ForegroundColor White
        }
    }
    
    Write-Host "`nShowing last $Lines lines" -ForegroundColor Gray
}

# Follow agent logs
function Follow-AgentLogs {
    param([int]$AgentId)
    
    $logPath = Get-AgentLogPath -AgentId $AgentId
    
    if ($null -eq $logPath) {
        return
    }
    
    Write-Host "Following logs for agent-$AgentId`nPress Ctrl+C to stop`n" -ForegroundColor Yellow
    
    Get-Content -Path $logPath -Wait -Tail 50 | ForEach-Object {
        $line = $_
        
        if ($line -match "\[ERROR\]") {
            Write-Host $line -ForegroundColor Red
        }
        elseif ($line -match "\[WARN\]") {
            Write-Host $line -ForegroundColor Yellow
        }
        elseif ($line -match "\[SUCCESS\]") {
            Write-Host $line -ForegroundColor Green
        }
        else {
            Write-Host $line -ForegroundColor White
        }
    }
}

# Search logs
function Search-Logs {
    param(
        [string]$SearchText,
        [string]$LogLevel
    )
    
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ LOG SEARCH RESULTS                                           ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host "Searching for: '$SearchText'" -ForegroundColor Gray
    if ($LogLevel) {
        Write-Host "Filter: Level = $LogLevel" -ForegroundColor Gray
    }
    Write-Host ""
    
    $logs = Get-ChildItem $LogsPath -Filter "*.log"
    $matches = @()
    
    foreach ($log in $logs) {
        $content = Get-Content $log.FullName
        
        $filtered = $content | Where-Object { $_ -match $SearchText }
        
        if ($LogLevel) {
            $filtered = $filtered | Where-Object { $_ -match "\[$LogLevel\]" }
        }
        
        if ($filtered) {
            foreach ($line in $filtered) {
                $matches += @{
                    File = $log.Name
                    Line = $line
                }
            }
        }
    }
    
    if ($matches.Count -eq 0) {
        Write-Host "No matches found" -ForegroundColor Yellow
        return
    }
    
    $groups = $matches | Group-Object { $_.File }
    
    foreach ($group in $groups) {
        Write-Host "`n$($group.Name) ($($group.Group.Count) matches):" -ForegroundColor Green
        
        foreach ($match in $group.Group | Select-Object -First 10) {
            $line = $match.Line
            if ($line -match "\[ERROR\]") {
                Write-Host "  $line" -ForegroundColor Red
            }
            elseif ($line -match "\[WARN\]") {
                Write-Host "  $line" -ForegroundColor Yellow
            }
            else {
                Write-Host "  $line" -ForegroundColor Gray
            }
        }
        
        if ($group.Group.Count -gt 10) {
            Write-Host "  ... and $($group.Group.Count - 10) more" -ForegroundColor Gray
        }
    }
    
    Write-Host "`nTotal matches: $($matches.Count)" -ForegroundColor White
}

# Clean old logs
function Remove-OldLogs {
    param([int]$DaysOld)
    
    $cutoffDate = (Get-Date).AddDays(-$DaysOld)
    $logs = Get-ChildItem $LogsPath -Filter "*.log"
    $removed = @()
    
    foreach ($log in $logs) {
        if ($log.LastWriteTime -lt $cutoffDate) {
            Remove-Item $log.FullName -Force
            $removed += $log.Name
        }
    }
    
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║ LOG CLEANUP COMPLETED                                        ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host "`nRemoved logs older than $DaysOld days ($cutoffDate)" -ForegroundColor Yellow
    
    if ($removed.Count -eq 0) {
        Write-Host "No logs to remove" -ForegroundColor Gray
    }
    else {
        Write-Host "Removed $($removed.Count) log files:" -ForegroundColor White
        $removed | ForEach-Object { Write-Host "  • $_" -ForegroundColor Gray }
    }
}

# Main execution
Write-Host ""

if ($Clean) {
    Remove-OldLogs -DaysOld $CleanDays
}
elseif ($List) {
    Show-LogsList
}
elseif ($AgentId -gt 0) {
    if ($Follow) {
        Follow-AgentLogs -AgentId $AgentId
    }
    else {
        Show-AgentLogs -AgentId $AgentId -Lines $Lines -SearchText $Search -LogLevel $Level -SinceTime $Since
    }
}
elseif ($Search) {
    Search-Logs -SearchText $Search -LogLevel $Level
}
else {
    Show-LogsList
}

Write-Host ""
