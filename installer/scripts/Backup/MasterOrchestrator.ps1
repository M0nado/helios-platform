#################################################################################
# HELIOS Platform - Master Backup Orchestrator
# Purpose: Central point for executing all backup operations
# Version: 1.0.0
#################################################################################

param(
    [ValidateSet("FullBackup", "IncrementalBackup", "DifferentialBackup", "CloudSync", "Verify", "HealthCheck", "RestorePointInTime", "RestoreDisaster")]
    [string]$Operation = "FullBackup",
    
    [string]$BackupId,
    
    [DateTime]$RecoveryTime,
    
    [bool]$SendNotifications = $true
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$orchestratorLog = Initialize-BackupLogging -LogType "Orchestrator"
Write-BackupLog -Message "HELIOS Backup Orchestrator - Operation: $Operation" -Level "INFO" -LogFile $orchestratorLog

$operationResult = @{
    Operation = $Operation
    StartTime = Get-Date
    Status = "In Progress"
    Details = @{}
}

try {
    switch ($Operation) {
        "FullBackup" {
            Write-BackupLog -Message "Executing Full System Backup..." -LogFile $orchestratorLog
            $result = & "C:\HELIOS\Scripts\Backup\1_FullBackup.ps1"
            $operationResult.BackupId = $result.BackupId
            $operationResult.Details = $result
        }
        
        "IncrementalBackup" {
            Write-BackupLog -Message "Executing Incremental Backup..." -LogFile $orchestratorLog
            $result = & "C:\HELIOS\Scripts\Backup\2_IncrementalBackup.ps1"
            $operationResult.BackupId = $result.BackupId
            $operationResult.Details = $result
        }
        
        "DifferentialBackup" {
            Write-BackupLog -Message "Executing Differential Backup..." -LogFile $orchestratorLog
            $result = & "C:\HELIOS\Scripts\Backup\3_DifferentialBackup.ps1"
            $operationResult.BackupId = $result.BackupId
            $operationResult.Details = $result
        }
        
        "CloudSync" {
            Write-BackupLog -Message "Executing Cloud Sync..." -LogFile $orchestratorLog
            if ($BackupId) {
                $result = & "C:\HELIOS\Scripts\Backup\4_CloudBackup.ps1" -BackupId $BackupId
                $operationResult.Details = $result
            }
            else {
                throw "BackupId required for CloudSync operation"
            }
        }
        
        "Verify" {
            Write-BackupLog -Message "Executing Backup Verification..." -LogFile $orchestratorLog
            if ($BackupId) {
                $result = & "C:\HELIOS\Scripts\Backup\Utilities\1_BackupVerification.ps1" -BackupId $BackupId
                $operationResult.Details = $result
            }
            else {
                throw "BackupId required for Verify operation"
            }
        }
        
        "HealthCheck" {
            Write-BackupLog -Message "Executing Health Check..." -LogFile $orchestratorLog
            $result = & "C:\HELIOS\Scripts\Backup\Utilities\3_HealthCheck.ps1"
            $operationResult.Details = $result
        }
        
        "RestorePointInTime" {
            Write-BackupLog -Message "Executing Point-in-Time Recovery..." -LogFile $orchestratorLog
            if ($RecoveryTime) {
                $result = & "C:\HELIOS\Scripts\Backup\Recovery\1_PointInTimeRecovery.ps1" -RecoveryTime $RecoveryTime -DryRun $true
                $operationResult.Details = $result
            }
            else {
                throw "RecoveryTime required for RestorePointInTime operation"
            }
        }
        
        "RestoreDisaster" {
            Write-BackupLog -Message "Executing Disaster Recovery..." -LogFile $orchestratorLog
            if ($BackupId) {
                $result = & "C:\HELIOS\Scripts\Backup\Recovery\2_DisasterRecovery.ps1" -BackupId $BackupId -DryRun $true
                $operationResult.Details = $result
            }
            else {
                throw "BackupId required for RestoreDisaster operation"
            }
        }
    }
    
    $operationResult.Status = if ($result.Success) { "Completed" } else { "Failed" }
    
    Write-BackupLog -Message "Operation completed with status: $($operationResult.Status)" -Level $(if($operationResult.Status -eq 'Completed'){"SUCCESS"}else{"ERROR"}) -LogFile $orchestratorLog
    
    # Send notification
    if ($SendNotifications) {
        $notifStatus = if ($operationResult.Status -eq "Completed") { "Completed" } else { "Failed" }
        & "C:\HELIOS\Scripts\Backup\Utilities\Send-BackupAlert.ps1" -Status $notifStatus -Message "Backup operation: $Operation" -Details $operationResult
    }
}
catch {
    $operationResult.Status = "Failed"
    $operationResult.Error = $_
    
    Write-BackupLog -Message "Operation FAILED: $_" -Level "ERROR" -LogFile $orchestratorLog
    
    if ($SendNotifications) {
        & "C:\HELIOS\Scripts\Backup\Utilities\Send-BackupAlert.ps1" -Status "Failed" -Message "Backup operation $Operation failed: $_"
    }
}

$operationResult.EndTime = Get-Date
$operationResult.Duration = ($operationResult.EndTime - $operationResult.StartTime).TotalSeconds

return $operationResult
