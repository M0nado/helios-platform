# Before/After System State Capture - HELIOS Platform v2

## Overview

Capturing system state before and after each phase is critical for validation, rollback, and performance measurement.

## State Capture Purpose

| Purpose | Use Case |
|---------|----------|
| **Validation** | Verify changes were applied correctly |
| **Rollback** | Restore system if something breaks |
| **Performance** | Measure improvements achieved |
| **Audit** | Document what changed and when |
| **Debugging** | Understand what caused issues |

## What to Capture

### Before Each Phase

Capture complete baseline before applying changes:

```powershell
function Capture-PrePhaseState {
    param(
        [Parameter(Mandatory=$true)]
        [int]$Phase,
        
        [Parameter(Mandatory=$true)]
        [string]$OutputPath
    )
    
    $Snapshot = @{
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Phase = $Phase
        Hostname = $env:COMPUTERNAME
        UserName = $env:USERNAME
        OS = (Get-WmiObject -Class Win32_OperatingSystem).Caption
    }
    
    # System Performance Metrics
    $Snapshot.Performance = @{
        BootTime = Measure-BootTime
        SystemMemory = Get-SystemMemoryUsage
        CPUUsage = Get-AverageCPUUsage -Duration 30
        DiskIOPS = Get-DiskIOPS -Duration 30
        ProcessCount = (Get-Process | Measure-Object).Count
    }
    
    # Registry State
    $Snapshot.Registry = @{
        Phase0 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase0" -ErrorAction SilentlyContinue
        Phase1 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase1" -ErrorAction SilentlyContinue
        Phase2 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase2" -ErrorAction SilentlyContinue
    }
    
    # Service State
    $Snapshot.Services = @{
        All = Get-Service | Select-Object Name, Status, StartType
        Running = (Get-Service | Where-Object Status -eq "Running" | Measure-Object).Count
        Stopped = (Get-Service | Where-Object Status -eq "Stopped" | Measure-Object).Count
        Disabled = (Get-Service | Where-Object StartType -eq "Disabled" | Measure-Object).Count
    }
    
    # File State
    $Snapshot.Files = @{
        HeliosDir = Get-DirectorySnapshot -Path "C:\HELIOS"
        SystemModified = Get-ModifiedFiles -Path "C:\Windows\System32" -Since (Get-Date).AddDays(-7)
        DriverFiles = Get-ChildItem -Path "C:\Windows\System32\Drivers" -ErrorAction SilentlyContinue
    }
    
    # Security State
    $Snapshot.Security = @{
        DefenderStatus = Get-MpComputerStatus
        FirewallStatus = Get-NetFirewallProfile
        UAC = Get-ItemProperty -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Policies\System" -Name "EnableLUA"
    }
    
    # Network State
    $Snapshot.Network = @{
        Adapters = Get-NetAdapter | Select-Object Name, Status, MacAddress
        DNSServers = Get-DnsClientServerAddress
        Routes = Get-NetRoute
    }
    
    # Export snapshot
    $Snapshot | ConvertTo-Json -Depth 10 | 
        Set-Content -Path "$OutputPath\phase-$Phase-before.json"
    
    Write-Host "✓ Pre-Phase $Phase snapshot captured: $OutputPath\phase-$Phase-before.json"
    
    return $Snapshot
}
```

### After Each Phase

Capture state after changes to verify execution:

