<#
.SYNOPSIS
Azure Backup and Recovery Management for HELIOS Platform

.DESCRIPTION
Manages Azure Backup and recovery including:
- Recovery Services vault setup
- Backup policies configuration
- VM backup management
- File recovery
- Disaster recovery configuration
- Backup monitoring and reporting

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.RecoveryServices module
#>

#Requires -Modules Az.RecoveryServices, Az.Backup

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "backup-recovery-$(Get-Date -Format 'yyyyMMdd').log"

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

function New-RecoveryServicesVault {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Creating Recovery Services Vault: $VaultName"
        
        $vault = New-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -Location $Location -ErrorAction Stop
        
        if ($Tags) {
            Update-AzTag -ResourceId $vault.Id -Tag $Tags -Operation Merge | Out-Null
        }
        
        Set-AzRecoveryServicesBackupProperties -Vault $vault -BackupStorageRedundancy GeoRedundant `
            -ErrorAction Stop | Out-Null
        
        Write-Log "Recovery Services Vault created successfully: $VaultName" -Level Success
        return $vault
    }
    catch {
        Write-Log "Failed to create Recovery Services Vault: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Enable-VMBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VMName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName,
        
        [Parameter(Mandatory = $false)]
        [string]$PolicyName = 'DefaultPolicy'
    )
    
    try {
        Write-Log "Enabling backup for VM: $VMName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $vm = Get-AzVM -ResourceGroupName $ResourceGroupName -Name $VMName -ErrorAction Stop
        
        $policy = Get-AzRecoveryServicesBackupProtectionPolicy -Name $PolicyName `
            -ErrorAction Stop
        
        Enable-AzRecoveryServicesBackupProtection -ResourceGroupName $ResourceGroupName `
            -Name $VMName -Policy $policy -ErrorAction Stop
        
        Write-Log "Backup enabled successfully for VM: $VMName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to enable backup: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Trigger-VMBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VMName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName
    )
    
    try {
        Write-Log "Triggering backup for VM: $VMName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $container = Get-AzRecoveryServicesBackupContainer -ContainerType AzureVM `
            -FriendlyName $VMName -ErrorAction Stop
        
        $item = Get-AzRecoveryServicesBackupItem -Container $container -WorkloadType AzureVM `
            -ErrorAction Stop
        
        Backup-AzRecoveryServicesBackupItem -Item $item -ErrorAction Stop
        
        Write-Log "Backup triggered successfully for VM: $VMName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to trigger backup: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-BackupProtectionStatus {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName
    )
    
    try {
        Write-Log "Retrieving backup protection status"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $protectedItems = Get-AzRecoveryServicesBackupItem -BackupManagementType AzureVM `
            -WorkloadType AzureVM -ErrorAction Stop
        
        $status = @()
        foreach ($item in $protectedItems) {
            $status += @{
                Name              = $item.Name
                ProtectionStatus  = $item.ProtectionStatus
                LastBackupStatus  = $item.LastBackupStatus
                LastBackupTime    = $item.LastBackupTime
            }
        }
        
        Write-Log "Retrieved $($status.Count) protected items" -Level Success
        return $status
    }
    catch {
        Write-Log "Failed to retrieve protection status: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-RecoveryPoints {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VMName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName
    )
    
    try {
        Write-Log "Retrieving recovery points for VM: $VMName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $container = Get-AzRecoveryServicesBackupContainer -ContainerType AzureVM `
            -FriendlyName $VMName -ErrorAction Stop
        
        $item = Get-AzRecoveryServicesBackupItem -Container $container -WorkloadType AzureVM `
            -ErrorAction Stop
        
        $recoveryPoints = Get-AzRecoveryServicesBackupRecoveryPoint -Item $item `
            -ErrorAction Stop
        
        Write-Log "Retrieved $($recoveryPoints.Count) recovery points" -Level Success
        return $recoveryPoints
    }
    catch {
        Write-Log "Failed to retrieve recovery points: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Restore-VMFromBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VMName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName,
        
        [Parameter(Mandatory = $true)]
        [string]$NewVMName,
        
        [Parameter(Mandatory = $false)]
        [int]$RecoveryPointIndex = 0
    )
    
    try {
        Write-Log "Restoring VM from backup: $VMName -> $NewVMName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $container = Get-AzRecoveryServicesBackupContainer -ContainerType AzureVM `
            -FriendlyName $VMName -ErrorAction Stop
        
        $item = Get-AzRecoveryServicesBackupItem -Container $container -WorkloadType AzureVM `
            -ErrorAction Stop
        
        $recoveryPoints = Get-AzRecoveryServicesBackupRecoveryPoint -Item $item `
            -ErrorAction Stop
        
        if ($recoveryPoints.Count -le $RecoveryPointIndex) {
            throw "Recovery point index out of range"
        }
        
        $rp = $recoveryPoints[$RecoveryPointIndex]
        
        $restoreConfig = New-AzRecoveryServicesBackupRestoreConfig -SourceItemType AzureVM `
            -RecoveryPoint $rp -RestoreType CreateNewCloudVM -VaultId $vault.ID `
            -ErrorAction Stop
        
        $restoreConfig.VirtualMachineId = "/subscriptions/$(Get-AzContext).Subscription.Id/resourceGroups/$ResourceGroupName/providers/Microsoft.Compute/virtualMachines/$NewVMName"
        
        Restore-AzRecoveryServicesBackupItem -RestoreConfig $restoreConfig -WaitForCompletion `
            -VaultId $vault.ID -ErrorAction Stop
        
        Write-Log "VM restored successfully: $NewVMName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to restore VM: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Create-BackupPolicy {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VaultName,
        
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$PolicyName,
        
        [Parameter(Mandatory = $false)]
        [string]$SchedulePolicy = 'Daily',
        
        [Parameter(Mandatory = $false)]
        [int]$RetentionDays = 30
    )
    
    try {
        Write-Log "Creating backup policy: $PolicyName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $schPolicy = Get-AzRecoveryServicesBackupSchedulePolicyObject -WorkloadType AzureVM `
            -BackupManagementType AzureVM -PolicyType Standard -ErrorAction Stop
        
        $retPolicy = Get-AzRecoveryServicesBackupRetentionPolicyObject -WorkloadType AzureVM `
            -BackupManagementType AzureVM -RetentionDurationType Days -RetentionCount $RetentionDays `
            -ErrorAction Stop
        
        $policy = New-AzRecoveryServicesBackupProtectionPolicy -Name $PolicyName `
            -WorkloadType AzureVM -BackupManagementType AzureVM `
            -SchedulePolicy $schPolicy -RetentionPolicy $retPolicy `
            -ErrorAction Stop
        
        Write-Log "Backup policy created successfully: $PolicyName" -Level Success
        return $policy
    }
    catch {
        Write-Log "Failed to create backup policy: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Disable-VMBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VMName,
        
        [Parameter(Mandatory = $true)]
        [string]$VaultName,
        
        [Parameter(Mandatory = $false)]
        [switch]$RetainBackups
    )
    
    try {
        Write-Log "Disabling backup for VM: $VMName"
        
        $vault = Get-AzRecoveryServicesVault -Name $VaultName -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop
        
        Set-AzRecoveryServicesVaultContext -Vault $vault -ErrorAction Stop
        
        $container = Get-AzRecoveryServicesBackupContainer -ContainerType AzureVM `
            -FriendlyName $VMName -ErrorAction Stop
        
        $item = Get-AzRecoveryServicesBackupItem -Container $container -WorkloadType AzureVM `
            -ErrorAction Stop
        
        Disable-AzRecoveryServicesBackupProtection -Item $item -RemoveRecoveryPoints:(-not $RetainBackups) `
            -Force -ErrorAction Stop
        
        Write-Log "Backup disabled successfully for VM: $VMName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to disable backup: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-RecoveryServicesVault',
    'Enable-VMBackup',
    'Trigger-VMBackup',
    'Get-BackupProtectionStatus',
    'Get-RecoveryPoints',
    'Restore-VMFromBackup',
    'Create-BackupPolicy',
    'Disable-VMBackup'
)
