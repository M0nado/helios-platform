<#
.SYNOPSIS
    On-premise system health monitoring
.DESCRIPTION
    Windows system monitoring for on-premises infrastructure
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "On-Prem-Health"

class OnPremSystemMonitor {
    [hashtable]$SystemMetrics
    [array]$HealthHistory
    [hashtable]$Thresholds
    
    OnPremSystemMonitor() {
        $this.SystemMetrics = @{}
        $this.HealthHistory = @()
        
        $config = Get-MonitoringConfig
        $this.Thresholds = @{
            CPUThreshold = $config.monitoring.onPrem.cpuThreshold
            MemoryThreshold = $config.monitoring.onPrem.memoryThreshold
            DiskThreshold = $config.monitoring.onPrem.diskThreshold
        }
    }
    
    [hashtable]GetSystemMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            ComputerName = [System.Environment]::MachineName
            OSInfo = $this.GetOSInfo()
            Hardware = $this.GetHardwareInfo()
            Performance = $this.GetPerformanceMetrics()
            Storage = $this.GetStorageMetrics()
            EventLogs = $this.GetEventLogMetrics()
        }
        
        $this.SystemMetrics = $metrics
        $this.HealthHistory += $metrics
        
        if ($this.HealthHistory.Count -gt 100) {
            $this.HealthHistory = $this.HealthHistory[-100..-1]
        }
        
        return $metrics
    }
    
    [hashtable]GetOSInfo() {
        $os = Get-WmiObject Win32_OperatingSystem -ErrorAction SilentlyContinue
        
        if (-not $os) {
            return @{ Error = "Unable to retrieve OS information" }
        }
        
        return @{
            Caption = $os.Caption
            Version = $os.Version
            BuildNumber = $os.BuildNumber
            InstallDate = $os.ConvertToDateTime($os.InstallDate)
            LastBootTime = $os.ConvertToDateTime($os.LastBootUpTime)
            TotalVisibleMemoryMB = [Math]::Round($os.TotalVisibleMemorySize / 1024, 2)
            FreePhysicalMemoryMB = [Math]::Round($os.FreePhysicalMemory / 1024, 2)
            UpTimeDays = ([datetime]::Now - $os.ConvertToDateTime($os.LastBootUpTime)).Days
        }
    }
    
    [hashtable]GetHardwareInfo() {
        $cpu = Get-WmiObject Win32_Processor -ErrorAction SilentlyContinue | Select-Object -First 1
        $cs = Get-WmiObject Win32_ComputerSystem -ErrorAction SilentlyContinue
        
        if (-not $cpu -or -not $cs) {
            return @{ Error = "Unable to retrieve hardware information" }
        }
        
        return @{
            ProcessorName = $cpu.Name
            ProcessorCores = $cpu.NumberOfCores
            ProcessorLogical = $cpu.NumberOfLogicalProcessors
            ProcessorSpeed = $cpu.MaxClockSpeed
            Manufacturer = $cs.Manufacturer
            Model = $cs.Model
            Chassis = (Get-WmiObject Win32_SystemEnclosure -ErrorAction SilentlyContinue).ChassisTypes[0]
        }
    }
    
    [hashtable]GetPerformanceMetrics() {
        $perfMetrics = @{
            CPU = $null
            Memory = @{}
            Disk = @{}
        }
        
        try {
            $cpuCounter = Get-Counter -Counter "\Processor(_Total)\% Processor Time" -ErrorAction SilentlyContinue
            if ($cpuCounter) {
                $perfMetrics.CPU = [Math]::Round($cpuCounter.CounterSamples[0].CookedValue, 2)
            }
        }
        catch {
            Write-MonitoringLog "Error getting CPU counter: $_" -Level "DEBUG"
            $perfMetrics.CPU = -1
        }
        
        $os = Get-WmiObject Win32_OperatingSystem -ErrorAction SilentlyContinue
        if ($os) {
            $memUsed = $os.TotalVisibleMemorySize - $os.FreePhysicalMemory
            $memPercent = ($memUsed / $os.TotalVisibleMemorySize) * 100
            
            $perfMetrics.Memory = @{
                PercentUsed = [Math]::Round($memPercent, 2)
                UsedMB = [Math]::Round($memUsed / 1024, 2)
                TotalMB = [Math]::Round($os.TotalVisibleMemorySize / 1024, 2)
            }
        }
        
        try {
            $pageFile = Get-WmiObject Win32_PageFile -ErrorAction SilentlyContinue
            if ($pageFile) {
                $perfMetrics.Memory.PageFileUsage = $pageFile.CurrentUsage
            }
        }
        catch {
            Write-MonitoringLog "Error getting page file info: $_" -Level "DEBUG"
        }
        
        return $perfMetrics
    }
    
    [hashtable]GetStorageMetrics() {
        $storage = @{
            Volumes = @()
            TotalCapacityGB = 0
            TotalUsedGB = 0
            TotalFreeGB = 0
        }
        
        try {
            Get-Volume -ErrorAction SilentlyContinue | Where-Object { $_.SizeRemaining -gt 0 } | ForEach-Object {
                $percentUsed = (($_.Size - $_.SizeRemaining) / $_.Size) * 100
                $sizeGB = [Math]::Round($_.Size / 1GB, 2)
                $usedGB = [Math]::Round(($_.Size - $_.SizeRemaining) / 1GB, 2)
                $freeGB = [Math]::Round($_.SizeRemaining / 1GB, 2)
                
                $storage.Volumes += @{
                    DriveLetter = $_.DriveLetter
                    FileSystemLabel = $_.FileSystemLabel
                    FileSystem = $_.FileSystem
                    SizeGB = $sizeGB
                    UsedGB = $usedGB
                    FreeGB = $freeGB
                    PercentUsed = [Math]::Round($percentUsed, 2)
                    Status = if ($percentUsed -gt $this.Thresholds.DiskThreshold) { "Alert" } else { "OK" }
                }
                
                $storage.TotalCapacityGB += $sizeGB
                $storage.TotalUsedGB += $usedGB
                $storage.TotalFreeGB += $freeGB
            }
        }
        catch {
            Write-MonitoringLog "Error getting volume info: $_" -Level "DEBUG"
        }
        
        return $storage
    }
    
    [hashtable]GetEventLogMetrics() {
        $eventMetrics = @{
            Security = @{}
            System = @{}
            Application = @{}
        }
        
        foreach ($logName in @("Security", "System", "Application")) {
            try {
                $log = Get-WinEvent -LogName $logName -MaxEvents 1000 -ErrorAction SilentlyContinue
                if ($log) {
                    $errors = @($log | Where-Object { $_.LevelDisplayName -eq "Error" })
                    $warnings = @($log | Where-Object { $_.LevelDisplayName -eq "Warning" })
                    
                    $eventMetrics[$logName] = @{
                        TotalEvents = $log.Count
                        ErrorCount = $errors.Count
                        WarningCount = $warnings.Count
                        LatestEvent = $log[0].TimeCreated
                    }
                }
            }
            catch {
                Write-MonitoringLog "Error reading $logName log: $_" -Level "DEBUG"
            }
        }
        
        return $eventMetrics
    }
    
    [array]GenerateAlerts() {
        $alerts = @()
        $perf = $this.SystemMetrics.Performance
        $storage = $this.SystemMetrics.Storage
        
        if ($perf.CPU -gt $this.Thresholds.CPUThreshold) {
            $alerts += @{
                Severity = "High"
                Title = "High CPU Usage"
                Message = "CPU usage is $($perf.CPU)%"
            }
        }
        
        if ($perf.Memory.PercentUsed -gt $this.Thresholds.MemoryThreshold) {
            $alerts += @{
                Severity = "High"
                Title = "High Memory Usage"
                Message = "Memory usage is $($perf.Memory.PercentUsed)%"
            }
        }
        
        foreach ($vol in $storage.Volumes) {
            if ($vol.PercentUsed -gt $this.Thresholds.DiskThreshold) {
                $alerts += @{
                    Severity = if ($vol.PercentUsed -gt 95) { "Critical" } else { "High" }
                    Title = "Disk Space Alert - $($vol.DriveLetter)"
                    Message = "Disk usage is $($vol.PercentUsed)%"
                }
            }
        }
        
        return $alerts
    }
}

