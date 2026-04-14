<#
.SYNOPSIS
    System health overview and status monitoring
.DESCRIPTION
    Provides comprehensive health checks across all system components
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "Health-Overview"

class HealthMonitor {
    [hashtable]$HealthStatus
    [array]$HealthHistory
    
    HealthMonitor() {
        $this.HealthStatus = @{}
        $this.HealthHistory = @()
    }
    
    [hashtable]GetSystemHealth() {
        $health = @{
            Timestamp = Get-Date -AsUTC
            OverallStatus = "Healthy"
            Components = @{}
        }
        
        try {
            # Check OS Health
            $osHealth = $this.CheckOSHealth()
            $health.Components.OS = $osHealth
            
            # Check Disk Health
            $diskHealth = $this.CheckDiskHealth()
            $health.Components.Disk = $diskHealth
            
            # Check Services Health
            $servicesHealth = $this.CheckServicesHealth()
            $health.Components.Services = $servicesHealth
            
            # Check Network Health
            $networkHealth = $this.CheckNetworkHealth()
            $health.Components.Network = $networkHealth
            
            # Check Security Health
            $securityHealth = $this.CheckSecurityHealth()
            $health.Components.Security = $securityHealth
            
            # Determine overall status
            $statuses = $health.Components.Values | Select-Object -ExpandProperty Status
            if ($statuses -contains "Critical") {
                $health.OverallStatus = "Critical"
            }
            elseif ($statuses -contains "Warning") {
                $health.OverallStatus = "Warning"
            }
            else {
                $health.OverallStatus = "Healthy"
            }
            
            $this.HealthStatus = $health
            return $health
        }
        catch {
            Write-MonitoringLog "Error getting system health: $_" -Level "ERROR"
            return $health
        }
    }
    
    [hashtable]CheckOSHealth() {
        $osHealth = @{
            Status = "Healthy"
            Details = @{}
        }
        
        try {
            $os = Get-WmiObject Win32_OperatingSystem -ErrorAction SilentlyContinue
            
            # Check if Windows Update is pending
            $updatePending = Test-Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired" -ErrorAction SilentlyContinue
            
            $osHealth.Details = @{
                OSVersion = $os.Caption
                Build = $os.BuildNumber
                LastBootTime = $os.LastBootUpTime
                UpdatePending = $updatePending
                SystemDrive = $os.SystemDrive
            }
            
            if ($updatePending) {
                $osHealth.Status = "Warning"
            }
        }
        catch {
            Write-MonitoringLog "Error checking OS health: $_" -Level "DEBUG"
            $osHealth.Status = "Unknown"
        }
        
        return $osHealth
    }
    
    [hashtable]CheckDiskHealth() {
        $diskHealth = @{
            Status = "Healthy"
            Volumes = @()
        }
        
        try {
            Get-Volume -ErrorAction SilentlyContinue | ForEach-Object {
                if ($_.SizeRemaining -gt 0) {
                    $percentUsed = (($_.Size - $_.SizeRemaining) / $_.Size) * 100
                    $volumeStatus = "Healthy"
                    
                    if ($percentUsed -gt 95) {
                        $volumeStatus = "Critical"
                        $diskHealth.Status = "Critical"
                    }
                    elseif ($percentUsed -gt 85) {
                        $volumeStatus = "Warning"
                        if ($diskHealth.Status -ne "Critical") {
                            $diskHealth.Status = "Warning"
                        }
                    }
                    
                    $diskHealth.Volumes += @{
                        Drive = $_.DriveLetter
                        Status = $volumeStatus
                        PercentUsed = [Math]::Round($percentUsed, 2)
                        SizeGB = [Math]::Round($_.Size / 1GB, 2)
                        FreeGB = [Math]::Round($_.SizeRemaining / 1GB, 2)
                    }
                }
            }
        }
        catch {
            Write-MonitoringLog "Error checking disk health: $_" -Level "DEBUG"
            $diskHealth.Status = "Unknown"
        }
        
        return $diskHealth
    }
    
