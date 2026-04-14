<#
.SYNOPSIS
    Resource tracking and performance analytics for on-premises
.DESCRIPTION
    CPU, memory, disk, and network resource monitoring
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "Resource-Tracking"

class ResourceTracker {
    [array]$PerformanceCounters
    [hashtable]$CurrentMetrics
    [array]$MetricsHistory
    
    ResourceTracker() {
        $this.PerformanceCounters = @()
        $this.CurrentMetrics = @{}
        $this.MetricsHistory = @()
        $this.InitializeCounters()
    }
    
    [void]InitializeCounters() {
        try {
            # Initialize performance counters
            $counterNames = @(
                "\Processor(_Total)\% Processor Time"
                "\Memory\% Committed Bytes In Use"
                "\Disk(_Total)\% Disk Time"
                "\Network Interface(*)\Bytes Received/sec"
                "\Network Interface(*)\Bytes Sent/sec"
            )
            
            foreach ($counter in $counterNames) {
                try {
                    $this.PerformanceCounters += Get-Counter -Counter $counter -ErrorAction SilentlyContinue
                }
                catch {
                    Write-MonitoringLog "Could not initialize counter: $counter" -Level "DEBUG"
                }
            }
        }
        catch {
            Write-MonitoringLog "Error initializing counters: $_" -Level "ERROR"
        }
    }
    
    [hashtable]GetCPUMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            Processors = @()
            AverageCPU = 0
        }
        
        try {
            $cpuCounter = Get-Counter -Counter "\Processor(_Total)\% Processor Time" -ErrorAction SilentlyContinue
            $metrics.AverageCPU = [Math]::Round($cpuCounter.CounterSamples[0].CookedValue, 2)
            
            # Per-processor metrics
            $cpuProcessors = Get-WmiObject Win32_Processor -ErrorAction SilentlyContinue
            $metrics.Processors = @($cpuProcessors | ForEach-Object {
                @{
                    Name = $_.Name
                    Cores = $_.NumberOfCores
                    LoadPercentage = $_.LoadPercentage
                }
            })
        }
        catch {
            Write-MonitoringLog "Error getting CPU metrics: $_" -Level "DEBUG"
        }
        
        return $metrics
    }
    
    [hashtable]GetMemoryMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
        }
        
        try {
            $os = Get-WmiObject Win32_OperatingSystem -ErrorAction SilentlyContinue
            if ($os) {
                $totalMem = $os.TotalVisibleMemorySize
                $freeMem = $os.FreePhysicalMemory
                $usedMem = $totalMem - $freeMem
                
                $metrics.TotalMB = [Math]::Round($totalMem / 1024, 2)
                $metrics.UsedMB = [Math]::Round($usedMem / 1024, 2)
                $metrics.FreeMB = [Math]::Round($freeMem / 1024, 2)
                $metrics.UsedPercent = [Math]::Round(($usedMem / $totalMem) * 100, 2)
                
                # Page file
                $pageFile = Get-WmiObject Win32_PageFile -ErrorAction SilentlyContinue
                if ($pageFile) {
                    $metrics.PageFileMB = [Math]::Round($pageFile.CurrentUsage, 2)
                }
            }
        }
        catch {
            Write-MonitoringLog "Error getting memory metrics: $_" -Level "DEBUG"
        }
        
        return $metrics
    }
    
    [hashtable]GetNetworkMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            Adapters = @()
        }
        
        try {
            $adapters = Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Up" }
            
            foreach ($adapter in $adapters) {
                $stats = Get-NetAdapterStatistics -Name $adapter.Name -ErrorAction SilentlyContinue
                if ($stats) {
                    $metrics.Adapters += @{
                        Name = $adapter.Name
                        Status = $adapter.Status
                        Speed = $adapter.Speed
                        BytesReceived = $stats.ReceivedBytes
                        BytesSent = $stats.SentBytes
                        PacketsReceived = $stats.ReceivedUnicastPackets
                        PacketsSent = $stats.SentUnicastPackets
                        DroppedPacketsIn = $stats.IncomingPacketsDiscarded
                        DroppedPacketsOut = $stats.OutgoingPacketsDiscarded
                    }
                }
            }
        }
        catch {
            Write-MonitoringLog "Error getting network metrics: $_" -Level "DEBUG"
        }
        
        return $metrics
    }
    
    [hashtable]GetDiskIOMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            Drives = @()
        }
        
        try {
            $logicalDisks = Get-WmiObject Win32_LogicalDisk -ErrorAction SilentlyContinue | Where-Object { $_.DriveType -eq 3 }
            
            foreach ($disk in $logicalDisks) {
                $usedSpace = $disk.Size - $disk.FreeSpace
                $usedPercent = ($usedSpace / $disk.Size) * 100
                
                $metrics.Drives += @{
                    Drive = $disk.DeviceID
                    VolumeName = $disk.VolumeName
                    SizeGB = [Math]::Round($disk.Size / 1GB, 2)
                    UsedGB = [Math]::Round($usedSpace / 1GB, 2)
                    FreeGB = [Math]::Round($disk.FreeSpace / 1GB, 2)
                    UsedPercent = [Math]::Round($usedPercent, 2)
                }
            }
        }
        catch {
            Write-MonitoringLog "Error getting disk IO metrics: $_" -Level "DEBUG"
        }
        
        return $metrics
    }
    
    [hashtable]GetProcessMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            TopByMemory = @()
            TopByCPU = @()
        }
        
        try {
            $processes = Get-Process -ErrorAction SilentlyContinue
            
            # Top 10 by memory
            $metrics.TopByMemory = @($processes | 
                Sort-Object -Property WorkingSet -Descending | 
                Select-Object -First 10 | 
                ForEach-Object {
                    @{
                        Name = $_.ProcessName
                        PID = $_.Id
                        MemoryMB = [Math]::Round($_.WorkingSet / 1MB, 2)
                        HandleCount = $_.Handles
                    }
                })
            
            # Top 10 by CPU
            $metrics.TopByCPU = @($processes | 
                Where-Object { $_.CPU -gt 0 } |
                Sort-Object -Property CPU -Descending | 
                Select-Object -First 10 | 
                ForEach-Object {
                    @{
                        Name = $_.ProcessName
                        PID = $_.Id
                        CPU = [Math]::Round($_.CPU, 2)
                        MemoryMB = [Math]::Round($_.WorkingSet / 1MB, 2)
                    }
                })
        }
        catch {
            Write-MonitoringLog "Error getting process metrics: $_" -Level "DEBUG"
        }
        
        return $metrics
    }
    
    [hashtable]CaptureAllMetrics() {
        $allMetrics = @{
            Timestamp = Get-Date -AsUTC
            CPU = $this.GetCPUMetrics()
            Memory = $this.GetMemoryMetrics()
            Network = $this.GetNetworkMetrics()
            Disk = $this.GetDiskIOMetrics()
            Processes = $this.GetProcessMetrics()
        }
        
        $this.CurrentMetrics = $allMetrics
        $this.MetricsHistory += $allMetrics
        
        if ($this.MetricsHistory.Count -gt 288) {  # 24 hours at 5-minute intervals
            $this.MetricsHistory = $this.MetricsHistory[-288..-1]
        }
        
        return $allMetrics
    }
}