function Start-OnPremMonitoring {
    param(
        [int]$IntervalSeconds = 300
    )
    
    Write-MonitoringLog "Starting on-premises system monitoring..."
    
    $monitor = [OnPremSystemMonitor]::new()
    
    while ($true) {
        try {
            $metrics = $monitor.GetSystemMetrics()
            $alerts = $monitor.GenerateAlerts()
            
            DisplayOnPremMetrics -Metrics $metrics -Alerts $alerts
            
            Start-Sleep -Seconds $IntervalSeconds
        }
        catch {
            Write-MonitoringLog "On-premises monitoring error: $_" -Level "ERROR"
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
}

function DisplayOnPremMetrics {
    param(
        [hashtable]$Metrics,
        [array]$Alerts
    )
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  ON-PREMISES SYSTEM MONITORING                               ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "SYSTEM INFORMATION" -ForegroundColor Yellow
    Write-Host "  Computer: $($Metrics.ComputerName)"
    Write-Host "  OS: $($Metrics.OSInfo.Caption) Build $($Metrics.OSInfo.BuildNumber)"
    Write-Host "  Uptime: $($Metrics.OSInfo.UpTimeDays) days"
    Write-Host ""
    
    Write-Host "HARDWARE" -ForegroundColor Yellow
    Write-Host "  Processor: $($Metrics.Hardware.ProcessorName)"
    Write-Host "  Cores: $($Metrics.Hardware.ProcessorCores) | Logical: $($Metrics.Hardware.ProcessorLogical)"
    Write-Host ""
    
    Write-Host "PERFORMANCE" -ForegroundColor Yellow
    $cpuColor = if ($Metrics.Performance.CPU -gt 80) { "Red" } elseif ($Metrics.Performance.CPU -gt 60) { "Yellow" } else { "Green" }
    Write-Host "  CPU: $($Metrics.Performance.CPU)%" -ForegroundColor $cpuColor
    
    $memColor = if ($Metrics.Performance.Memory.PercentUsed -gt 85) { "Red" } elseif ($Metrics.Performance.Memory.PercentUsed -gt 70) { "Yellow" } else { "Green" }
    Write-Host "  Memory: $($Metrics.Performance.Memory.PercentUsed)% ($($Metrics.Performance.Memory.UsedMB)MB/$($Metrics.Performance.Memory.TotalMB)MB)" -ForegroundColor $memColor
    Write-Host ""
    
    Write-Host "STORAGE" -ForegroundColor Yellow
    foreach ($vol in $Metrics.Storage.Volumes) {
        $volColor = if ($vol.PercentUsed -gt 90) { "Red" } elseif ($vol.PercentUsed -gt 75) { "Yellow" } else { "Green" }
        Write-Host "  $($vol.DriveLetter):: $($vol.PercentUsed)% ($($vol.FreeGB)GB free)" -ForegroundColor $volColor
    }
    Write-Host ""
    
    if ($Alerts.Count -gt 0) {
        Write-Host "ALERTS" -ForegroundColor Red
        $Alerts | ForEach-Object {
            $alertColor = if ($_.Severity -eq "Critical") { "Red" } else { "Yellow" }
            Write-Host "  [$($_.Severity)] $($_.Title)" -ForegroundColor $alertColor
        }
    }
    
    Write-Host ""
    Write-Host "Last Updated: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
}

Export-ModuleMember -Function @('Start-OnPremMonitoring', 'DisplayOnPremMetrics')
Export-ModuleMember -Class 'OnPremSystemMonitor'
