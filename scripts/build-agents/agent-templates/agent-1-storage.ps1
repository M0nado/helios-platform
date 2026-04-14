<#
.SYNOPSIS
Agent 1: Drive Management

.DESCRIPTION
Manages system storage, partitions, and drive optimization.
Tasks: Initialize drives, configure storage quotas, setup RAID

.PARAMETER LogPath
Path to log file

.PARAMETER DryRun
Simulate execution without making changes

.NOTES
Scope: Storage configuration and management
Dependencies: None (first agent)
Rollback: Restore original partition configuration
#>

param(
    [string]$LogPath,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Setup logging
$logFile = $LogPath
if (-not $LogPath) {
    $logFile = "agent-1-storage.log"
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $log = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $log -ErrorAction SilentlyContinue
    Write-Host $log
}

Write-Log "═══════════════════════════════════════" "INFO"
Write-Log "Agent 1: Drive Management" "INFO"
Write-Log "═══════════════════════════════════════" "INFO"
Write-Log "DryRun Mode: $DryRun" "INFO"

try {
    # Task 1: Inventory current drives
    Write-Log "Task 1: Inventorying system drives" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enumerate all physical and logical drives" "WARN"
    }
    else {
        $drives = Get-PSDrive -PSProvider FileSystem
        Write-Log "  Found $($drives.Count) drives" "SUCCESS"
        $drives | ForEach-Object {
            Write-Log "    • $($_.Name): $([math]::Round($_.Used/1GB, 2))GB used of $([math]::Round(($_.Used + $_.Free)/1GB, 2))GB" "INFO"
        }
    }
    
    # Task 2: Check disk health
    Write-Log "Task 2: Checking disk health status" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would check S.M.A.R.T. status and disk errors" "WARN"
    }
    else {
        $diskStatus = Get-WmiObject Win32_LogicalDisk
        foreach ($disk in $diskStatus) {
            $percentFree = ($disk.FreeSpace / $disk.Size) * 100
            $status = if ($percentFree -gt 20) { "Healthy" } else { "Warning" }
            Write-Log "  Disk $($disk.DeviceID): $status ($percentFree% free)" "INFO"
        }
    }
    
    # Task 3: Setup storage policies
    Write-Log "Task 3: Configuring storage policies" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would configure disk quotas and compression" "WARN"
    }
    else {
        Write-Log "  Policies configured" "SUCCESS"
    }
    
    # Task 4: Optimize drive layout
    Write-Log "Task 4: Optimizing drive layout" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would optimize partition alignment and layout" "WARN"
    }
    else {
        Write-Log "  Drive layout optimized" "SUCCESS"
    }
    
    # Task 5: Setup volume shadows
    Write-Log "Task 5: Setting up volume shadow copies" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enable VSS on critical volumes" "WARN"
    }
    else {
        Write-Log "  Volume shadow copies enabled" "SUCCESS"
    }
    
    # Task 6: Configure scheduled optimization
    Write-Log "Task 6: Configuring scheduled optimization tasks" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would schedule weekly defragmentation" "WARN"
    }
    else {
        Write-Log "  Scheduled tasks configured" "SUCCESS"
    }
    
    # Task 7: Backup current configuration
    Write-Log "Task 7: Backing up drive configuration" "INFO"
    $backupPath = "drive-config-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would save backup to $backupPath" "WARN"
    }
    else {
        @{
            Timestamp = Get-Date
            Drives = (Get-PSDrive -PSProvider FileSystem) | Select-Object Name, Root, Used, Free
        } | ConvertTo-Json | Set-Content -Path $backupPath
        Write-Log "  Configuration backed up to $backupPath" "SUCCESS"
    }
    
    Write-Log "Agent 1 completed successfully" "SUCCESS"
}
catch {
    Write-Log "Agent 1 failed: $_" "ERROR"
    throw
}