    [hashtable]CheckServicesHealth() {
        $servicesHealth = @{
            Status = "Healthy"
            CriticalServices = @()
            StoppedServices = @()
        }
        
        try {
            $criticalServices = @("WinRM", "EventLog", "Winlogon", "LanmanServer", "Dnscache", "RpcSs", "LSM")
            
            foreach ($serviceName in $criticalServices) {
                $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                if ($svc) {
                    if ($svc.Status -ne "Running") {
                        $servicesHealth.StoppedServices += @{
                            Name = $svc.DisplayName
                            Status = $svc.Status.ToString()
                        }
                        $servicesHealth.Status = "Critical"
                    }
                    else {
                        $servicesHealth.CriticalServices += @{
                            Name = $svc.DisplayName
                            Status = "Running"
                        }
                    }
                }
            }
        }
        catch {
            Write-MonitoringLog "Error checking services health: $_" -Level "DEBUG"
            $servicesHealth.Status = "Unknown"
        }
        
        return $servicesHealth
    }
    
    [hashtable]CheckNetworkHealth() {
        $networkHealth = @{
            Status = "Healthy"
            Adapters = @()
        }
        
        try {
            $adapters = Get-NetAdapter -ErrorAction SilentlyContinue
            
            foreach ($adapter in $adapters) {
                $ipConfig = Get-NetIPConfiguration -InterfaceAlias $adapter.Name -ErrorAction SilentlyContinue
                $networkHealth.Adapters += @{
                    Name = $adapter.Name
                    Status = $adapter.Status
                    Speed = $adapter.Speed
                    MediaType = $adapter.MediaType
                    MACAddress = $adapter.MacAddress
                    IPAddresses = @($ipConfig.IPv4Address.IPAddress -join ", ")
                }
            }
            
            if ($networkHealth.Adapters.Count -eq 0 -or -not ($networkHealth.Adapters | Where-Object { $_.Status -eq "Up" })) {
                $networkHealth.Status = "Critical"
            }
        }
        catch {
            Write-MonitoringLog "Error checking network health: $_" -Level "DEBUG"
            $networkHealth.Status = "Unknown"
        }
        
        return $networkHealth
    }
    
