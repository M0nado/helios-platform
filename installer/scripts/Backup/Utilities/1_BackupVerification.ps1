#################################################################################
# HELIOS Platform - Backup Verification
# Purpose: Verifies backup integrity and recoverability
# Version: 1.0.0
#################################################################################

param(
    [string]$BackupId,
    [bool]$TestRestore = $false
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "BackupVerification"
Write-BackupLog -Message "Backup Verification Started for BackupId: $BackupId" -Level "INFO" -LogFile $logFile

try {
    # Get backup metadata
    $metadata = Get-BackupMetadata -BackupId $BackupId
    if (-not $metadata) {
        throw "Backup metadata not found for ID: $BackupId"
    }
    
    Write-BackupLog -Message "Verifying backup: $($metadata.BackupName)" -LogFile $logFile
    
    $verificationReport = @{
        BackupId = $BackupId
        BackupName = $metadata.BackupName
        StartTime = Get-Date
        Results = @()
        OverallStatus = "Passed"
    }
    
    #################################################################################
    # Phase 1: File Integrity Verification
    #################################################################################
    
    Write-BackupLog -Message "Phase 1: Verifying file integrity..." -Level "INFO" -LogFile $logFile
    
    $backupPath = Join-Path $Script:BackupLocations.SystemPath $metadata.BackupName
    
    if (-not (Test-Path $backupPath)) {
        throw "Backup path not found: $backupPath"
    }
    
    try {
        $currentChecksums = Generate-BackupChecksum -BackupPath $backupPath -Metadata $metadata
        
        $checksumMatch = $true
        foreach ($file in $metadata.Checksums.GetEnumerator()) {
            if ($currentChecksums[$file.Key].Hash -ne $file.Value.Hash) {
                $checksumMatch = $false
                Write-BackupLog -Message "Checksum mismatch: $($file.Key)" -Level "WARN" -LogFile $logFile
            }
        }
        
        $verificationReport.Results += @{
            Check = "File Integrity"
            Status = if ($checksumMatch) { "Passed" } else { "Failed" }
            Details = "$(($currentChecksums.Count)) files verified"
        }
        
        Write-BackupLog -Message "File integrity check: $(if($checksumMatch){'PASSED'}else{'FAILED'})" -Level $(if($checksumMatch){'SUCCESS'}else{'WARN'}) -LogFile $logFile
    }
    catch {
        $verificationReport.Results += @{
            Check = "File Integrity"
            Status = "Error"
            Details = $_
        }
        Write-BackupLog -Message "File integrity check error: $_" -Level "ERROR" -LogFile $logFile
    }
    
    #################################################################################
    # Phase 2: Database Backup Verification
    #################################################################################
    
    Write-BackupLog -Message "Phase 2: Verifying database backups..." -Level "INFO" -LogFile $logFile
    
    $dbBackupPath = Join-Path $backupPath "Databases"
    if (Test-Path $dbBackupPath) {
        $dbFiles = Get-ChildItem -Path $dbBackupPath -Filter "*.bak", "*.dif", "*.trn"
        
        foreach ($dbFile in $dbFiles) {
            try {
                $dbName = $dbFile.BaseName
                $dbServer = $Script:Databases[0].Server
                
                Write-BackupLog -Message "Verifying database backup: $dbName" -LogFile $logFile
                
                $verifyQuery = "RESTORE VERIFYONLY FROM DISK = N'$($dbFile.FullName)';"
                Invoke-Sqlcmd -ServerInstance $dbServer -Query $verifyQuery -QueryTimeout 3600
                
                $verificationReport.Results += @{
                    Check = "Database Backup"
                    Database = $dbName
                    Status = "Passed"
                    Details = "Backup is valid and can be restored"
                }
                
                Write-BackupLog -Message "Database backup verified: $dbName" -Level "SUCCESS" -LogFile $logFile
            }
            catch {
                $verificationReport.Results += @{
                    Check = "Database Backup"
                    Database = $dbName
                    Status = "Failed"
                    Details = $_
                }
                Write-BackupLog -Message "Database backup verification failed for $dbName`: $_" -Level "ERROR" -LogFile $logFile
                $verificationReport.OverallStatus = "Failed"
            }
        }
    }
    
    #################################################################################
    # Phase 3: Archive Integrity
    #################################################################################
    
    Write-BackupLog -Message "Phase 3: Verifying archive integrity..." -Level "INFO" -LogFile $logFile
    
    if ($backupPath -like "*.7z") {
        try {
            $7zipPath = "C:\Program Files\7-Zip\7z.exe"
            if (Test-Path $7zipPath) {
                $testOutput = & $7zipPath t "$backupPath" 2>&1
                $archiveValid = $LASTEXITCODE -eq 0
                
                $verificationReport.Results += @{
                    Check = "Archive Integrity"
                    Status = if ($archiveValid) { "Passed" } else { "Failed" }
                    Details = "7-Zip verification"
                }
                
                Write-BackupLog -Message "Archive integrity: $(if($archiveValid){'PASSED'}else{'FAILED'})" -Level $(if($archiveValid){'SUCCESS'}else{'ERROR'}) -LogFile $logFile
            }
        }
        catch {
            Write-BackupLog -Message "Archive integrity check error: $_" -Level "WARN" -LogFile $logFile
        }
    }
    
    #################################################################################
    # Phase 4: Test Restore (Optional)
    #################################################################################
    
    if ($TestRestore) {
        Write-BackupLog -Message "Phase 4: Performing test restore..." -Level "INFO" -LogFile $logFile
        
        try {
            $testRestorePath = Join-Path $Script:BackupLocations.SystemPath "test_restore_$([guid]::NewGuid().ToString().Substring(0,8))"
            New-Item -ItemType Directory -Path $testRestorePath -Force | Out-Null
            
            # Extract backup for testing
            if ($backupPath -like "*.7z") {
                $7zipPath = "C:\Program Files\7-Zip\7z.exe"
                & $7zipPath x "$backupPath" -o"$testRestorePath" | Out-Null
            }
            else {
                Copy-Item -Path "$backupPath\*" -Destination $testRestorePath -Recurse -Force
            }
            
            # Verify extracted content
            $extractedFiles = Get-ChildItem -Path $testRestorePath -Recurse
            $fileCount = $extractedFiles.Count
            
            $verificationReport.Results += @{
                Check = "Test Restore"
                Status = if ($fileCount -gt 0) { "Passed" } else { "Failed" }
                Details = "Extracted $fileCount files"
            }
            
            Write-BackupLog -Message "Test restore completed. Extracted $fileCount files" -Level "SUCCESS" -LogFile $logFile
            
            # Cleanup
            Remove-Item -Path $testRestorePath -Recurse -Force
        }
        catch {
            $verificationReport.Results += @{
                Check = "Test Restore"
                Status = "Failed"
                Details = $_
            }
            Write-BackupLog -Message "Test restore failed: $_" -Level "ERROR" -LogFile $logFile
            $verificationReport.OverallStatus = "Failed"
        }
    }
    
    #################################################################################
    # Final Report
    #################################################################################
    
    $verificationReport.EndTime = Get-Date
    $verificationReport.Duration = ($verificationReport.EndTime - $verificationReport.StartTime).TotalSeconds
    
    # Update metadata
    $metadata.VerificationStatus = $verificationReport.OverallStatus
    $metadata.LastVerified = Get-Date -Format "o"
    Save-BackupMetadata -Metadata $metadata
    
    Write-BackupLog -Message "Backup Verification Completed. Overall Status: $($verificationReport.OverallStatus)" -Level $(if($verificationReport.OverallStatus -eq 'Passed'){'SUCCESS'}else{'WARN'}) -LogFile $logFile
    
    return $verificationReport
}
catch {
    Write-BackupLog -Message "Backup Verification FAILED: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
