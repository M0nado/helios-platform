#################################################################################
# HELIOS Platform - Backup Scheduling Configuration
# Purpose: Sets up Windows Task Scheduler jobs for automated backups
# Version: 1.0.0
#################################################################################

param(
    [ValidateSet("Setup", "Disable", "List")]
    [string]$Action = "Setup"
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "Scheduling"

#################################################################################
# Task Definitions
#################################################################################

$backupTasks = @(
    @{
        Name = "HELIOS-FullBackup-Daily"
        Script = "C:\HELIOS\Scripts\Backup\1_FullBackup.ps1"
        Trigger = "Daily"
        Time = "22:00"
        Description = "Daily full system backup"
        Priority = 7
    }
    @{
        Name = "HELIOS-IncrementalBackup-Frequent"
        Script = "C:\HELIOS\Scripts\Backup\2_IncrementalBackup.ps1"
        Trigger = "Repeating"
        Time = "06:00"
        Interval = 360
        Description = "Incremental backup every 6 hours"
        Priority = 8
    }
    @{
        Name = "HELIOS-DifferentialBackup-Daily"
        Script = "C:\HELIOS\Scripts\Backup\3_DifferentialBackup.ps1"
        Trigger = "Daily"
        Time = "12:00"
        Description = "Daily differential backup"
        Priority = 7
    }
    @{
        Name = "HELIOS-CloudSync-Hourly"
        Script = "C:\HELIOS\Scripts\Backup\4_CloudBackup.ps1"
        Trigger = "Repeating"
        Time = "08:00"
        Interval = 60
        Description = "Hourly cloud backup sync"
        Priority = 5
    }
    @{
        Name = "HELIOS-BackupVerification-Weekly"
        Script = "C:\HELIOS\Scripts\Backup\Utilities\1_BackupVerification.ps1"
        Trigger = "Weekly"
        DayOfWeek = "Sunday"
        Time = "03:00"
        Description = "Weekly backup verification"
        Priority = 6
    }
    @{
        Name = "HELIOS-BackupCleanup-Weekly"
        Script = "C:\HELIOS\Scripts\Backup\Utilities\2_BackupCleanup.ps1"
        Trigger = "Weekly"
        DayOfWeek = "Saturday"
        Time = "02:00"
        Description = "Weekly cleanup of expired backups"
        Priority = 4
    }
    @{
        Name = "HELIOS-HealthCheck-Daily"
        Script = "C:\HELIOS\Scripts\Backup\Utilities\3_HealthCheck.ps1"
        Trigger = "Daily"
        Time = "06:00"
        Description = "Daily backup system health check"
        Priority = 9
    }
)

#################################################################################
# Setup Functions
#################################################################################

function New-BackupTask {
    param(
        [hashtable]$TaskDefinition
    )
    
    Write-BackupLog -Message "Creating scheduled task: $($TaskDefinition.Name)" -LogFile $logFile
    
    try {
        # Create PowerShell command
        $psCommand = @"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$($TaskDefinition.Script)"
"@
        
        # Create task action
        $action = New-ScheduledTaskAction -Execute "powershell.exe" `
            -Argument "-NoProfile -ExecutionPolicy Bypass -File '$($TaskDefinition.Script)'"
        
        # Create trigger based on type
        $trigger = switch ($TaskDefinition.Trigger) {
            "Daily" {
                New-ScheduledTaskTrigger -Daily -At $TaskDefinition.Time
            }
            "Weekly" {
                New-ScheduledTaskTrigger -Weekly -DaysOfWeek $TaskDefinition.DayOfWeek -At $TaskDefinition.Time
            }
            "Repeating" {
                $startTrigger = New-ScheduledTaskTrigger -Daily -At $TaskDefinition.Time
                $repeatInterval = New-TimeSpan -Minutes $TaskDefinition.Interval
                $startTrigger.Repetition.Duration = [timespan]::MaxValue
                $startTrigger.Repetition.Interval = $repeatInterval
                $startTrigger
            }
        }
        
        # Create task settings
        $settings = New-ScheduledTaskSettingsSet `
            -AllowStartIfOnBatteries `
            -DontStopIfGoingOnBatteries `
            -StartWhenAvailable `
            -RunOnlyIfNetworkAvailable `
            -MultipleInstances IgnoreNew `
            -Priority $TaskDefinition.Priority
        
        # Create principal (run as SYSTEM)
        $principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest
        
        # Create the task
        $taskPath = "\HELIOS\Backup\"
        Register-ScheduledTask -TaskName $TaskDefinition.Name `
            -TaskPath $taskPath `
            -Action $action `
            -Trigger $trigger `
            -Settings $settings `
            -Principal $principal `
            -Description $TaskDefinition.Description `
            -Force
        
        Write-BackupLog -Message "Scheduled task created successfully: $($TaskDefinition.Name)" -Level "SUCCESS" -LogFile $logFile
        return $true
    }
    catch {
        Write-BackupLog -Message "Failed to create scheduled task $($TaskDefinition.Name): $_" -Level "ERROR" -LogFile $logFile
        return $false
    }
}

function Remove-BackupTask {
    param(
        [string]$TaskName
    )
    
    Write-BackupLog -Message "Removing scheduled task: $TaskName" -LogFile $logFile
    
    try {
        Unregister-ScheduledTask -TaskName $TaskName -TaskPath "\HELIOS\Backup\" -Confirm:$false
        Write-BackupLog -Message "Scheduled task removed successfully: $TaskName" -Level "SUCCESS" -LogFile $logFile
        return $true
    }
    catch {
        Write-BackupLog -Message "Failed to remove scheduled task $TaskName`: $_" -Level "ERROR" -LogFile $logFile
        return $false
    }
}

function Get-BackupTaskStatus {
    
    Write-BackupLog -Message "Retrieving backup task status..." -LogFile $logFile
    
    try {
        $tasks = Get-ScheduledTask -TaskPath "\HELIOS\Backup\" -ErrorAction SilentlyContinue
        
        $status = @()
        foreach ($task in $tasks) {
            $lastRun = $task.LastRunTime
            $lastResult = $task.LastTaskResult
            
            $status += @{
                Name = $task.TaskName
                Status = $task.State
                LastRun = $lastRun
                LastResult = if ($lastResult -eq 0) { "Success" } else { "Failed ($lastResult)" }
                NextRun = $task.NextRunTime
            }
        }
        
        return $status
    }
    catch {
        Write-BackupLog -Message "Failed to retrieve task status: $_" -Level "ERROR" -LogFile $logFile
        return @()
    }
}

#################################################################################
# Main Logic
#################################################################################

switch ($Action) {
    "Setup" {
        Write-BackupLog -Message "Beginning backup task setup..." -Level "INFO" -LogFile $logFile
        
        $successCount = 0
        foreach ($task in $backupTasks) {
            if (New-BackupTask -TaskDefinition $task) {
                $successCount++
            }
        }
        
        Write-BackupLog -Message "Backup task setup completed. $successCount/$($backupTasks.Count) tasks created successfully" -Level $(if($successCount -eq $backupTasks.Count){"SUCCESS"}else{"WARN"}) -LogFile $logFile
    }
    
    "Disable" {
        Write-BackupLog -Message "Disabling all backup tasks..." -Level "INFO" -LogFile $logFile
        
        $tasks = Get-ScheduledTask -TaskPath "\HELIOS\Backup\" -ErrorAction SilentlyContinue
        foreach ($task in $tasks) {
            Disable-ScheduledTask -TaskName $task.TaskName -TaskPath "\HELIOS\Backup\"
            Write-BackupLog -Message "Disabled task: $($task.TaskName)" -LogFile $logFile
        }
        
        Write-BackupLog -Message "All backup tasks have been disabled" -Level "WARN" -LogFile $logFile
    }
    
    "List" {
        Write-BackupLog -Message "Retrieving backup task status..." -Level "INFO" -LogFile $logFile
        
        $status = Get-BackupTaskStatus
        
        if ($status.Count -gt 0) {
            Write-Host "`n=== HELIOS Backup Tasks Status ===" -ForegroundColor Cyan
            $status | Format-Table -AutoSize
            Write-BackupLog -Message "Found $($status.Count) backup tasks" -Level "INFO" -LogFile $logFile
        }
        else {
            Write-BackupLog -Message "No backup tasks found" -Level "WARN" -LogFile $logFile
        }
        
        return $status
    }
}

Write-BackupLog -Message "Scheduling configuration completed" -Level "SUCCESS" -LogFile $logFile
