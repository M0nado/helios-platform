<#
.SYNOPSIS
Lists all available build snapshots with metadata.

.DESCRIPTION
Displays all saved snapshots with their creation dates, variants, component counts, and
descriptions. Allows filtering and sorting of snapshots. Can show detailed information
about each snapshot.

.PARAMETER ShowDetails
Show detailed information for each snapshot.

.PARAMETER SortBy
Sort snapshots by 'date', 'name', or 'components' (default: date).

.PARAMETER Limit
Limit results to N most recent snapshots (default: all).

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\list-snapshots.ps1
# List all snapshots with basic info

.EXAMPLE
.\list-snapshots.ps1 -ShowDetails -Limit 5
# Show detailed info for 5 most recent snapshots

.EXAMPLE
.\list-snapshots.ps1 -SortBy name
# List all snapshots sorted by name

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding()]
param(
    [switch]$ShowDetails,
    [ValidateSet('date', 'name', 'components')]
    [string]$SortBy = 'date',
    [int]$Limit = 0,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$snapshotsDir = Join-Path $scriptRoot "snapshots"
$logPath = Join-Path $scriptRoot "logs\snapshots.log"

# Create logs directory if needed
if (-not (Test-Path (Split-Path -Parent $logPath))) {
    New-Item -Path (Split-Path -Parent $logPath) -ItemType Directory -Force | Out-Null
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
Gets all available snapshots.

.OUTPUTS
Array of snapshot objects.
#>
function Get-AllSnapshots {
    if (-not (Test-Path $snapshotsDir)) {
        Write-Verbose "Snapshots directory not found: $snapshotsDir"
        return @()
    }
    
    try {
        $snapshots = @()
        $files = Get-ChildItem -Path $snapshotsDir -Filter "*.json" -ErrorAction SilentlyContinue
        
        Write-Verbose "Found $($files.Count) snapshot files"
        
        foreach ($file in $files) {
            try {
                $snapshot = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
                $snapshots += $snapshot
            }
            catch {
                Write-Log -Message "Failed to load snapshot $($file.Name): $_" -Level Warning
            }
        }
        
        return $snapshots
    }
    catch {
        Write-Log -Message "Failed to read snapshots directory: $_" -Level Error
        return @()
    }
}

<#
.SYNOPSIS
Formats a snapshot for display.

.OUTPUTS
Formatted snapshot information.
#>
function Format-SnapshotInfo {
    param([object]$Snapshot)
    
    $created = $snapshot.created
    if ($snapshot.created) {
        try {
            $date = [DateTime]::Parse($snapshot.created)
            $created = $date.ToString("yyyy-MM-dd HH:mm:ss")
        }
        catch {
            # Use as-is if parsing fails
        }
    }
    
    $size = if ($snapshot.manifestSnapshot.estimatedSize) {
        $snapshot.manifestSnapshot.estimatedSize
    } else {
        "Unknown"
    }
    
    return @{
        ID = $snapshot.id
        Name = $snapshot.name
        Created = $created
        Variant = $snapshot.variant
        Components = "$($snapshot.enabledComponentCount)/$($snapshot.componentCount)"
        Size = $size
        Description = $snapshot.description
    }
}

<#
.SYNOPSIS
Displays snapshots in console.
#>
function Show-SnapshotsList {
    param(
        [object[]]$Snapshots,
        [bool]$Detailed
    )
    
    if ($Snapshots.Count -eq 0) {
        Write-Host ""
        Write-Host "No snapshots found." -ForegroundColor Yellow
        Write-Host ""
        return
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Available Build Snapshots" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($Detailed) {
        # Detailed view
        $index = 1
        foreach ($snapshot in $Snapshots) {
            $formatted = Format-SnapshotInfo -Snapshot $snapshot
            
            Write-Host "[$index] $($formatted.ID)" -ForegroundColor Yellow
            
            if ($formatted.Name) {
                Write-Host "    Name: $($formatted.Name)" -ForegroundColor White
            }
            
            Write-Host "    Created: $($formatted.Created)" -ForegroundColor Gray
            Write-Host "    Variant: $($formatted.Variant)" -ForegroundColor Cyan
            Write-Host "    Components: $($formatted.Components)" -ForegroundColor Green
            Write-Host "    Size: $($formatted.Size)" -ForegroundColor White
            
            if ($formatted.Description) {
                Write-Host "    Description: $($formatted.Description)" -ForegroundColor Gray
            }
            
            if ($snapshot.manifestSnapshot -and $snapshot.manifestSnapshot.variantDescription) {
                Write-Host "    Variant Description: $($snapshot.manifestSnapshot.variantDescription)" -ForegroundColor Gray
            }
            
            Write-Host ""
            $index++
        }
    } else {
        # Compact table view
        $tableFormat = "{0,-25} | {1,-20} | {2,-20} | {3,-20} | {4,-15}"
        Write-Host ($tableFormat -f "ID", "Created", "Variant", "Components", "Size") -ForegroundColor Gray
        Write-Host ("-" * 110) -ForegroundColor Gray
        
        foreach ($snapshot in $Snapshots) {
            $formatted = Format-SnapshotInfo -Snapshot $snapshot
            Write-Host ($tableFormat -f $formatted.ID, $formatted.Created, $formatted.Variant, $formatted.Components, $formatted.Size)
        }
        
        Write-Host ""
    }
    
    Write-Host "Total Snapshots: $($Snapshots.Count)" -ForegroundColor Cyan
    Write-Host ""
}

<#
.SYNOPSIS
Main function to list snapshots.
#>
function Invoke-ListSnapshots {
    Write-Log -Message "List snapshots operation started" -Level Info
    
    # Get all snapshots
    $snapshots = Get-AllSnapshots
    
    if ($snapshots.Count -eq 0) {
        Write-Log -Message "No snapshots found" -Level Info
        Write-Host ""
        Write-Host "No snapshots available. Create one using create-snapshot.ps1" -ForegroundColor Yellow
        Write-Host ""
        return $true
    }
    
    # Sort snapshots
    $sortedSnapshots = $snapshots | Sort-Object { $_.created } -Descending
    
    if ($SortBy -eq 'name') {
        $sortedSnapshots = $snapshots | Sort-Object { $_.name -or $_.id }
    }
    elseif ($SortBy -eq 'components') {
        $sortedSnapshots = $snapshots | Sort-Object { $_.enabledComponentCount } -Descending
    }
    
    # Apply limit
    if ($Limit -gt 0) {
        $sortedSnapshots = $sortedSnapshots | Select-Object -First $Limit
        Write-Verbose "Limited results to $Limit snapshots"
    }
    
    # Display snapshots
    Show-SnapshotsList -Snapshots $sortedSnapshots -Detailed $ShowDetails
    
    Write-Log -Message "List snapshots operation completed successfully" -Level Success
    return $true
}

# Main execution
try {
    $result = Invoke-ListSnapshots
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}
