#################################################################################
# HELIOS Platform - Core Backup Infrastructure Functions
# Purpose: Provides foundational backup functions used by all backup strategies
# Version: 1.0.0
# Author: HELIOS DevOps
#################################################################################

# Import required modules
Import-Module SqlServer -ErrorAction SilentlyContinue
Import-Module Az.Storage -ErrorAction SilentlyContinue

# Global Configuration
$Script:BackupConfig = @{
    BackupBasePath = "D:\HELIOS\Backups"
    LogPath = "D:\HELIOS\Logs\Backup"
    RetentionDays = 30
    FullBackupRetentionDays = 90
    CompressionLevel = 9
    VerificationEnabled = $true
    CloudBackupEnabled = $true
    AzureStorageAccount = "heliosbackup"
    AzureContainer = "helios-backups"
    EmailAlerts = "devops@helios.local"
    MaxParallelTasks = 4
}

$Script:Databases = @(
    @{ Name = "HELIOS_Core"; Server = "localhost\SQLEXPRESS"; Type = "Primary" }
    @{ Name = "HELIOS_Users"; Server = "localhost\SQLEXPRESS"; Type = "Secondary" }
    @{ Name = "HELIOS_Analytics"; Server = "localhost\SQLEXPRESS"; Type = "Secondary" }
    @{ Name = "HELIOS_Config"; Server = "localhost\SQLEXPRESS"; Type = "Secondary" }
    @{ Name = "HELIOS_Audit"; Server = "localhost\SQLEXPRESS"; Type = "Audit" }
)

$Script:BackupLocations = @{
    DatabasePath = "D:\HELIOS\Backups\Databases"
    ConfigPath = "D:\HELIOS\Backups\Configuration"
    FileSystemPath = "D:\HELIOS\Backups\FileSystem"
    SystemPath = "D:\HELIOS\Backups\System"
    CloudPath = "D:\HELIOS\Backups\Cloud"
    ArchivePath = "D:\HELIOS\Backups\Archive"
}

#################################################################################
# Logging Functions
#################################################################################

function Initialize-BackupLogging {
    param(
        [string]$LogType = "General"
    )
    
    $logDir = Join-Path $Script:BackupConfig.LogPath $LogType
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd"
    $logFile = Join-Path $logDir "$LogType-$timestamp.log"
    return $logFile
}