    [hashtable]CheckSecurityHealth() {
        $securityHealth = @{
            Status = "Healthy"
            Details = @{}
        }
        
        try {
            # Check Windows Defender
            $defenderKey = "HKLM:\Software\Microsoft\Windows Defender"
            $defenderEnabled = (Get-ItemProperty -Path $defenderKey -Name DisableAntiSpyware -ErrorAction SilentlyContinue).DisableAntiSpyware -eq 0
            
            # Check Firewall
            $firewallProfiles = Get-NetFirewallProfile -ErrorAction SilentlyContinue
            $firewallEnabled = $firewallProfiles | Where-Object { $_.Enabled -eq $true } | Measure-Object | Select-Object -ExpandProperty Count
            
            # Check UAC
            $uacEnabled = (Get-ItemProperty -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Policies\System" -Name ConsentPromptBehaviorAdmin -ErrorAction SilentlyContinue).ConsentPromptBehaviorAdmin -eq 1
            
            $securityHealth.Details = @{
                WindowsDefenderEnabled = $defenderEnabled
                FirewallEnabled = $firewallEnabled -gt 0
                UACEnabled = $uacEnabled
            }
            
            if (-not $defenderEnabled -or $firewallEnabled -eq 0 -or -not $uacEnabled) {
                $securityHealth.Status = "Warning"
            }
        }
        catch {
            Write-MonitoringLog "Error checking security health: $_" -Level "DEBUG"
            $securityHealth.Status = "Unknown"
        }
        
        return $securityHealth
    }
    
    [void]AddHealthSnapshot() {
        $this.HealthHistory += $this.HealthStatus
        
        if ($this.HealthHistory.Count -gt 100) {
            $this.HealthHistory = $this.HealthHistory[-100..-1]
        }
    }
    
    [array]GetHealthTrend([int]$LastRecords = 24) {
        return $this.HealthHistory[-$LastRecords..-1]
    }
}

function Start-HealthMonitoring {
    param(
        [int]$CheckIntervalSeconds = 300
    )
    
    Write-MonitoringLog "Starting health monitoring..."
    
    $healthMonitor = [HealthMonitor]::new()
    
    while ($true) {
        try {
            $health = $healthMonitor.GetSystemHealth()
            $healthMonitor.AddHealthSnapshot()
            
            DisplayHealthStatus -Health $health
            
            Start-Sleep -Seconds $CheckIntervalSeconds
        }
        catch {
            Write-MonitoringLog "Health monitoring error: $_" -Level "ERROR"
            Start-Sleep -Seconds $CheckIntervalSeconds
        }
    }
}

function DisplayHealthStatus {
    param([hashtable]$Health)
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  SYSTEM HEALTH OVERVIEW                                       ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    $statusColor = if ($Health.OverallStatus -eq "Healthy") { "Green" } 
                  elseif ($Health.OverallStatus -eq "Warning") { "Yellow" }
                  else { "Red" }
    
    Write-Host "OVERALL STATUS: $($Health.OverallStatus)" -ForegroundColor $statusColor
    Write-Host ""
    
    # OS Health
    $os = $Health.Components.OS
    Write-Host "OPERATING SYSTEM" -ForegroundColor Yellow
    Write-Host "  Status: $($os.Status)"
    Write-Host "  Version: $($os.Details.OSVersion)"
    Write-Host "  Build: $($os.Details.Build)"
    Write-Host ""
    
    # Disk Health
    $disk = $Health.Components.Disk
    Write-Host "DISK STATUS" -ForegroundColor Yellow
    foreach ($vol in $disk.Volumes) {
        $volColor = if ($vol.Status -eq "Critical") { "Red" } elseif ($vol.Status -eq "Warning") { "Yellow" } else { "Green" }
        Write-Host "  $($vol.Drive):: $($vol.Status) - $($vol.PercentUsed)% used ($($vol.FreeGB)GB free)" -ForegroundColor $volColor
    }
    Write-Host ""
    
    # Services Health
    $services = $Health.Components.Services
    Write-Host "SERVICES" -ForegroundColor Yellow
    if ($services.StoppedServices.Count -gt 0) {
        Write-Host "  STOPPED:" -ForegroundColor Red
        $services.StoppedServices | ForEach-Object {
            Write-Host "    ⚠ $($_.Name)" -ForegroundColor Red
        }
    }
    Write-Host "  ✓ $($services.CriticalServices.Count) critical services running" -ForegroundColor Green
    Write-Host ""
    
    # Network Health
    $network = $Health.Components.Network
    Write-Host "NETWORK" -ForegroundColor Yellow
    $network.Adapters | Where-Object { $_.Status -eq "Up" } | ForEach-Object {
        Write-Host "  ✓ $($_.Name) - Up" -ForegroundColor Green
    }
    Write-Host ""
    
    # Security
    $security = $Health.Components.Security
    Write-Host "SECURITY" -ForegroundColor Yellow
    Write-Host "  Defender: $(if ($security.Details.WindowsDefenderEnabled) { "✓ Enabled" } else { "✗ Disabled" })"
    Write-Host "  Firewall: $(if ($security.Details.FirewallEnabled) { "✓ Enabled" } else { "✗ Disabled" })"
    Write-Host "  UAC: $(if ($security.Details.UACEnabled) { "✓ Enabled" } else { "✗ Disabled" })"
    Write-Host ""
    
    Write-Host "Last Check: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
}

Export-ModuleMember -Function @('Start-HealthMonitoring', 'DisplayHealthStatus')
Export-ModuleMember -Class 'HealthMonitor'
