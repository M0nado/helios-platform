#!/usr/bin/env pwsh
<#
.SYNOPSIS
    HELIOS Partition Manager - Advanced Drive Management

.DESCRIPTION
    Manages partitions, DevDrive creation, and storage optimization.
    Provides partition analysis and recommendations.

.PARAMETER Analyze
    Analyze current partition configuration

.PARAMETER CreateDevDrive
    Create a DevDrive for optimized development

.PARAMETER Optimize
    Optimize partition settings

.PARAMETER ShowRecommendations
    Show storage recommendations

.EXAMPLE
    .\Manage-Partitions.ps1 -Analyze -ShowRecommendations
    .\Manage-Partitions.ps1 -CreateDevDrive -DriveLetter E

.AUTHOR
    HELIOS Solutions
#>

param(
    [switch]$Analyze,
    [switch]$CreateDevDrive,
    [switch]$Optimize,
    [switch]$ShowRecommendations,
    [char]$DriveLetter = 'E'
)

$ErrorActionPreference = "Continue"

# ============================================================================
# PARTITION ANALYSIS
# ============================================================================

function Get-PartitionAnalysis {
    Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  PARTITION ANALYSIS" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

    $partitions = Get-Volume
    
    $partitions | ForEach-Object {
        $driveLetter = $_.DriveLetter
        if ([string]::IsNullOrEmpty($driveLetter)) { return }
        
        try {
            $disk = Get-Item "$($driveLetter):" -ErrorAction SilentlyContinue
            $totalSize = (Get-Item "$($driveLetter):").PSDrive.Used + (Get-Item "$($driveLetter):").PSDrive.Free
            $usedSpace = (Get-Item "$($driveLetter):").PSDrive.Used
            $freeSpace = (Get-Item "$($driveLetter):").PSDrive.Free
            
            $usedPercent = if ($totalSize -gt 0) { [Math]::Round(($usedSpace / $totalSize) * 100, 2) } else { 0 }
            
            Write-Host "  Drive: $($driveLetter):" -ForegroundColor Green
            Write-Host "  Label: $($_.FileSystemLabel)" -ForegroundColor White
            Write-Host "  FileSystem: $($_.FileSystem)" -ForegroundColor White
            Write-Host "  Total: $([Math]::Round($totalSize / 1GB, 2)) GB" -ForegroundColor Cyan
            Write-Host "  Used: $([Math]::Round($usedSpace / 1GB, 2)) GB ($usedPercent%)" -ForegroundColor Cyan
            Write-Host "  Free: $([Math]::Round($freeSpace / 1GB, 2)) GB" -ForegroundColor Cyan
            
            # Draw usage bar
            $barLength = 30
            $filledLength = [Math]::Round(($usedPercent / 100) * $barLength)
            $bar = "█" * $filledLength + "░" * ($barLength - $filledLength)
            $barColor = if ($usedPercent -gt 80) { "Red" } elseif ($usedPercent -gt 60) { "Yellow" } else { "Green" }
            Write-Host "  [$bar] $usedPercent%" -ForegroundColor $barColor
            Write-Host ""
        }
        catch {
            # Skip inaccessible drives
        }
    }
}