function Write-BackupLog {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS", "DEBUG")]
        [string]$Level = "INFO",
        
        [string]$LogFile,
        
        [hashtable]$Context
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    if ($Context) {
        $contextStr = $Context.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" } | Join-String -Separator ", "
        $logMessage += " | Context: [$contextStr]"
    }
    
    if ($LogFile) {
        Add-Content -Path $LogFile -Value $logMessage -Encoding UTF8 -Force
    }
    
    $color = @{
        INFO = "Gray"
        WARN = "Yellow"
        ERROR = "Red"
        SUCCESS = "Green"
        DEBUG = "Cyan"
    }
    
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

#################################################################################
# Directory Initialization Functions
#################################################################################

function Initialize-BackupDirectories {
    
    $logFile = Initialize-BackupLogging -LogType "DirectorySetup"
    Write-BackupLog -Message "Initializing backup directory structure..." -LogFile $logFile
    
    try {
        foreach ($location in $Script:BackupLocations.GetEnumerator()) {
            if (-not (Test-Path $location.Value)) {
                New-Item -ItemType Directory -Path $location.Value -Force | Out-Null
                Write-BackupLog -Message "Created directory: $($location.Value)" -Level "INFO" -LogFile $logFile
            }
        }
        
        $metadataDir = Join-Path $Script:BackupConfig.BackupBasePath "Metadata"
        if (-not (Test-Path $metadataDir)) {
            New-Item -ItemType Directory -Path $metadataDir -Force | Out-Null
        }
        
        Write-BackupLog -Message "Backup directory initialization completed successfully" -Level "SUCCESS" -LogFile $logFile
        return $true
    }
    catch {
        Write-BackupLog -Message "Failed to initialize backup directories: $_" -Level "ERROR" -LogFile $logFile
        return $false
    }
}

#################################################################################
# Backup Metadata Functions
#################################################################################

function New-BackupMetadata {
    param(
        [Parameter(Mandatory=$true)]
        [string]$BackupName,
        
        [ValidateSet("Full", "Incremental", "Differential")]
        [string]$BackupType = "Full",
        
        [string]$Description,
        
        [string[]]$Sources
    )
    
    $metadata = @{
        BackupId = [guid]::NewGuid().ToString()
        BackupName = $BackupName
        BackupType = $BackupType
        Description = $Description
        CreatedAt = Get-Date -Format "o"
        Sources = $Sources
        Status = "In Progress"
        Checksums = @{}
        Size = 0
        CompressedSize = 0
        Files = @()
        VerificationStatus = "Pending"
        CloudSyncStatus = "Pending"
    }
    
    return $metadata
}

function Save-BackupMetadata {
    param(
        [Parameter(Mandatory=$true)]
        [hashtable]$Metadata
    )
    
    $metadataDir = Join-Path $Script:BackupConfig.BackupBasePath "Metadata"
    $metadataFile = Join-Path $metadataDir "$($Metadata.BackupId).json"
    
    $Metadata | ConvertTo-Json -Depth 10 | Set-Content -Path $metadataFile -Encoding UTF8
    return $metadataFile
}

function Get-BackupMetadata {
    param(
        [string]$BackupId
    )
    
    $metadataDir = Join-Path $Script:BackupConfig.BackupBasePath "Metadata"
    $metadataFile = Join-Path $metadataDir "$BackupId.json"
    
    if (Test-Path $metadataFile) {
        return Get-Content -Path $metadataFile -Raw | ConvertFrom-Json -AsHashtable
    }
    
    return $null
}

#################################################################################
# Compression Functions
#################################################################################

function Compress-BackupData {
    param(
        [Parameter(Mandatory=$true)]
        [string]$SourcePath,
        
        [Parameter(Mandatory=$true)]
        [string]$DestinationPath,
        
        [int]$CompressionLevel = 9
    )
    
    $logFile = Initialize-BackupLogging -LogType "Compression"
    
    try {
        $7zipPath = "C:\Program Files\7-Zip\7z.exe"
        
        if (Test-Path $7zipPath) {
            Write-BackupLog -Message "Compressing with 7-Zip: $SourcePath -> $DestinationPath" -LogFile $logFile
            & $7zipPath a -t7z -mx=$CompressionLevel -mmt=on "$DestinationPath" "$SourcePath" | Out-Null
        }
        else {
            Write-BackupLog -Message "Using Windows native compression: $SourcePath -> $DestinationPath" -LogFile $logFile
            Compress-Archive -Path $SourcePath -DestinationPath $DestinationPath -CompressionLevel Optimal -Force
        }
        
        $compressedSize = (Get-Item $DestinationPath).Length
        Write-BackupLog -Message "Compression completed. Size: $(($compressedSize / 1MB).ToString('F2')) MB" -Level "SUCCESS" -LogFile $logFile
        
        return $compressedSize
    }
    catch {
        Write-BackupLog -Message "Compression failed: $_" -Level "ERROR" -LogFile $logFile
        throw
    }
}

#################################################################################
# Checksum and Verification Functions
#################################################################################

function Calculate-FileChecksum {
    param(
        [Parameter(Mandatory=$true)]
        [string]$FilePath,
        
        [ValidateSet("SHA256", "SHA512", "MD5")]
        [string]$Algorithm = "SHA256"
    )
    
    $hash = Get-FileHash -Path $FilePath -Algorithm $Algorithm
    return $hash.Hash
}

function Generate-BackupChecksum {
    param(
        [Parameter(Mandatory=$true)]
        [string]$BackupPath,
        
        [hashtable]$Metadata
    )
    
    $logFile = Initialize-BackupLogging -LogType "Verification"
    
    try {
        Write-BackupLog -Message "Generating backup checksums for: $BackupPath" -LogFile $logFile
        
        $checksums = @{}
        $files = Get-ChildItem -Path $BackupPath -Recurse -File
        
        foreach ($file in $files) {
            $relPath = $file.FullName.Substring($BackupPath.Length).TrimStart('\')
            $hash = Calculate-FileChecksum -FilePath $file.FullName
            $checksums[$relPath] = @{
                Hash = $hash
                Size = $file.Length
                Modified = $file.LastWriteTime
            }
        }
        
        $Metadata.Checksums = $checksums
        $Metadata.Files = $files.FullName
        
        Write-BackupLog -Message "Generated checksums for $($checksums.Count) files" -Level "SUCCESS" -LogFile $logFile
        
        return $checksums
    }
    catch {
        Write-BackupLog -Message "Checksum generation failed: $_" -Level "ERROR" -LogFile $logFile
        throw
    }
}

#################################################################################
# Cleanup and Retention Functions
#################################################################################

function Remove-ExpiredBackups {
    param(
        [string]$BackupPath,
        [int]$RetentionDays,
        [string]$LogFile
    )
    
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $expiredBackups = Get-ChildItem -Path $BackupPath -Directory | Where-Object { $_.CreationTime -lt $cutoffDate }
    
    foreach ($backup in $expiredBackups) {
        try {
            Remove-Item -Path $backup.FullName -Recurse -Force
            Write-BackupLog -Message "Removed expired backup: $($backup.Name)" -Level "INFO" -LogFile $LogFile
        }
        catch {
            Write-BackupLog -Message "Failed to remove backup $($backup.Name): $_" -Level "WARN" -LogFile $LogFile
        }
    }
    
    return $expiredBackups.Count
}

function Cleanup-BackupDirectories {
    
    $logFile = Initialize-BackupLogging -LogType "Cleanup"
    Write-BackupLog -Message "Starting backup cleanup process..." -LogFile $logFile
    
    try {
        $fullBackupCount = Remove-ExpiredBackups -BackupPath $Script:BackupLocations.SystemPath `
            -RetentionDays $Script:BackupConfig.FullBackupRetentionDays -LogFile $logFile
        
        $metadataDir = Join-Path $Script:BackupConfig.BackupBasePath "Metadata"
        $cutoffDate = (Get-Date).AddDays(-$Script:BackupConfig.RetentionDays)
        $oldMetadata = Get-ChildItem -Path $metadataDir -Filter "*.json" | Where-Object { $_.CreationTime -lt $cutoffDate }
        
        foreach ($file in $oldMetadata) {
            Remove-Item -Path $file.FullName -Force
        }
        
        Write-BackupLog -Message "Cleanup completed. Removed $fullBackupCount full backups" -Level "SUCCESS" -LogFile $logFile
        
        return @{
            FullBackupsRemoved = $fullBackupCount
            MetadataFilesRemoved = $oldMetadata.Count
        }
    }
    catch {
        Write-BackupLog -Message "Cleanup process failed: $_" -Level "ERROR" -LogFile $logFile
        throw
    }
}

#################################################################################
# Health and Status Functions
#################################################################################

function Get-BackupStorageStatus {
    
    try {
        $backupDrive = (Get-Item $Script:BackupConfig.BackupBasePath).Root.Name
        $driveLetter = $backupDrive[0]
        $driveInfo = Get-Volume -DriveLetter $driveLetter
        
        $backupSize = Get-ChildItem -Path $Script:BackupConfig.BackupBasePath -Recurse -File | Measure-Object -Property Length -Sum
        
        return @{
            TotalDriveSize = $driveInfo.Size
            UsedDriveSpace = $driveInfo.Size - $driveInfo.SizeRemaining
            AvailableDriveSpace = $driveInfo.SizeRemaining
            BackupTotalSize = $backupSize.Sum
            DiskUsagePercent = [math]::Round(($backupSize.Sum / $driveInfo.Size) * 100, 2)
            WarningThreshold = 80
            CriticalThreshold = 95
        }
    }
    catch {
        return @{ Error = $_ }
    }
}

function Test-BackupHealth {
    
    $logFile = Initialize-BackupLogging -LogType "HealthCheck"
    $healthReport = @{
        Status = "Healthy"
        Checks = @()
        Timestamp = Get-Date
    }
    
    try {
        $dirCheck = $true
        foreach ($location in $Script:BackupLocations.GetEnumerator()) {
            if (-not (Test-Path $location.Value)) {
                $dirCheck = $false
                Write-BackupLog -Message "Missing backup directory: $($location.Value)" -Level "WARN" -LogFile $logFile
            }
        }
        $healthReport.Checks += @{ CheckName = "DirectoryStructure"; Status = if ($dirCheck) { "OK" } else { "FAILED" } }
        
        $storageStatus = Get-BackupStorageStatus
        $storageCheck = $storageStatus.DiskUsagePercent -lt $storageStatus.WarningThreshold
        $healthReport.Checks += @{ CheckName = "StorageSpace"; Status = if ($storageCheck) { "OK" } else { "WARNING" }; Details = $storageStatus }
        
        $latestFull = Get-ChildItem -Path $Script:BackupLocations.SystemPath -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
        $hoursSinceLastFull = if ($latestFull) { ((Get-Date) - $latestFull.CreationTime).TotalHours } else { 999 }
        $fullBackupCheck = $hoursSinceLastFull -lt 24
        $healthReport.Checks += @{ CheckName = "LastFullBackup"; Status = if ($fullBackupCheck) { "OK" } else { "WARNING" }; HoursSinceLast = $hoursSinceLastFull }
        
        $failedChecks = @($healthReport.Checks | Where-Object { $_.Status -eq "FAILED" })
        $warningChecks = @($healthReport.Checks | Where-Object { $_.Status -eq "WARNING" })
        
        if ($failedChecks.Count -gt 0) {
            $healthReport.Status = "Unhealthy"
        }
        elseif ($warningChecks.Count -gt 0) {
            $healthReport.Status = "Degraded"
        }
        
        Write-BackupLog -Message "Health check completed. Status: $($healthReport.Status)" -Level "INFO" -LogFile $logFile
    }
    catch {
        Write-BackupLog -Message "Health check failed: $_" -Level "ERROR" -LogFile $logFile
        $healthReport.Status = "Error"
    }
    
    return $healthReport
}

Export-ModuleMember -Function @(
    'Initialize-BackupLogging',
    'Write-BackupLog',
    'Initialize-BackupDirectories',
    'New-BackupMetadata',
    'Save-BackupMetadata',
    'Get-BackupMetadata',
    'Compress-BackupData',
    'Calculate-FileChecksum',
    'Generate-BackupChecksum',
    'Remove-ExpiredBackups',
    'Cleanup-BackupDirectories',
    'Get-BackupStorageStatus',
    'Test-BackupHealth'
) -Variable @('BackupConfig', 'Databases', 'BackupLocations')

Write-Host "Core Backup Infrastructure Module Loaded Successfully" -ForegroundColor Green
