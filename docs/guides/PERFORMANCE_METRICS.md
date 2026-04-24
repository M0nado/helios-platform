# Performance Metrics - HELIOS Platform v2

## Overview

Systematic performance measurement to verify HELIOS Platform v2 delivers promised improvements.

## Key Performance Indicators (KPIs)

| Metric | Baseline | Target | Unit |
|--------|----------|--------|------|
| Boot Time | TBD | -20% | seconds |
| App Launch | TBD | -15% | milliseconds |
| Idle Memory | TBD | -10% | GB |
| Idle CPU | TBD | -25% | % |
| Disk I/O | TBD | -30% | IOPS |

## Measurement Tools

### Built-in Windows Tools

```powershell
# System Information
Get-WmiObject -Class Win32_OperatingSystem | Select-Object *

# Performance Monitor
Get-WmiObject -Class Win32_PerfFormattedData_PerfOS_System

# Event Logs
Get-EventLog -LogName System -Source "EventLog" -Newest 100

# Task Manager Data
Get-Process | Select-Object Name, CPU, Memory, Handles
```

### HELIOS Custom Metrics

```powershell
# Boot time measurement
function Measure-BootTime {
    $LastBoot = Get-WmiObject -Class Win32_OperatingSystem | 
        Select-Object -ExpandProperty LastBootUpTime
    
    $CurrentTime = Get-Date
    $BootDuration = ($CurrentTime - [Management.ManagementDateTimeConverter]::ToDateTime($LastBoot)).TotalSeconds
    
    Write-Output $BootDuration
}

# Application launch time
function Measure-AppLaunchTime {
    param(
        [Parameter(Mandatory=$true)]
        [string]$AppPath
    )
    
    $StartTime = Get-Date
    $Process = Start-Process -FilePath $AppPath -PassThru -WindowStyle Hidden
    
    # Wait for window to appear or timeout
    $WindowAppeared = $false
    for ($i = 0; $i -lt 50; $i++) {
        if ($Process.MainWindowHandle -ne 0) {
            $WindowAppeared = $true
            break
        }
        Start-Sleep -Milliseconds 100
    }
    
    $EndTime = Get-Date
    $Duration = ($EndTime - $StartTime).TotalMilliseconds
    
    Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    
    Write-Output $Duration
}

# Memory usage measurement
function Measure-SystemMemoryUsage {
    $OS = Get-WmiObject -Class Win32_OperatingSystem
    $TotalMemory = $OS.TotalVisibleMemorySize
    $FreeMemory = $OS.FreePhysicalMemory
    $UsedMemory = $TotalMemory - $FreeMemory
    
    $UsagePercent = ($UsedMemory / $TotalMemory) * 100
    
    Write-Output @{
        Used = $UsedMemory / 1024 / 1024  # GB
        Total = $TotalMemory / 1024 / 1024  # GB
        Percent = $UsagePercent
    }
}

# CPU usage measurement
function Measure-AverageCPUUsage {
    param(
        [int]$DurationSeconds = 30,
        [int]$SampleIntervalMs = 1000
    )
    
    $Samples = @()
    $EndTime = (Get-Date).AddSeconds($DurationSeconds)
    
    while ((Get-Date) -lt $EndTime) {
        $CPUUsage = Get-WmiObject -Class Win32_PerfFormattedData_PerfOS_Processor | 
            Where-Object { $_.Name -eq "_Total" } | 
            Select-Object -ExpandProperty "PercentProcessorTime"
        
        $Samples += $CPUUsage
        Start-Sleep -Milliseconds $SampleIntervalMs
    }
    
    $Average = ($Samples | Measure-Object -Average).Average
    Write-Output $Average
}

# Disk I/O measurement
function Measure-DiskIOPS {
    param(
        [int]$DurationSeconds = 30,
        [int]$SampleIntervalMs = 1000
    )
    
    $Drive = Get-Volume -ErrorAction SilentlyContinue | Select-Object -First 1
    $Samples = @()
    $EndTime = (Get-Date).AddSeconds($DurationSeconds)
    
    while ((Get-Date) -lt $EndTime) {
        $Counter = (Get-Counter -Counter "\PhysicalDisk(_Total)\Disk Transfers/sec").CounterSamples[0].CookedValue
        $Samples += $Counter
        Start-Sleep -Milliseconds $SampleIntervalMs
    }
    
    $Average = ($Samples | Measure-Object -Average).Average
    Write-Output $Average
}
```

