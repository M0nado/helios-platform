#################################################################################
# HELIOS Platform - Cloud Backup and Replication
# Purpose: Syncs backups to Azure/cloud storage with encryption
# Version: 1.0.0
# Frequency: After each backup completion
#################################################################################

param(
    [string]$BackupId,
    [bool]$DeleteLocalAfterUpload = $false,
    [bool]$EncryptBeforeUpload = $true
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "CloudBackup"
Write-BackupLog -Message "Cloud Backup Process Started for BackupId: $BackupId" -Level "INFO" -LogFile $logFile

try {
    # Get backup metadata
    $metadata = Get-BackupMetadata -BackupId $BackupId
    if (-not $metadata) {
        throw "Backup metadata not found for ID: $BackupId"
    }
    
    Write-BackupLog -Message "Backup found: $($metadata.BackupName)" -LogFile $logFile
    
    $backupPath = $metadata.BackupPath
    if (-not (Test-Path $backupPath)) {
        throw "Backup path not found: $backupPath"
    }
    
    #################################################################################
    # Phase 1: Compression for Cloud
    #################################################################################
    
    Write-BackupLog -Message "Phase 1: Preparing backup for cloud upload..." -Level "INFO" -LogFile $logFile
    
    $cloudStagingPath = Join-Path $Script:BackupLocations.CloudPath "$($metadata.BackupId).7z"
    
    if (-not (Test-Path $cloudStagingPath)) {
        Write-BackupLog -Message "Compressing backup for cloud..." -LogFile $logFile
        $compressedSize = Compress-BackupData -SourcePath $backupPath -DestinationPath $cloudStagingPath -CompressionLevel 9
        $metadata.CompressedSize = $compressedSize
    }
    
    #################################################################################
    # Phase 2: Encryption
    #################################################################################
    
    if ($EncryptBeforeUpload) {
        Write-BackupLog -Message "Phase 2: Encrypting backup..." -Level "INFO" -LogFile $logFile
        
        $encryptedPath = "$cloudStagingPath.gpg"
        
        try {
            # This requires GPG installed. If not available, skip encryption warning
            gpg --symmetric --cipher-algo AES256 --output $encryptedPath $cloudStagingPath
            Remove-Item -Path $cloudStagingPath -Force
            $uploadPath = $encryptedPath
            Write-BackupLog -Message "Backup encrypted successfully" -Level "SUCCESS" -LogFile $logFile
        }
        catch {
            Write-BackupLog -Message "GPG encryption not available. Uploading unencrypted: $_" -Level "WARN" -LogFile $logFile
            $uploadPath = $cloudStagingPath
        }
    }
    else {
        $uploadPath = $cloudStagingPath
    }
    
    #################################################################################
    # Phase 3: Azure Upload (if configured)
    #################################################################################
    
    Write-BackupLog -Message "Phase 3: Uploading to Azure..." -Level "INFO" -LogFile $logFile
    
    try {
        # Check if Azure module is available
        if (Get-Module -Name Az.Storage -ListAvailable) {
            $azureAccount = $env:AZURE_ACCOUNT
            $azureKey = $env:AZURE_KEY
            
            if ($azureAccount -and $azureKey) {
                Write-BackupLog -Message "Authenticating with Azure..." -LogFile $logFile
                
                $ctx = New-AzStorageContext -StorageAccountName $azureAccount -StorageAccountKey $azureKey
                
                $containerName = $Script:BackupConfig.AzureContainer
                $fileName = Split-Path -Leaf $uploadPath
                
                Write-BackupLog -Message "Uploading $fileName to Azure blob storage..." -LogFile $logFile
                
                Set-AzStorageBlobContent -File $uploadPath -Container $containerName -Blob $fileName -Context $ctx -Force | Out-Null
                
                Write-BackupLog -Message "Upload to Azure completed successfully" -Level "SUCCESS" -LogFile $logFile
                
                $metadata.CloudSyncStatus = "Completed"
                $metadata.CloudPath = "https://$azureAccount.blob.core.windows.net/$containerName/$fileName"
            }
            else {
                Write-BackupLog -Message "Azure credentials not configured" -Level "WARN" -LogFile $logFile
            }
        }
        else {
            Write-BackupLog -Message "Azure Storage module not available" -Level "WARN" -LogFile $logFile
        }
    }
    catch {
        Write-BackupLog -Message "Azure upload failed: $_" -Level "WARN" -LogFile $logFile
        $metadata.CloudSyncStatus = "Failed"
    }
    
    #################################################################################
    # Phase 4: Local Cleanup (Optional)
    #################################################################################
    
    if ($DeleteLocalAfterUpload -and $metadata.CloudSyncStatus -eq "Completed") {
        Write-BackupLog -Message "Phase 4: Removing local backup copy..." -Level "INFO" -LogFile $logFile
        
        try {
            Remove-Item -Path $backupPath -Recurse -Force
            Write-BackupLog -Message "Local backup removed after successful cloud sync" -Level "SUCCESS" -LogFile $logFile
        }
        catch {
            Write-BackupLog -Message "Failed to remove local backup: $_" -Level "WARN" -LogFile $logFile
        }
    }
    
    # Update metadata
    $metadata.LastCloudSync = Get-Date -Format "o"
    Save-BackupMetadata -Metadata $metadata
    
    Write-BackupLog -Message "Cloud Backup Process Completed Successfully!" -Level "SUCCESS" -LogFile $logFile
    
    return @{
        Success = $true
        BackupId = $BackupId
        CloudPath = $metadata.CloudPath
        Status = $metadata.CloudSyncStatus
    }
}
catch {
    Write-BackupLog -Message "Cloud Backup Process FAILED: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
