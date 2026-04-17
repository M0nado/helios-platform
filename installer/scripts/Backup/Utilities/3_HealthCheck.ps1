#################################################################################
# HELIOS Platform - Backup Health Check
# Purpose: Performs comprehensive backup system health assessment
# Version: 1.0.0
#################################################################################

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "HealthCheck"
Write-BackupLog -Message "Backup System Health Check Started" -Level "INFO" -LogFile $logFile

try {
    # Run comprehensive health check
    $healthReport = Test-BackupHealth
    
    Write-BackupLog -Message "Health Check Results:" -Level "INFO" -LogFile $logFile
    Write-BackupLog -Message "  Overall Status: $($healthReport.Status)" -Level "INFO" -LogFile $logFile
    
    foreach ($check in $healthReport.Checks) {
        $status = $check.Status
        $color = if ($status -eq "OK") { "Green" } elseif ($status -eq "WARNING") { "Yellow" } else { "Red" }
        Write-BackupLog -Message "  - $($check.CheckName): $status" -Level $status -LogFile $logFile
        
        if ($check.Details) {
            Write-BackupLog -Message "    Details: $($check.Details | ConvertTo-Json)" -LogFile $logFile
        }
    }
    
    # Get storage status
    $storageStatus = Get-BackupStorageStatus
    Write-BackupLog -Message "Storage Status:" -Level "INFO" -LogFile $logFile
    Write-BackupLog -Message "  Disk Usage: $($storageStatus.DiskUsagePercent)%" -LogFile $logFile
    Write-BackupLog -Message "  Available Space: $(($storageStatus.AvailableDriveSpace/1GB).ToString('F2')) GB" -LogFile $logFile
    Write-BackupLog -Message "  Backup Size: $(($storageStatus.BackupTotalSize/1GB).ToString('F2')) GB" -LogFile $logFile
    
    # Send alert if health is degraded
    if ($healthReport.Status -ne "Healthy") {
        Write-BackupLog -Message "Health Alert: System is in $($healthReport.Status) state" -Level "WARN" -LogFile $logFile
    }
    
    Write-BackupLog -Message "Health Check Completed" -Level "SUCCESS" -LogFile $logFile
    
    return $healthReport
}
catch {
    Write-BackupLog -Message "Health Check FAILED: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