## Baseline Establishment

### Phase 0: Establish Initial Baseline

**Before any HELIOS phases are applied**

```powershell
function Establish-Baseline {
    param(
        [string]$BaselineFile = "C:\HELIOS\baseline.json"
    )
    
    Write-Host "Establishing baseline metrics..."
    
    $Baseline = @{
        CapturedDate = Get-Date
        Phase = "Baseline"
        System = @{
            Hostname = $env:COMPUTERNAME
            OS = (Get-WmiObject -Class Win32_OperatingSystem).Caption
            OSVersion = (Get-WmiObject -Class Win32_OperatingSystem).Version
            Processor = (Get-WmiObject -Class Win32_Processor).Name
            RAM = (Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB
            DiskSize = (Get-WmiObject -Class Win32_LogicalDisk | Where-Object DriveType -eq 3).Size / 1GB
        }
        Metrics = @{}
    }
    
    Write-Host "Measuring boot time (3 iterations)..."
    $BootTimes = @()
    for ($i = 1; $i -le 3; $i++) {
        Write-Host "  Boot $i/3..."
        Restart-Computer -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 5
        $BootTimes += Measure-BootTime
        Write-Host "    Time: $($BootTimes[-1]) seconds"
    }
    
    $Baseline.Metrics.BootTime = @{
        Values = $BootTimes
        Average = ($BootTimes | Measure-Object -Average).Average
        Min = ($BootTimes | Measure-Object -Minimum).Minimum
        Max = ($BootTimes | Measure-Object -Maximum).Maximum
        StdDev = Get-StandardDeviation -Values $BootTimes
    }
    
    Write-Host "Measuring memory usage..."
    $MemSamples = @()
    for ($i = 1; $i -le 20; $i++) {
        $MemSamples += (Measure-SystemMemoryUsage).Used
        Start-Sleep -Seconds 5
    }
    
    $Baseline.Metrics.Memory = @{
        Average = ($MemSamples | Measure-Object -Average).Average
        Min = ($MemSamples | Measure-Object -Minimum).Minimum
        Max = ($MemSamples | Measure-Object -Maximum).Maximum
    }
    
    Write-Host "Measuring CPU usage..."
    $Baseline.Metrics.CPU = @{
        IdleAverage = Measure-AverageCPUUsage -DurationSeconds 60
    }
    
    Write-Host "Measuring disk I/O..."
    $Baseline.Metrics.DiskIO = @{
        IdleIOPS = Measure-DiskIOPS -DurationSeconds 60
    }
    
    Write-Host "Measuring app launch times..."
    $Baseline.Metrics.AppLaunch = @{
        Notepad = (Measure-AppLaunchTime -AppPath "notepad.exe" | Measure-Object -Average).Average
        Calc = (Measure-AppLaunchTime -AppPath "calc.exe" | Measure-Object -Average).Average
    }
    
    # Save baseline
    $Baseline | ConvertTo-Json -Depth 10 | Set-Content -Path $BaselineFile
    Write-Host "✓ Baseline saved: $BaselineFile"
    
    return $Baseline
}
```

## Post-Phase Measurement

### Phase 1: After Phase 0 (Optimization)

