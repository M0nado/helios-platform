<#
.SYNOPSIS
Creates snapshots of the current build state.

.DESCRIPTION
Saves a complete snapshot of the current BUILD_MANIFEST.json and all build configuration
state to a timestamped backup that can be restored later. Snapshots include metadata about
when they were created and what variant they represent.

.PARAMETER Name
Custom name for the snapshot (optional).

.PARAMETER Description
Description for the snapshot (optional).

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\create-snapshot.ps1
# Creates a snapshot with default name (current timestamp)

.EXAMPLE
.\create-snapshot.ps1 -Name "pre-deployment" -Description "Build state before production deployment"
# Creates a named snapshot with description

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding()]
param(
    [string]$Name,
    [string]$Description,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$snapshotsDir = Join-Path $scriptRoot "snapshots"
$logPath = Join-Path $scriptRoot "logs\snapshots.log"

# Create directories if needed
@($snapshotsDir, (Split-Path -Parent $logPath)) | ForEach-Object {
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
Loads the BUILD_MANIFEST.json configuration.
#>
function Get-BuildManifest {
    if (-not (Test-Path $manifestPath)) {
        Write-Log -Message "Manifest not found at $manifestPath" -Level Error
        return $null
    }
    
    try {
        $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        Write-Verbose "Loaded manifest from $manifestPath"
        return $manifest
    }
    catch {
        Write-Log -Message "Failed to load manifest: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Creates a new snapshot of the current build state.

.OUTPUTS
The snapshot object or $null on failure.
#>
function New-Snapshot {
    param(
        [string]$SnapshotName,
        [string]$SnapshotDescription,
        [object]$Manifest
    )
    
    if (-not $Manifest) {
        Write-Log -Message "Cannot create snapshot without valid manifest" -Level Error
        return $null
    }
    
    try {
        $timestamp = Get-Date -Format "o"
        $snapshotId = if ($SnapshotName) { $SnapshotName } else { "snapshot-$(Get-Date -Format 'yyyyMMdd-HHmmss')" }
        
        $snapshot = @{
            id = $snapshotId
            name = $SnapshotName
            description = $SnapshotDescription
            created = $timestamp
            variant = $Manifest.selectedVariant
            variantName = $Manifest.variantName
            variantDescription = $Manifest.variantDescription
            componentCount = @($Manifest.components).Count
            enabledComponentCount = @($Manifest.components | Where-Object { $_.enabled }).Count
            manifestSnapshot = $Manifest
        }
        
        Write-Verbose "Created snapshot object: $snapshotId"
        return $snapshot
    }
    catch {
        Write-Log -Message "Failed to create snapshot: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Saves snapshot to disk.

.OUTPUTS
The snapshot file path or $null on failure.
#>
function Save-Snapshot {
    param([object]$Snapshot)
    
    if (-not $Snapshot) {
        return $null
    }
    
    try {
        $fileName = "$($Snapshot.id).json"
        $filePath = Join-Path $snapshotsDir $fileName
        
        $Snapshot | ConvertTo-Json -Depth 10 | Set-Content -Path $filePath -Encoding UTF8
        
        Write-Log -Message "Snapshot saved: $fileName" -Level Success
        Write-Verbose "Snapshot file path: $filePath"
        
        return $filePath
    }
    catch {
        Write-Log -Message "Failed to save snapshot: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Main function to create a snapshot.
#>
function Invoke-CreateSnapshot {
    Write-Log -Message "Create snapshot operation started" -Level Info
    
    # Load manifest
    $manifest = Get-BuildManifest
    if (-not $manifest) {
        return $false
    }
    
    # Display current build info
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Current Build State" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Variant: $($manifest.selectedVariant)" -ForegroundColor Yellow
    Write-Host "Name: $($manifest.variantName)" -ForegroundColor Yellow
    Write-Host "Components: $(@($manifest.components | Where-Object { $_.enabled }).Count) enabled" -ForegroundColor Yellow
    Write-Host ""
    
    # Create snapshot
    $snapshot = New-Snapshot -SnapshotName $Name -SnapshotDescription $Description -Manifest $manifest
    if (-not $snapshot) {
        return $false
    }
    
    # Save snapshot
    $filePath = Save-Snapshot -Snapshot $snapshot
    if (-not $filePath) {
        return $false
    }
    
    # Display success
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Snapshot Created Successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Snapshot ID: $($snapshot.id)" -ForegroundColor White
    if ($snapshot.name) {
        Write-Host "Name: $($snapshot.name)" -ForegroundColor White
    }
    if ($snapshot.description) {
        Write-Host "Description: $($snapshot.description)" -ForegroundColor White
    }
    Write-Host "Created: $($snapshot.created)" -ForegroundColor White
    Write-Host "Location: $filePath" -ForegroundColor Gray
    Write-Host ""
    
    return $true
}

# Main execution
try {
    $result = Invoke-CreateSnapshot
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}
