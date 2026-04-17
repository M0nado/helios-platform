<#
.SYNOPSIS
Restores build state from a snapshot.

.DESCRIPTION
Restores a previously saved build snapshot back to the BUILD_MANIFEST.json. Allows selection
of snapshots by ID or index, validates the snapshot before restoration, and provides rollback
capability if restoration fails.

.PARAMETER SnapshotId
The snapshot ID to restore.

.PARAMETER SnapshotIndex
The index of the snapshot to restore (from list-snapshots.ps1).

.PARAMETER WhatIf
Preview the changes without applying them.

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\restore-snapshot.ps1 -SnapshotId "snapshot-20240101-120000"
# Restore specific snapshot by ID

.EXAMPLE
.\restore-snapshot.ps1 -SnapshotIndex 1
# Restore most recent snapshot (index 1)

.EXAMPLE
.\restore-snapshot.ps1 -SnapshotId "pre-deployment" -WhatIf
# Preview restoration without applying changes

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$SnapshotId,
    [int]$SnapshotIndex = 0,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$snapshotsDir = Join-Path $scriptRoot "snapshots"
$backupDir = Join-Path $scriptRoot "backups"
$logPath = Join-Path $scriptRoot "logs\snapshots.log"

# Create directories if needed
@($backupDir, (Split-Path -Parent $logPath)) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -Path $_ -ItemType Directory -Force | Out-Null
    }
}

<#
.SYNOPSIS
Logs a message to file and console.
#>
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Warning', 'Error', 'Success')]
        [string]$Level = 'Info'
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    Add-Content -Path $logPath -Value $logMessage
    
    switch ($Level) {
        'Info' { Write-Host $logMessage -ForegroundColor Gray }
        'Warning' { Write-Host $logMessage -ForegroundColor Yellow }
        'Error' { Write-Host $logMessage -ForegroundColor Red }
        'Success' { Write-Host $logMessage -ForegroundColor Green }
    }
}

<#
.SYNOPSIS
Gets all available snapshots sorted by date (most recent first).
#>
function Get-AllSnapshots {
    if (-not (Test-Path $snapshotsDir)) {
        return @()
    }
    
    try {
        $snapshots = @()
        $files = Get-ChildItem -Path $snapshotsDir -Filter "*.json" -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            try {
                $snapshot = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
                $snapshots += $snapshot
            }
            catch {
                Write-Log -Message "Failed to load snapshot $($file.Name): $_" -Level Warning
            }
        }
        
        return $snapshots | Sort-Object { $_.created } -Descending
    }
    catch {
        Write-Log -Message "Failed to read snapshots: $_" -Level Error
        return @()
    }
}

<#
.SYNOPSIS
Finds a snapshot by ID or returns from list by index.
#>
function Get-Snapshot {
    param(
        [string]$Id,
        [int]$Index
    )
    
    $snapshots = Get-AllSnapshots
    
    if ($snapshots.Count -eq 0) {
        Write-Log -Message "No snapshots available" -Level Error
        return $null
    }
    
    if ($Id) {
        $snapshot = $snapshots | Where-Object { $_.id -eq $Id }
        if ($snapshot) {
            return $snapshot
        }
        Write-Log -Message "Snapshot not found: $Id" -Level Error
        return $null
    }
    
    if ($Index -gt 0 -and $Index -le $snapshots.Count) {
        return $snapshots[$Index - 1]
    }
    
    Write-Log -Message "Invalid snapshot index: $Index" -Level Error
    return $null
}

<#
.SYNOPSIS
Creates a backup of current manifest before restoration.
#>
function Backup-CurrentManifest {
    try {
        if (Test-Path $manifestPath) {
            $backupFile = Join-Path $backupDir "manifest-pre-restore-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
            Copy-Item -Path $manifestPath -Destination $backupFile -Force
            Write-Verbose "Created backup: $backupFile"
            return $backupFile
        }
    }
    catch {
        Write-Log -Message "Failed to create backup: $_" -Level Error
    }
    
    return $null
}

<#
.SYNOPSIS
Validates snapshot before restoration.
#>
function Test-SnapshotValidity {
    param([object]$Snapshot)
    
    $issues = @()
    
    if (-not $Snapshot.id) {
        $issues += "Snapshot missing ID"
    }
    
    if (-not $Snapshot.created) {
        $issues += "Snapshot missing created timestamp"
    }
    
    if (-not $Snapshot.manifestSnapshot) {
        $issues += "Snapshot missing manifest data"
    }
    
    if ($issues.Count -gt 0) {
        return @{
            Valid = $false
            Issues = $issues
        }
    }
    
    return @{
        Valid = $true
        Issues = @()
    }
}

<#
.SYNOPSIS
Displays snapshot information for confirmation.
#>
function Show-SnapshotInfo {
    param([object]$Snapshot)
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Snapshot Information" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Snapshot ID: $($Snapshot.id)" -ForegroundColor Yellow
    
    if ($Snapshot.name) {
        Write-Host "Name: $($Snapshot.name)" -ForegroundColor Yellow
    }
    
    Write-Host "Created: $($Snapshot.created)" -ForegroundColor Gray
    Write-Host "Variant: $($Snapshot.variant)" -ForegroundColor Cyan
    Write-Host "Components: $($Snapshot.enabledComponentCount) enabled / $($Snapshot.componentCount) total" -ForegroundColor Cyan
    
    if ($Snapshot.description) {
        Write-Host "Description: $($Snapshot.description)" -ForegroundColor Gray
    }
    
    Write-Host ""
}

