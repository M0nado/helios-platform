#################################################################################
# HELIOS Platform - Full System Backup
# Purpose: Creates complete snapshots of all systems, configs, and data
# Version: 1.0.0
# Frequency: Daily (typically overnight)
#################################################################################

param(
    [string]$BackupName = "FullBackup_$(Get-Date -Format 'yyyyMMdd_HHmmss')",
    [bool]$VerifyAfterBackup = $true,
    [bool]$CompressBackup = $true,
    [bool]$UploadToCloud = $true
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "FullBackup"
Write-BackupLog -Message "Full System Backup Started: $BackupName" -Level "INFO" -LogFile $logFile

try {
    $null = Initialize-BackupDirectories
    
    $metadata = New-BackupMetadata -BackupName $BackupName -BackupType "Full" `
        -Description "Complete system snapshot including databases, configs, and file system"
    
    $backupRootPath = Join-Path $Script:BackupLocations.SystemPath $BackupName
    New-Item -ItemType Directory -Path $backupRootPath -Force | Out-Null
    
    Write-BackupLog -Message "Backup root path created: $backupRootPath" -LogFile $logFile
    
    #################################################################################
    # Phase 1: Database Backup
    #################################################################################
    
    Write-BackupLog -Message "Phase 1: Beginning database backups..." -Level "INFO" -LogFile $logFile
    
    $dbBackupPath = Join-Path $backupRootPath "Databases"
    New-Item -ItemType Directory -Path $dbBackupPath -Force | Out-Null
    
    foreach ($database in $Script:Databases) {
        try {
            $dbName = $database.Name
            $dbServer = $database.Server
            $backupFile = Join-Path $dbBackupPath "$dbName.bak"
            
            Write-BackupLog -Message "Backing up database: $dbName on $dbServer" -LogFile $logFile
            
            $backupQuery = @"
BACKUP DATABASE [$dbName] 
TO DISK = N'$backupFile'
WITH NOFORMAT, NOINIT, NAME = N'$dbName-FullBackup', SKIP, REWIND, NOUNLOAD, STATS = 10;
"@
            
            Invoke-Sqlcmd -ServerInstance $dbServer -Query $backupQuery -QueryTimeout 3600
            
            $backupSize = (Get-Item $backupFile).Length
            Write-BackupLog -Message "Database backup completed: $dbName (Size: $(($backupSize/1MB).ToString('F2')) MB)" -Level "SUCCESS" -LogFile $logFile
        }
        catch {
            Write-BackupLog -Message "Failed to backup database $($database.Name): $_" -Level "ERROR" -LogFile $logFile
        }
    }
    
    #################################################################################
    # Phase 2: Configuration Backup
    #################################################################################
    
    Write-BackupLog -Message "Phase 2: Beginning configuration backup..." -Level "INFO" -LogFile $logFile
    
    $configBackupPath = Join-Path $backupRootPath "Configuration"
    New-Item -ItemType Directory -Path $configBackupPath -Force | Out-Null
    
    $configDirs = @(
        "C:\HELIOS\Config",
        "C:\HELIOS\Settings",
        "C:\HELIOS\SSL"
    )
    
    foreach ($configDir in $configDirs) {
        if (Test-Path $configDir) {
            $itemName = Split-Path -Leaf $configDir
            $destPath = Join-Path $configBackupPath $itemName
            
            Copy-Item -Path $configDir -Destination $destPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-BackupLog -Message "Configuration backed up: $configDir" -LogFile $logFile
        }
    }
    
    #################################################################################
    # Phase 3: File System Backup
    #################################################################################
    
    Write-BackupLog -Message "Phase 3: Beginning file system backup..." -Level "INFO" -LogFile $logFile
    
    $fsBackupPath = Join-Path $backupRootPath "FileSystem"
    New-Item -ItemType Directory -Path $fsBackupPath -Force | Out-Null
    
    $dirsToBackup = @(
        @{ Source = "D:\HELIOS\Data"; Exclude = @("*.tmp", "*.log") }
        @{ Source = "D:\HELIOS\Documents"; Exclude = @() }
    )
    
    foreach ($dir in $dirsToBackup) {
        if (Test-Path $dir.Source) {
            $dirName = Split-Path -Leaf $dir.Source
            $destPath = Join-Path $fsBackupPath $dirName
            
            Write-BackupLog -Message "Backing up directory: $($dir.Source)" -LogFile $logFile
            Copy-Item -Path $dir.Source -Destination $destPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    #################################################################################
    # Phase 4: Generate Checksums
    #################################################################################
    
    Write-BackupLog -Message "Phase 4: Generating backup checksums..." -Level "INFO" -LogFile $logFile
    
    $metadata.Status = "Generating Checksums"
    Generate-BackupChecksum -BackupPath $backupRootPath -Metadata $metadata
    
    $metadata.Status = "Completed"
    $metadata.CompletedAt = Get-Date -Format "o"
    $totalSize = (Get-ChildItem -Path $backupRootPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
    $metadata.Size = $totalSize
    
    Save-BackupMetadata -Metadata $metadata
    
    Write-BackupLog -Message "Full System Backup Completed Successfully!" -Level "SUCCESS" -LogFile $logFile
    Write-BackupLog -Message "Backup Statistics: Total Size: $(($metadata.Size/1GB).ToString('F2')) GB" -Level "SUCCESS" -LogFile $logFile
    
    return @{
        Success = $true
        BackupId = $metadata.BackupId
        BackupPath = $backupRootPath
        TotalSize = $metadata.Size
    }
}
catch {
    Write-BackupLog -Message "Full System Backup FAILED: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