```powershell
function Capture-PostPhaseState {
    param(
        [Parameter(Mandatory=$true)]
        [int]$Phase,
        
        [Parameter(Mandatory=$true)]
        [string]$OutputPath
    )
    
    $Snapshot = @{
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Phase = $Phase
        Hostname = $env:COMPUTERNAME
    }
    
    # System Performance Metrics
    $Snapshot.Performance = @{
        BootTime = Measure-BootTime
        SystemMemory = Get-SystemMemoryUsage
        CPUUsage = Get-AverageCPUUsage -Duration 30
        DiskIOPS = Get-DiskIOPS -Duration 30
        ProcessCount = (Get-Process | Measure-Object).Count
    }
    
    # Registry State
    $Snapshot.Registry = @{
        Phase0 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase0" -ErrorAction SilentlyContinue
        Phase1 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase1" -ErrorAction SilentlyContinue
        Phase2 = Get-RegistrySnapshot -Path "HKLM:\Software\HELIOS\Phase2" -ErrorAction SilentlyContinue
    }
    
    # Service State (same as before)
    $Snapshot.Services = @{
        All = Get-Service | Select-Object Name, Status, StartType
        Running = (Get-Service | Where-Object Status -eq "Running" | Measure-Object).Count
        Stopped = (Get-Service | Where-Object Status -eq "Stopped" | Measure-Object).Count
    }
    
    # Additional verification
    $Snapshot.VerificationChecks = @{
        RegistryKeysCreated = Verify-RegistryCreation -Phase $Phase
        ServicesModified = Verify-ServiceChanges -Phase $Phase
        FilesModified = Verify-FileModifications -Phase $Phase
        Errors = Get-ErrorLog -Since (Get-Date).AddMinutes(-30)
    }
    
    # Export snapshot
    $Snapshot | ConvertTo-Json -Depth 10 | 
        Set-Content -Path "$OutputPath\phase-$Phase-after.json"
    
    Write-Host "✓ Post-Phase $Phase snapshot captured: $OutputPath\phase-$Phase-after.json"
    
    return $Snapshot
}
```

## Snapshot Structure

```json
{
  "Timestamp": "2024-01-15 14:32:00",
  "Phase": 0,
  "Hostname": "WORKSTATION-01",
  "Performance": {
    "BootTime": 85,
    "SystemMemory": 2.4,
    "CPUUsage": 12.5,
    "DiskIOPS": 2400
  },
  "Registry": {
    "Phase0": [
      {
        "Path": "HKLM:\\Software\\HELIOS\\Phase0",
        "Name": "OptimizationLevel",
        "Value": "High",
        "Type": "String"
      }
    ]
  },
  "Services": {
    "Running": 156,
    "Stopped": 42,
    "Disabled": 18
  },
  "VerificationChecks": {
    "RegistryKeysCreated": 12,
    "ServicesModified": 5,
    "FilesModified": 23
  }
}
```

## Creating Snapshots

### Manual Snapshot Creation

```powershell
# Before Phase 0
$Before0 = Capture-PrePhaseState -Phase 0 -OutputPath "C:\HELIOS\snapshots"

# Execute Phase 0
.\Phase-0-Optimize.ps1

# After Phase 0
$After0 = Capture-PostPhaseState -Phase 0 -OutputPath "C:\HELIOS\snapshots"

# Compare snapshots
Compare-Snapshots -Before $Before0 -After $After0
```

### Automated Snapshot Creation

```powershell
# Create snapshots for all phases automatically
function Execute-AllPhasesWithSnapshots {
    param(
        [string]$SnapshotDir = "C:\HELIOS\snapshots"
    )
    
    $Phases = 0, 1, 2
    $Snapshots = @()
    
    foreach ($Phase in $Phases) {
        Write-Host "Phase $Phase..."
        
        # Capture before
        $Before = Capture-PrePhaseState -Phase $Phase -OutputPath $SnapshotDir
        $Snapshots += @{ Before = $Before }
        
        # Execute phase
        & ".\Phase-$Phase-$(Get-PhaseName -Phase $Phase).ps1"
        
        # Capture after
        $After = Capture-PostPhaseState -Phase $Phase -OutputPath $SnapshotDir
        $Snapshots += @{ After = $After }
        
        # Verify success
        $Comparison = Compare-Snapshots -Before $Before -After $After
        if ($Comparison.Status -ne "Success") {
            Write-Error "Phase $Phase failed verification!"
            return $false
        }
    }
    
    return $Snapshots
}
```

## Snapshot Comparison

### Comparing Before/After States