```powershell
function Measure-AfterPhase0 {
    param(
        [string]$OutputFile = "C:\HELIOS\metrics-phase0.json"
    )
    
    Write-Host "Measuring system performance after Phase 0..."
    
    $Metrics = @{
        CapturedDate = Get-Date
        Phase = 0
        Metrics = @{}
    }
    
    # Repeat same measurements as baseline
    Write-Host "Measuring boot time..."
    $BootTimes = @()
    for ($i = 1; $i -le 3; $i++) {
        Restart-Computer -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 5
        $BootTimes += Measure-BootTime
    }
    
    $Metrics.Metrics.BootTime = @{
        Average = ($BootTimes | Measure-Object -Average).Average
        Min = ($BootTimes | Measure-Object -Minimum).Minimum
        Max = ($BootTimes | Measure-Object -Maximum).Maximum
    }
    
    # All other metrics...
    
    # Calculate improvements
    $Baseline = Get-Content -Path "C:\HELIOS\baseline.json" | ConvertFrom-Json
    
    $BootImprovement = (($Baseline.Metrics.BootTime.Average - $Metrics.Metrics.BootTime.Average) / 
        $Baseline.Metrics.BootTime.Average) * 100
    
    $Metrics.Improvement = @{
        BootTime = @{
            Before = $Baseline.Metrics.BootTime.Average
            After = $Metrics.Metrics.BootTime.Average
            PercentChange = $BootImprovement
        }
    }
    
    $Metrics | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile
    Write-Host "✓ Metrics saved: $OutputFile"
    
    return $Metrics
}
```

## Performance Comparison

### Side-by-Side Metrics

```powershell
function Compare-Metrics {
    param(
        [object]$Baseline,
        [object]$After,
        [string]$OutputReport = "C:\HELIOS\performance-report.txt"
    )
    
    $Report = @"
╔════════════════════════════════════════════════════════════╗
║         HELIOS PLATFORM v2 - PERFORMANCE REPORT            ║
╚════════════════════════════════════════════════════════════╝

SYSTEM INFORMATION
──────────────────────────────────────────────────────────────
Computer:     $($Baseline.System.Hostname)
OS:           $($Baseline.System.OS)
Processor:    $($Baseline.System.Processor)
RAM:          $($Baseline.System.RAM)GB

BOOT TIME PERFORMANCE
──────────────────────────────────────────────────────────────
Before:       $($Baseline.Metrics.BootTime.Average.ToString('F1')) seconds
After:        $($After.Metrics.BootTime.Average.ToString('F1')) seconds
Improvement:  $(((($Baseline.Metrics.BootTime.Average - $After.Metrics.BootTime.Average) / $Baseline.Metrics.BootTime.Average) * 100).ToString('F1'))%

MEMORY USAGE (MB)
──────────────────────────────────────────────────────────────
Before:       $($Baseline.Metrics.Memory.Average.ToString('F0'))
After:        $($After.Metrics.Memory.Average.ToString('F0'))
Improvement:  $(((($Baseline.Metrics.Memory.Average - $After.Metrics.Memory.Average) / $Baseline.Metrics.Memory.Average) * 100).ToString('F1'))%

CPU USAGE (Idle %)
──────────────────────────────────────────────────────────────
Before:       $($Baseline.Metrics.CPU.IdleAverage.ToString('F1'))%
After:        $($After.Metrics.CPU.IdleAverage.ToString('F1'))%
Improvement:  $(((($Baseline.Metrics.CPU.IdleAverage - $After.Metrics.CPU.IdleAverage) / $Baseline.Metrics.CPU.IdleAverage) * 100).ToString('F1'))%

DISK I/O (IOPS)
──────────────────────────────────────────────────────────────
Before:       $($Baseline.Metrics.DiskIO.IdleIOPS.ToString('F0'))
After:        $($After.Metrics.DiskIO.IdleIOPS.ToString('F0'))
Improvement:  $(((($Baseline.Metrics.DiskIO.IdleIOPS - $After.Metrics.DiskIO.IdleIOPS) / $Baseline.Metrics.DiskIO.IdleIOPS) * 100).ToString('F1'))%

APPLICATION LAUNCH TIME (ms)
──────────────────────────────────────────────────────────────
Notepad Before: $($Baseline.Metrics.AppLaunch.Notepad.ToString('F0'))ms
Notepad After:  $($After.Metrics.AppLaunch.Notepad.ToString('F0'))ms
Improvement:    $(((($Baseline.Metrics.AppLaunch.Notepad - $After.Metrics.AppLaunch.Notepad) / $Baseline.Metrics.AppLaunch.Notepad) * 100).ToString('F1'))%

OVERALL ASSESSMENT
──────────────────────────────────────────────────────────────
✓ Boot time improved by XX%
✓ Memory usage reduced by XX%
✓ CPU load reduced by XX%
✓ Disk I/O reduced by XX%
✓ Application launch faster by XX%

Report Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@
    
    $Report | Set-Content -Path $OutputReport
    Write-Host $Report
}
```

