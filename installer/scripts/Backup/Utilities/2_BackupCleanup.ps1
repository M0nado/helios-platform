#################################################################################
# HELIOS Platform - Backup Cleanup Utility
# Purpose: Performs retention policy enforcement and old backup cleanup
# Version: 1.0.0
#################################################################################

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "CleanupUtility"
Write-BackupLog -Message "Backup Cleanup Utility Started" -Level "INFO" -LogFile $logFile

try {
    Write-BackupLog -Message "Enforcing retention policies..." -Level "INFO" -LogFile $logFile
    
    $cleanupResults = Cleanup-BackupDirectories
    
    Write-BackupLog -Message "Cleanup Summary:" -Level "INFO" -LogFile $logFile
    Write-BackupLog -Message "  - Full Backups Removed: $($cleanupResults.FullBackupsRemoved)" -LogFile $logFile
    Write-BackupLog -Message "  - Metadata Files Removed: $($cleanupResults.MetadataFilesRemoved)" -LogFile $logFile
    
    # Calculate storage freed
    Write-BackupLog -Message "Cleanup Completed Successfully!" -Level "SUCCESS" -LogFile $logFile
    
    return @{
        Success = $true
        Results = $cleanupResults
    }
}
catch {
    Write-BackupLog -Message "Cleanup FAILED: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