function Start-ResourceTracking {
    param(
        [int]$IntervalSeconds = 300
    )
    
    Write-MonitoringLog "Starting resource tracking..."
    
    $tracker = [ResourceTracker]::new()
    
    while ($true) {
        try {
            $metrics = $tracker.CaptureAllMetrics()
            DisplayResourceMetrics -Metrics $metrics
            
            Start-Sleep -Seconds $IntervalSeconds
        }
        catch {
            Write-MonitoringLog "Resource tracking error: $_" -Level "ERROR"
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
}

function DisplayResourceMetrics {
    param([hashtable]$Metrics)
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  RESOURCE TRACKING & PERFORMANCE ANALYTICS                   ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    # CPU
    Write-Host "CPU PERFORMANCE" -ForegroundColor Yellow
    Write-Host "  Average: $($Metrics.CPU.AverageCPU)%"
    $Metrics.CPU.Processors | ForEach-Object {
        Write-Host "    $($_.Name): $($_.LoadPercentage)%"
    }
    Write-Host ""
    
    # Memory
    Write-Host "MEMORY USAGE" -ForegroundColor Yellow
    Write-Host "  Used: $($Metrics.Memory.UsedMB)MB / Total: $($Metrics.Memory.TotalMB)MB ($($Metrics.Memory.UsedPercent)%)"
    Write-Host "  Free: $($Metrics.Memory.FreeMB)MB"
    Write-Host ""
    
    # Network
    Write-Host "NETWORK ADAPTERS" -ForegroundColor Yellow
    $Metrics.Network.Adapters | ForEach-Object {
        Write-Host "  $($_.Name): $([Math]::Round($_.BytesReceived/1MB, 2))MB Rx | $([Math]::Round($_.BytesSent/1MB, 2))MB Tx"
    }
    Write-Host ""
    
    # Disk
    Write-Host "DISK USAGE" -ForegroundColor Yellow
    $Metrics.Disk.Drives | ForEach-Object {
        $color = if ($_.UsedPercent -gt 90) { "Red" } elseif ($_.UsedPercent -gt 75) { "Yellow" } else { "Green" }
        Write-Host "  $($_.Drive): $($_.UsedPercent)% ($($_.FreeGB)GB free)" -ForegroundColor $color
    }
    Write-Host ""
    
    # Top Processes
    Write-Host "TOP PROCESSES (Memory)" -ForegroundColor Yellow
    $Metrics.Processes.TopByMemory | Select-Object -First 5 | ForEach-Object {
        Write-Host "  $($_.Name): $($_.MemoryMB)MB"
    }
    Write-Host ""
    
    Write-Host "Last Updated: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
}

Export-ModuleMember -Function @('Start-ResourceTracking', 'DisplayResourceMetrics')
Export-ModuleMember -Class 'ResourceTracker'
