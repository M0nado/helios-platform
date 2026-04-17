<#
.SYNOPSIS
    Real-time metrics collection and display
.DESCRIPTION
    Continuous collection and streaming of live performance metrics
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "Real-Time-Metrics"

class RealtimeMetricsCollector {
    [hashtable]$CurrentMetrics
    [array]$MetricsHistory
    [int]$HistoryMaxSize = 1000
    [datetime]$CollectionStartTime
    
    RealtimeMetricsCollector() {
        $this.CurrentMetrics = @{}
        $this.MetricsHistory = @()
        $this.CollectionStartTime = Get-Date
    }
    
    [void]CollectSystemMetrics() {
        try {
            # CPU Metrics
            $cpuCounter = Get-Counter -Counter "\Processor(_Total)\% Processor Time" -ErrorAction SilentlyContinue
            $cpu = $cpuCounter.CounterSamples[0].CookedValue
            
            # Memory Metrics
            $memObj = Get-WmiObject Win32_OperatingSystem -ErrorAction SilentlyContinue
            $memUsed = $memObj.TotalVisibleMemorySize - $memObj.FreePhysicalMemory
            $memPercent = ($memUsed / $memObj.TotalVisibleMemorySize) * 100
            
            # Disk Metrics
            $diskMetrics = @()
            Get-Volume -ErrorAction SilentlyContinue | ForEach-Object {
                if ($_.SizeRemaining -gt 0) {
                    $diskPercent = (($_.Size - $_.SizeRemaining) / $_.Size) * 100
                    $diskMetrics += @{
                        Drive = $_.DriveLetter
                        SizeGB = [Math]::Round($_.Size / 1GB, 2)
                        UsedGB = [Math]::Round(($_.Size - $_.SizeRemaining) / 1GB, 2)
                        FreeGB = [Math]::Round($_.SizeRemaining / 1GB, 2)
                        PercentUsed = [Math]::Round($diskPercent, 2)
                    }
                }
            }
            
            # Network Metrics
            $networkMetrics = @()
            Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Up" } | ForEach-Object {
                $stats = Get-NetAdapterStatistics -Name $_.Name -ErrorAction SilentlyContinue
                if ($stats) {
                    $networkMetrics += @{
                        Interface = $_.Name
                        BytesReceived = $stats.ReceivedBytes
                        BytesSent = $stats.SentBytes
                        PacketsReceived = $stats.ReceivedUnicastPackets
                        PacketsSent = $stats.SentUnicastPackets
                    }
                }
            }
            
            $this.CurrentMetrics = @{
                Timestamp = Get-Date -AsUTC
                CPU = [Math]::Round($cpu, 2)
                Memory = @{
                    PercentUsed = [Math]::Round($memPercent, 2)
                    UsedMB = [Math]::Round($memUsed / 1MB, 2)
                    TotalMB = [Math]::Round($memObj.TotalVisibleMemorySize / 1MB, 2)
                }
                Disk = $diskMetrics
                Network = $networkMetrics
                ProcessCount = @(Get-Process -ErrorAction SilentlyContinue).Count
            }
            
            $this.AddToHistory($this.CurrentMetrics)
        }
        catch {
            Write-MonitoringLog "Error collecting system metrics: $_" -Level "ERROR"
        }
    }
    
    [void]CollectApplicationMetrics() {
        try {
            $appMetrics = @{}
            
            # Get key processes
            $processes = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Handles -gt 100 } | Select-Object -First 10
            
            $appMetrics.TopProcesses = @()
            foreach ($proc in $processes) {
                $appMetrics.TopProcesses += @{
                    Name = $proc.Name
                    CPU = [Math]::Round($proc.CPU, 2)
                    MemoryMB = [Math]::Round($proc.WorkingSet / 1MB, 2)
                    Handles = $proc.Handles
                    Threads = $proc.Threads.Count
                }
            }
            
            $this.CurrentMetrics.Applications = $appMetrics
        }
        catch {
            Write-MonitoringLog "Error collecting application metrics: $_" -Level "ERROR"
        }
    }
    
    [void]CollectServiceMetrics() {
        try {
            $services = @()
            $criticalServices = @("WinRM", "EventLog", "Winlogon", "LanmanServer", "Dnscache")
            
            foreach ($serviceName in $criticalServices) {
                $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                if ($svc) {
                    $services += @{
                        Name = $svc.Name
                        DisplayName = $svc.DisplayName
                        Status = $svc.Status.ToString()
                        StartType = $svc.StartType.ToString()
                    }
                }
            }
            
            $this.CurrentMetrics.Services = $services
        }
        catch {
            Write-MonitoringLog "Error collecting service metrics: $_" -Level "ERROR"
        }
    }
    
    [void]AddToHistory([hashtable]$Metric) {
        $this.MetricsHistory += $Metric
        
        if ($this.MetricsHistory.Count -gt $this.HistoryMaxSize) {
            $this.MetricsHistory = $this.MetricsHistory[-$this.HistoryMaxSize..-1]
        }
    }
    
    [hashtable]GetMetricsSnapshot() {
        return $this.CurrentMetrics.Clone()
    }
    
    [array]GetMetricsHistory([int]$LastNRecords = 100) {
        $startIndex = [Math]::Max(0, $this.MetricsHistory.Count - $LastNRecords)
        return $this.MetricsHistory[$startIndex..($this.MetricsHistory.Count - 1)]
    }
    
    [hashtable]GetMetricsAnalysis() {
        if ($this.MetricsHistory.Count -lt 2) {
            return @{}
        }
        
        $history = $this.MetricsHistory
        $cpuValues = $history | Select-Object -ExpandProperty CPU
        $memValues = $history | Select-Object -ExpandProperty Memory | Select-Object -ExpandProperty PercentUsed
        
        return @{
            CPU = @{
                Current = $cpuValues[-1]
                Average = [Math]::Round(($cpuValues | Measure-Object -Average).Average, 2)
                Min = [Math]::Round(($cpuValues | Measure-Object -Minimum).Minimum, 2)
                Max = [Math]::Round(($cpuValues | Measure-Object -Maximum).Maximum, 2)
            }
            Memory = @{
                Current = $memValues[-1]
                Average = [Math]::Round(($memValues | Measure-Object -Average).Average, 2)
                Min = [Math]::Round(($memValues | Measure-Object -Minimum).Minimum, 2)
                Max = [Math]::Round(($memValues | Measure-Object -Maximum).Maximum, 2)
            }
            UpTime = New-TimeSpan -Start $this.CollectionStartTime -End (Get-Date)
        }
    }
}