## Continuous Performance Monitoring

### Monitoring Service

```powershell
function Start-PerformanceMonitoring {
    param(
        [int]$IntervalSeconds = 300,  # 5 minutes
        [int]$RetentionDays = 7
    )
    
    $MonitoringScript = {
        param($Interval, $Retention)
        
        $MetricsDir = "C:\HELIOS\monitoring"
        if (-not (Test-Path $MetricsDir)) {
            New-Item -Path $MetricsDir -ItemType Directory -Force | Out-Null
        }
        
        while ($true) {
            $Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
            $MetricFile = Join-Path $MetricsDir "metrics_$Timestamp.json"
            
            $CurrentMetrics = @{
                Timestamp = Get-Date
                Memory = Measure-SystemMemoryUsage
                CPU = Measure-AverageCPUUsage -DurationSeconds 5
                DiskIO = Measure-DiskIOPS -DurationSeconds 5
            }
            
            $CurrentMetrics | ConvertTo-Json | Set-Content -Path $MetricFile
            
            # Cleanup old metrics
            Get-ChildItem -Path $MetricsDir -Filter "metrics_*.json" | 
                Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$Retention) } | 
                Remove-Item -Force
            
            Start-Sleep -Seconds $Interval
        }
    }
    
    $Job = Start-Job -ScriptBlock $MonitoringScript -ArgumentList $IntervalSeconds, $RetentionDays
    Write-Host "✓ Performance monitoring started (Job ID: $($Job.Id))"
    
    return $Job
}

function Get-PerformanceReport {
    param(
        [int]$HoursBack = 24
    )
    
    $MetricsDir = "C:\HELIOS\monitoring"
    $StartTime = (Get-Date).AddHours(-$HoursBack)
    
    $Metrics = Get-ChildItem -Path $MetricsDir -Filter "metrics_*.json" | 
        Where-Object { $_.LastWriteTime -gt $StartTime } | 
        ForEach-Object { Get-Content $_.FullName | ConvertFrom-Json }
    
    return @{
        AvgMemory = ($Metrics.Memory.Used | Measure-Object -Average).Average
        AvgCPU = ($Metrics.CPU | Measure-Object -Average).Average
        AvgDiskIO = ($Metrics.DiskIO | Measure-Object -Average).Average
        SampleCount = $Metrics.Count
    }
}
```

## Performance Regression Testing

### Detecting Performance Regressions

```powershell
function Test-PerformanceRegression {
    param(
        [object]$Baseline,
        [object]$Current,
        [double]$TolerancePercent = 5
    )
    
    $Regressions = @()
    
    # Check boot time
    $BootRegression = (($Current.Metrics.BootTime.Average - $Baseline.Metrics.BootTime.Average) / 
        $Baseline.Metrics.BootTime.Average) * 100
    
    if ($BootRegression -gt $TolerancePercent) {
        $Regressions += "Boot time regressed by $($BootRegression.ToString('F1'))%"
    }
    
    # Check memory
    $MemRegression = (($Current.Metrics.Memory.Average - $Baseline.Metrics.Memory.Average) / 
        $Baseline.Metrics.Memory.Average) * 100
    
    if ($MemRegression -gt $TolerancePercent) {
        $Regressions += "Memory usage regressed by $($MemRegression.ToString('F1'))%"
    }
    
    # Check CPU
    $CPURegression = (($Current.Metrics.CPU.IdleAverage - $Baseline.Metrics.CPU.IdleAverage) / 
        $Baseline.Metrics.CPU.IdleAverage) * 100
    
    if ($CPURegression -gt $TolerancePercent) {
        $Regressions += "CPU usage regressed by $($CPURegression.ToString('F1'))%"
    }
    
    if ($Regressions.Count -gt 0) {
        Write-Warning "Performance Regressions Detected:"
        $Regressions | ForEach-Object { Write-Warning "  - $_" }
        return $false
    }
    
    Write-Host "✓ No performance regressions detected"
    return $true
}
```

---

**Version:** 2.0  
**Last Updated:** 2024  
**Maintained By:** HELIOS Development Team
