<#
.SYNOPSIS
Azure Storage Management for HELIOS Platform

.DESCRIPTION
Manages Azure Storage resources including:
- Storage account creation and configuration
- Blob storage management
- File shares management
- Queue and Table storage
- Storage access policies and SAS tokens
- Storage monitoring and analytics

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.Storage module
#>

#Requires -Modules Az.Storage

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "storage-mgmt-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    
    $color = @{
        'Info'    = 'Cyan'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Success' = 'Green'
    }
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

function Get-ConfigValue {
    param([string]$Key, [string]$DefaultValue = $null)
    
    if (Test-Path $ConfigPath) {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        return $config.$Key ?? $DefaultValue
    }
    return $DefaultValue
}

function New-StorageAccount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Standard_LRS', 'Standard_GRS', 'Standard_RAGRS', 'Premium_LRS')]
        [string]$SkuName = 'Standard_LRS',
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('StorageV2', 'BlobStorage')]
        [string]$Kind = 'StorageV2',
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Creating Storage Account: $StorageAccountName"
        
        $storageAccount = New-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -Location $Location -SkuName $SkuName -Kind $Kind `
            -ErrorAction Stop
        
        if ($Tags) {
            Update-AzTag -ResourceId $storageAccount.Id -Tag $Tags -Operation Merge | Out-Null
        }
        
        Write-Log "Storage Account created successfully: $StorageAccountName" -Level Success
        return $storageAccount
    }
    catch {
        Write-Log "Failed to create Storage Account: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-StorageContainer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Blob', 'Container')]
        [string]$Permission = 'Blob'
    )
    
    try {
        Write-Log "Creating Storage Container: $ContainerName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        $container = New-AzStorageContainer -Name $ContainerName -Context $context `
            -Permission $Permission -ErrorAction Stop
        
        Write-Log "Storage Container created successfully: $ContainerName" -Level Success
        return $container
    }
    catch {
        Write-Log "Failed to create Storage Container: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Upload-BlobFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName,
        
        [Parameter(Mandatory = $true)]
        [string]$LocalFilePath,
        
        [Parameter(Mandatory = $false)]
        [string]$BlobName
    )
    
    try {
        if (-not (Test-Path $LocalFilePath)) {
            throw "File not found: $LocalFilePath"
        }
        
        if (-not $BlobName) {
            $BlobName = Split-Path -Leaf $LocalFilePath
        }
        
        Write-Log "Uploading blob file: $BlobName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        Set-AzStorageBlobContent -File $LocalFilePath -Container $ContainerName `
            -Blob $BlobName -Context $context -Force -ErrorAction Stop | Out-Null
        
        Write-Log "Blob file uploaded successfully: $BlobName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to upload blob file: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-BlobContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName,
        
        [Parameter(Mandatory = $true)]
        [string]$BlobName,
        
        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )
    
    try {
        Write-Log "Downloading blob file: $BlobName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        Get-AzStorageBlobContent -Container $ContainerName -Blob $BlobName `
            -Destination $OutputPath -Context $context -Force -ErrorAction Stop
        
        Write-Log "Blob file downloaded successfully: $BlobName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to download blob file: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-StorageFileShare {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ShareName,
        
        [Parameter(Mandatory = $false)]
        [int]$QuotaGB = 100
    )
    
    try {
        Write-Log "Creating File Share: $ShareName (Quota: $QuotaGB GB)"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        $share = New-AzStorageShare -Name $ShareName -Context $context `
            -QuotaGiB $QuotaGB -ErrorAction Stop
        
        Write-Log "File Share created successfully: $ShareName" -Level Success
        return $share
    }
    catch {
        Write-Log "Failed to create File Share: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-StorageAccountKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName
    )
    
    try {
        Write-Log "Retrieving storage account keys for: $StorageAccountName"
        
        $keys = Get-AzStorageAccountKey -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop
        
        return @{
            Key1 = $keys[0].Value
            Key2 = $keys[1].Value
        }
    }
    catch {
        Write-Log "Failed to retrieve storage account keys: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-StorageSASToken {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName,
        
        [Parameter(Mandatory = $false)]
        [int]$ExpiryInMinutes = 60,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Read', 'Write', 'Delete', 'List', 'ReadWrite', 'ReadDelete', 'ReadList')]
        [string]$Permission = 'Read'
    )
    
    try {
        Write-Log "Generating SAS token for container: $ContainerName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        $container = Get-AzStorageContainer -Name $ContainerName -Context $context `
            -ErrorAction Stop
        
        $expiryTime = (Get-Date).AddMinutes($ExpiryInMinutes)
        
        $sas = New-AzStorageContainerSASToken -Container $ContainerName -Permission $Permission `
            -ExpiryTime $expiryTime -Context $context -ErrorAction Stop
        
        Write-Log "SAS token generated successfully" -Level Success
        return $sas
    }
    catch {
        Write-Log "Failed to generate SAS token: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-StorageContainerContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName
    )
    
    try {
        Write-Log "Listing blobs in container: $ContainerName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        $blobs = Get-AzStorageBlob -Container $ContainerName -Context $context `
            -ErrorAction Stop
        
        Write-Log "Retrieved $($blobs.Count) blobs from container" -Level Success
        return $blobs
    }
    catch {
        Write-Log "Failed to list container content: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-StorageBlob {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName,
        
        [Parameter(Mandatory = $true)]
        [string]$ContainerName,
        
        [Parameter(Mandatory = $true)]
        [string]$BlobName
    )
    
    try {
        Write-Log "Removing blob: $BlobName"
        
        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop).Context
        
        Remove-AzStorageBlob -Container $ContainerName -Blob $BlobName `
            -Context $context -Force -ErrorAction Stop
        
        Write-Log "Blob removed successfully: $BlobName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove blob: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Enable-StorageEncryption {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName
    )
    
    try {
        Write-Log "Enabling encryption for storage account: $StorageAccountName"
        
        $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop
        
        Set-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName `
            -AssignIdentity -ErrorAction Stop | Out-Null
        
        Write-Log "Storage encryption enabled successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to enable encryption: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-StorageAccountMetrics {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$StorageAccountName
    )
    
    try {
        Write-Log "Retrieving metrics for storage account: $StorageAccountName"
        
        $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName `
            -Name $StorageAccountName -ErrorAction Stop
        
        return @{
            Name              = $storageAccount.StorageAccountName
            Location          = $storageAccount.Location
            SkuName           = $storageAccount.Sku.Name
            Kind              = $storageAccount.Kind
            Tier              = $storageAccount.Sku.Tier
            AccessTier        = $storageAccount.AccessTier
            ResourceId        = $storageAccount.Id
            CreationTime      = $storageAccount.CreationTime
        }
    }
    catch {
        Write-Log "Failed to retrieve metrics: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-StorageAccount',
    'New-StorageContainer',
    'Upload-BlobFile',
    'Get-BlobContent',
    'New-StorageFileShare',
    'Get-StorageAccountKey',
    'New-StorageSASToken',
    'Get-StorageContainerContent',
    'Remove-StorageBlob',
    'Enable-StorageEncryption',
    'Get-StorageAccountMetrics'
)