function Start-RealtimeMetricsCollection {
    param(
        [int]$CollectionIntervalSeconds = 5,
        [int]$DisplayIntervalSeconds = 10
    )
    
    Write-MonitoringLog "Starting real-time metrics collection..."
    
    $collector = [RealtimeMetricsCollector]::new()
    $displayCounter = 0
    
    while ($true) {
        try {
            # Collect all metrics
            $collector.CollectSystemMetrics()
            $collector.CollectApplicationMetrics()
            $collector.CollectServiceMetrics()
            
            $displayCounter++
            
            if ($displayCounter * $CollectionIntervalSeconds -ge $DisplayIntervalSeconds) {
                DisplayMetricsDashboard -Collector $collector
                $displayCounter = 0
            }
            
            Start-Sleep -Seconds $CollectionIntervalSeconds
        }
        catch {
            Write-MonitoringLog "Metrics collection error: $_" -Level "ERROR"
            Start-Sleep -Seconds $CollectionIntervalSeconds
        }
    }
}

function DisplayMetricsDashboard {
    param([RealtimeMetricsCollector]$Collector)
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  REAL-TIME METRICS - Live System Performance                  ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    $metrics = $Collector.GetMetricsSnapshot()
    $analysis = $Collector.GetMetricsAnalysis()
    
    # CPU Display
    Write-Host "CPU USAGE" -ForegroundColor Yellow
    $cpuColor = if ($metrics.CPU -gt 80) { "Red" } elseif ($metrics.CPU -gt 60) { "Yellow" } else { "Green" }
    Write-Host "  Current: $($metrics.CPU)%" -ForegroundColor $cpuColor
    if ($analysis.CPU) {
        Write-Host "  Average: $($analysis.CPU.Average)% | Min: $($analysis.CPU.Min)% | Max: $($analysis.CPU.Max)%"
    }
    Write-Host ""
    
    # Memory Display
    Write-Host "MEMORY USAGE" -ForegroundColor Yellow
    $memColor = if ($metrics.Memory.PercentUsed -gt 85) { "Red" } elseif ($metrics.Memory.PercentUsed -gt 70) { "Yellow" } else { "Green" }
    Write-Host "  Current: $($metrics.Memory.PercentUsed)% ($($metrics.Memory.UsedMB)MB/$($metrics.Memory.TotalMB)MB)" -ForegroundColor $memColor
    if ($analysis.Memory) {
        Write-Host "  Average: $($analysis.Memory.Average)% | Min: $($analysis.Memory.Min)% | Max: $($analysis.Memory.Max)%"
    }
    Write-Host ""
    
    # Disk Display
    if ($metrics.Disk) {
        Write-Host "DISK USAGE" -ForegroundColor Yellow
        foreach ($disk in $metrics.Disk) {
            $diskColor = if ($disk.PercentUsed -gt 90) { "Red" } elseif ($disk.PercentUsed -gt 75) { "Yellow" } else { "Green" }
            Write-Host "  $($disk.Drive):: $($disk.PercentUsed)% ($($disk.UsedGB)GB/$($disk.SizeGB)GB)" -ForegroundColor $diskColor
        }
        Write-Host ""
    }
    
    # Top Processes
    if ($metrics.Applications -and $metrics.Applications.TopProcesses) {
        Write-Host "TOP PROCESSES" -ForegroundColor Yellow
        $metrics.Applications.TopProcesses | Select-Object -First 5 | ForEach-Object {
            Write-Host "  $($_.Name) - CPU: $($_.CPU)% | Mem: $($_.MemoryMB)MB"
        }
        Write-Host ""
    }
    
    # Services Status
    if ($metrics.Services) {
        Write-Host "CRITICAL SERVICES" -ForegroundColor Yellow
        $metrics.Services | Where-Object { $_.Status -ne "Running" } | ForEach-Object {
            Write-Host "  ⚠ $($_.DisplayName): $($_.Status)" -ForegroundColor Red
        }
        $runningCount = @($metrics.Services | Where-Object { $_.Status -eq "Running" }).Count
        Write-Host "  ✓ $runningCount/$($metrics.Services.Count) services running" -ForegroundColor Green
        Write-Host ""
    }
    
    Write-Host "Updated: $(Get-Date -Format 'HH:mm:ss') | Uptime: $($analysis.UpTime.Days)d $($analysis.UpTime.Hours)h $($analysis.UpTime.Minutes)m" -ForegroundColor Gray
}

function Get-MetricsExport {
    param(
        [RealtimeMetricsCollector]$Collector,
        [string]$OutputPath = "$PSScriptRoot\..\logs\metrics-export.json",
        [int]$LastRecords = 100
    )
    
    $history = $Collector.GetMetricsHistory($LastRecords)
    $export = @{
        ExportTime = Get-Date -AsUTC
        RecordCount = $history.Count
        History = $history
        Analysis = $Collector.GetMetricsAnalysis()
    }
    
    $export | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-MonitoringLog "Metrics exported to $OutputPath"
    return $true
}

Export-ModuleMember -Function @('Start-RealtimeMetricsCollection', 'Get-MetricsExport')
Export-ModuleMember -Class 'RealtimeMetricsCollector'