<#
.SYNOPSIS
Restores manifest from snapshot.
#>
function Restore-ManifestFromSnapshot {
    param(
        [object]$Snapshot,
        [string]$BackupFile
    )
    
    try {
        # Prepare restored manifest
        $restoredManifest = $Snapshot.manifestSnapshot
        $restoredManifest.restoredFrom = $Snapshot.id
        $restoredManifest.restoredAt = (Get-Date -Format "o")
        
        # Save to manifest
        $restoredManifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8
        
        Write-Log -Message "Manifest restored from snapshot: $($Snapshot.id)" -Level Success
        return $true
    }
    catch {
        Write-Log -Message "Failed to restore manifest: $_" -Level Error
        
        # Attempt rollback
        if ($BackupFile -and (Test-Path $BackupFile)) {
            try {
                Copy-Item -Path $BackupFile -Destination $manifestPath -Force
                Write-Log -Message "Rolled back to pre-restore state" -Level Info
            }
            catch {
                Write-Log -Message "Rollback failed: $_" -Level Error
            }
        }
        
        return $false
    }
}

<#
.SYNOPSIS
Shows interactive snapshot selection if needed.
#>
function Select-Snapshot {
    $snapshots = Get-AllSnapshots
    
    if ($snapshots.Count -eq 0) {
        Write-Host ""
        Write-Host "No snapshots available. Create one using create-snapshot.ps1" -ForegroundColor Yellow
        Write-Host ""
        return $null
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Available Snapshots" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    $index = 1
    foreach ($snapshot in $snapshots) {
        $created = $snapshot.created
        if ($snapshot.created) {
            try {
                $date = [DateTime]::Parse($snapshot.created)
                $created = $date.ToString("yyyy-MM-dd HH:mm:ss")
            }
            catch { }
        }
        
        $nameStr = if ($snapshot.name) { " [$($snapshot.name)]" } else { "" }
        Write-Host "[$index] $($snapshot.id)$nameStr" -ForegroundColor Yellow
        Write-Host "    Created: $created | Variant: $($snapshot.variant) | Components: $($snapshot.enabledComponentCount)/$($snapshot.componentCount)" -ForegroundColor Gray
        
        $index++
    }
    
    Write-Host ""
    do {
        $selection = Read-Host "Enter snapshot number to restore (1-$($snapshots.Count)) or 0 to cancel"
        
        if ($selection -eq '0') {
            return $null
        }
        
        if ([int]$selection -ge 1 -and [int]$selection -le $snapshots.Count) {
            return $snapshots[[int]$selection - 1]
        }
        
        Write-Host "Invalid selection. Please try again." -ForegroundColor Red
    } while ($true)
}

<#
.SYNOPSIS
Main function to restore from snapshot.
#>
function Invoke-RestoreSnapshot {
    Write-Log -Message "Restore snapshot operation started" -Level Info
    
    # Get snapshot
    $snapshot = $null
    
    if ($SnapshotId) {
        $snapshot = Get-Snapshot -Id $SnapshotId
    }
    elseif ($SnapshotIndex -gt 0) {
        $snapshot = Get-Snapshot -Index $SnapshotIndex
    }
    else {
        $snapshot = Select-Snapshot
    }
    
    if (-not $snapshot) {
        Write-Log -Message "Restore cancelled - no snapshot selected" -Level Info
        return $false
    }
    
    # Validate snapshot
    $validation = Test-SnapshotValidity -Snapshot $snapshot
    if (-not $validation.Valid) {
        Write-Log -Message "Snapshot validation failed: $($validation.Issues -join '; ')" -Level Error
        Write-Host "Snapshot validation failed:" -ForegroundColor Red
        foreach ($issue in $validation.Issues) {
            Write-Host "  - $issue" -ForegroundColor Red
        }
        return $false
    }
    
    # Show snapshot info
    Show-SnapshotInfo -Snapshot $snapshot
    
    # Get confirmation
    $response = Read-Host "Proceed with restoration? (yes/no)"
    if ($response -ne 'yes' -and $response -ne 'y') {
        Write-Log -Message "Restore cancelled by user" -Level Info
        return $false
    }
    
    # Check WhatIf
    if ($PSCmdlet.ShouldProcess("Snapshot: $($snapshot.id)", "Restore")) {
        # Create backup before restoration
        $backupFile = Backup-CurrentManifest
        
        # Restore manifest
        if (-not (Restore-ManifestFromSnapshot -Snapshot $snapshot -BackupFile $backupFile)) {
            return $false
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Restoration Completed Successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        
        if ($backupFile) {
            Write-Host "Backup of pre-restore state: $backupFile" -ForegroundColor Gray
        }
        
        Write-Host ""
        return $true
    } else {
        Write-Host "WhatIf: Would restore snapshot '$($snapshot.id)'" -ForegroundColor Blue
        return $true
    }
}

# Main execution
try {
    $result = Invoke-RestoreSnapshot
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}