```powershell
function Compare-Snapshots {
    param(
        [Parameter(Mandatory=$true)]
        [object]$Before,
        
        [Parameter(Mandatory=$true)]
        [object]$After
    )
    
    $Comparison = @{
        Performance = Compare-Performance -Before $Before.Performance -After $After.Performance
        Registry = Compare-Registry -Before $Before.Registry -After $After.Registry
        Services = Compare-Services -Before $Before.Services -After $After.Services
        Files = Compare-Files -Before $Before.Files -After $After.Files
    }
    
    # Generate report
    $Report = @"
SNAPSHOT COMPARISON REPORT
=============================

Performance Changes:
  Boot Time: $($Comparison.Performance.BootTimeChange)
  Memory: $($Comparison.Performance.MemoryChange)
  CPU: $($Comparison.Performance.CPUChange)

Registry Changes:
  Keys Created: $($Comparison.Registry.KeysCreated)
  Values Modified: $($Comparison.Registry.ValuesModified)
  Keys Deleted: $($Comparison.Registry.KeysDeleted)

Service Changes:
  Disabled: $($Comparison.Services.Disabled)
  Enabled: $($Comparison.Services.Enabled)
  Started: $($Comparison.Services.Started)

File Changes:
  Created: $($Comparison.Files.Created)
  Modified: $($Comparison.Files.Modified)
  Deleted: $($Comparison.Files.Deleted)

Status: $($Comparison.Status)
"@
    
    Write-Host $Report
    return $Comparison
}
```

## Performance Baseline Establishment

### Establishing Baseline

```powershell
function Establish-PerformanceBaseline {
    param(
        [string]$BaselineFile = "C:\HELIOS\baseline-metrics.json"
    )
    
    Write-Host "Establishing performance baseline..."
    
    $Baseline = @{
        CapturedDate = Get-Date
        Environment = @{
            ComputerName = $env:COMPUTERNAME
            OS = (Get-WmiObject -Class Win32_OperatingSystem).Caption
            RAM = (Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB
            CPU = (Get-WmiObject -Class Win32_Processor).Name
        }
        Metrics = @{}
    }
    
    # Measure across multiple boots
    Write-Host "Measuring boot times (3 iterations)..."
    $BootTimes = @()
    for ($i = 1; $i -le 3; $i++) {
        Write-Host "  Iteration $i/3..."
        Restart-Computer -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 10
        $BootTimes += Measure-BootTime
    }
    
    $Baseline.Metrics.BootTime = @{
        Average = ($BootTimes | Measure-Object -Average).Average
        Min = ($BootTimes | Measure-Object -Minimum).Minimum
        Max = ($BootTimes | Measure-Object -Maximum).Maximum
        StdDev = Get-StandardDeviation $BootTimes
    }
    
    # Measure memory usage
    Write-Host "Measuring memory usage..."
    $MemMeasurements = @()
    for ($i = 1; $i -le 10; $i++) {
        $MemMeasurements += Get-SystemMemoryUsage
        Start-Sleep -Seconds 10
    }
    
    $Baseline.Metrics.Memory = @{
        Average = ($MemMeasurements | Measure-Object -Average).Average
        Min = ($MemMeasurements | Measure-Object -Minimum).Minimum
        Max = ($MemMeasurements | Measure-Object -Maximum).Maximum
    }
    
    # Export baseline
    $Baseline | ConvertTo-Json | Set-Content -Path $BaselineFile
    Write-Host "✓ Baseline established: $BaselineFile"
    
    return $Baseline
}
```

### Comparing Against Baseline

```powershell
function Compare-ToBaseline {
    param(
        [Parameter(Mandatory=$true)]
        [object]$CurrentMetrics,
        
        [string]$BaselineFile = "C:\HELIOS\baseline-metrics.json"
    )
    
    $Baseline = Get-Content -Path $BaselineFile | ConvertFrom-Json
    
    $Comparison = @{
        BootTimeImprovement = (($Baseline.Metrics.BootTime.Average - $CurrentMetrics.BootTime) / 
            $Baseline.Metrics.BootTime.Average) * 100
        
        MemoryImprovement = (($Baseline.Metrics.Memory.Average - $CurrentMetrics.Memory) / 
            $Baseline.Metrics.Memory.Average) * 100
    }
    
    Write-Host @"
Performance Improvement vs Baseline
====================================
Boot Time: $($Comparison.BootTimeImprovement.ToString("F1"))% faster
Memory: $($Comparison.MemoryImprovement.ToString("F1"))% lower
"@
    
    return $Comparison
}
```