function Get-PartitionRecommendations {
    Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  STORAGE RECOMMENDATIONS" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

    $recommendations = @()
    
    # Analyze each partition
    Get-Volume | Where-Object { -not [string]::IsNullOrEmpty($_.DriveLetter) } | ForEach-Object {
        $driveLetter = $_.DriveLetter
        try {
            $totalSize = (Get-Item "$($driveLetter):").PSDrive.Used + (Get-Item "$($driveLetter):").PSDrive.Free
            $usedSpace = (Get-Item "$($driveLetter):").PSDrive.Used
            $usedPercent = if ($totalSize -gt 0) { [Math]::Round(($usedSpace / $totalSize) * 100, 2) } else { 0 }
            $freeSpace = (Get-Item "$($driveLetter):").PSDrive.Free
            
            # Check for fragmentation risk
            if ($usedPercent -gt 80) {
                $recommendations += @{
                    Drive = "$($driveLetter):"
                    Issue = "Low Disk Space"
                    Severity = "High"
                    Action = "Consider freeing up space or expanding partition"
                }
            }
            
            # Check for system drive
            if ($driveLetter -eq $env:SystemDrive.Substring(0, 1)) {
                if ($usedPercent -gt 60) {
                    $recommendations += @{
                        Drive = "$($driveLetter):"
                        Issue = "System Drive High Usage"
                        Severity = "Medium"
                        Action = "Move user files to secondary drive"
                    }
                }
            }
            
            # Check for small free space
            if ($freeSpace / 1GB -lt 1) {
                $recommendations += @{
                    Drive = "$($driveLetter):"
                    Issue = "Critically Low Free Space"
                    Severity = "Critical"
                    Action = "Immediate action required - free up space"
                }
            }
        }
        catch { }
    }
    
    if ($recommendations.Count -eq 0) {
        Write-Host "  ✓ No critical issues found" -ForegroundColor Green
        Write-Host "  All partitions are in healthy state" -ForegroundColor White
    }
    else {
        foreach ($rec in $recommendations) {
            $severityColor = switch ($rec.Severity) {
                "Critical" { "Red" }
                "High" { "Yellow" }
                "Medium" { "Cyan" }
                default { "White" }
            }
            
            Write-Host "  [$($rec.Severity)] $($rec.Drive)" -ForegroundColor $severityColor
            Write-Host "    Issue: $($rec.Issue)" -ForegroundColor White
            Write-Host "    Action: $($rec.Action)" -ForegroundColor Cyan
            Write-Host ""
        }
    }
}

function New-DevDrive {
    param([char]$Letter = 'E')
    
    Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  DEVDRIVE CREATION" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

    Write-Host "DevDrive is an optimized storage format for development." -ForegroundColor Cyan
    Write-Host "Requires Windows 11 24H2 or later." -ForegroundColor Yellow
    
    # Check Windows version
    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Build -lt 26100) {
        Write-Host "Error: Your Windows version does not support DevDrive" -ForegroundColor Red
        return $false
    }
    
    Write-Host "`nNote: Full DevDrive creation requires additional tools." -ForegroundColor Cyan
    Write-Host "This is a simulation of the DevDrive setup process." -ForegroundColor Yellow
    
    # For now, provide guidance
    Write-Host "`nTo create a real DevDrive, use:" -ForegroundColor Green
    Write-Host "  format $($Letter): /FS:ReFS /DevDrive" -ForegroundColor White
    
    Write-Host "`nDevDrive Benefits:" -ForegroundColor Cyan
    Write-Host "  • 50% faster file operations" -ForegroundColor White
    Write-Host "  • Optimized for development workloads" -ForegroundColor White
    Write-Host "  • Better compression and deduplication" -ForegroundColor White
    
    return $true
}

function Optimize-Partitions {
    Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  PARTITION OPTIMIZATION" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

    # Optimize NTFS volumes
    Get-Volume | Where-Object { $_.FileSystem -eq "NTFS" -and -not [string]::IsNullOrEmpty($_.DriveLetter) } | ForEach-Object {
        $driveLetter = $_.DriveLetter
        Write-Host "Optimizing $($driveLetter):" -ForegroundColor Cyan
        
        try {
            # Defragment
            Optimize-Volume -DriveLetter $driveLetter -Defrag -Verbose:$false
            Write-Host "  ✓ Defragmentation completed" -ForegroundColor Green
        }
        catch {
            Write-Host "  Note: Could not defragment" -ForegroundColor Yellow
        }
        
        try {
            # Trim (for SSDs)
            Optimize-Volume -DriveLetter $driveLetter -TrimFront -Verbose:$false
            Write-Host "  ✓ SSD trimming completed" -ForegroundColor Green
        }
        catch {
            Write-Host "  Note: Could not trim" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n✓ Optimization completed" -ForegroundColor Green
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

if ($Analyze) {
    Get-PartitionAnalysis
}

if ($ShowRecommendations) {
    Get-PartitionRecommendations
}

if ($CreateDevDrive) {
    New-DevDrive -Letter $DriveLetter
}

if ($Optimize) {
    Optimize-Partitions
}

if (-not $Analyze -and -not $ShowRecommendations -and -not $CreateDevDrive -and -not $Optimize) {
    Write-Host "HELIOS Partition Manager" -ForegroundColor Cyan
    Write-Host "Usage: .\Manage-Partitions.ps1 -Analyze -ShowRecommendations" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    Get-Help $PSCommandPath -Full
}