## Rollback Point Management

### Creating Rollback Points

```powershell
function Create-RollbackPoint {
    param(
        [int]$Phase,
        [string]$Label = "Before-Phase-$Phase",
        [string]$RollbackDir = "C:\HELIOS\rollback"
    )
    
    $RollbackPoint = @{
        Label = $Label
        Phase = $Phase
        CreatedDate = Get-Date
        Items = @()
    }
    
    # Backup registry
    $RollbackPoint.Items += @{
        Type = "Registry"
        Path = "HKLM:\Software\HELIOS"
        Backup = Export-RegistryHive -Path "HKLM:\Software\HELIOS" `
            -ExportPath "$RollbackDir\registry-$Phase.reg"
    }
    
    # Backup critical files
    $RollbackPoint.Items += @{
        Type = "Files"
        Path = "C:\HELIOS"
        Backup = Backup-Directory -Path "C:\HELIOS" `
            -BackupPath "$RollbackDir\files-$Phase.zip"
    }
    
    # Backup service configuration
    $RollbackPoint.Items += @{
        Type = "Services"
        Services = Get-Service | Select-Object Name, Status, StartType
    }
    
    # Save rollback point manifest
    $RollbackPoint | ConvertTo-Json | 
        Set-Content -Path "$RollbackDir\rollback-$Phase.json"
    
    Write-Host "✓ Rollback point created: $RollbackDir\rollback-$Phase.json"
    
    return $RollbackPoint
}
```

### Restoring from Rollback Point

```powershell
function Restore-FromRollbackPoint {
    param(
        [int]$Phase,
        [string]$RollbackDir = "C:\HELIOS\rollback"
    )
    
    Write-Host "Restoring from Phase $Phase rollback point..."
    
    $Manifest = Get-Content -Path "$RollbackDir\rollback-$Phase.json" | ConvertFrom-Json
    
    # Restore registry
    Write-Host "Restoring registry..."
    Invoke-RegImport -Path "$RollbackDir\registry-$Phase.reg"
    
    # Restore files
    Write-Host "Restoring files..."
    Expand-Archive -Path "$RollbackDir\files-$Phase.zip" -DestinationPath "C:\" -Force
    
    # Restore services
    Write-Host "Restoring service configuration..."
    foreach ($ServiceConfig in $Manifest.Items | Where-Object Type -eq "Services") {
        foreach ($Service in $ServiceConfig.Services) {
            Set-Service -Name $Service.Name -StartupType $Service.StartType -ErrorAction SilentlyContinue
        }
    }
    
    Write-Host "✓ Rollback complete"
}
```

## Snapshot Management

### Listing Snapshots

```powershell
Get-ChildItem -Path "C:\HELIOS\snapshots" -Filter "phase-*.json" | 
    ForEach-Object {
        $Data = Get-Content $_.FullName | ConvertFrom-Json
        [PSCustomObject]@{
            File = $_.Name
            Timestamp = $Data.Timestamp
            Phase = $Data.Phase
            Type = if ($_.Name -match "before") { "Before" } else { "After" }
        }
    } | Format-Table -AutoSize
```

### Cleanup Old Snapshots

```powershell
function Cleanup-OldSnapshots {
    param(
        [int]$DaysToKeep = 30,
        [string]$SnapshotDir = "C:\HELIOS\snapshots"
    )
    
    $CutoffDate = (Get-Date).AddDays(-$DaysToKeep)
    
    Get-ChildItem -Path $SnapshotDir -Filter "*.json" | 
        Where-Object { $_.LastWriteTime -lt $CutoffDate } | 
        Remove-Item -Force
    
    Write-Host "✓ Cleaned up snapshots older than $DaysToKeep days"
}
```

---

**Version:** 2.0  
**Last Updated:** 2024  
**Maintained By:** HELIOS Development Team
