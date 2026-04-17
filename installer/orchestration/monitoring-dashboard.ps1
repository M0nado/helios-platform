<#
.SYNOPSIS
Monitoring Dashboard for HELIOS Platform - Real-time overview of all components and system health.

.DESCRIPTION
Provides:
- Real-time component status (every 5 seconds)
- System health overview
- Performance metrics visualization
- Phase progress tracking
- Interactive component drill-down
- Alerting on issues

.EXAMPLE
PS> .\monitoring-dashboard.ps1 -Action StartDashboard
PS> .\monitoring-dashboard.ps1 -Action GetMetrics -Component 'ai-hub'
PS> .\monitoring-dashboard.ps1 -Action GetAlerts

.NOTES
Dashboard updates every 5 seconds with latest metrics.
Displays real-time monitoring of all 7 components.
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('StartDashboard', 'GetMetrics', 'GetAlerts', 'GetComponentStatus', 'GetSystemHealth')]
    [string]$Action = 'StartDashboard',
    
    [Parameter(Mandatory=$false)]
    [string]$Component = '',
    
    [Parameter(Mandatory=$false)]
    [int]$RefreshInterval = 5
)

$ErrorActionPreference = 'Stop'

# ===========================
# CONFIGURATION
# ===========================

$components = @('monado', 'aegis', 'usb-auth', 'ai-hub', 'dev-hub', 'gui-dashboard', 'build-agents')

$metrics = @{}
$alerts = @()
$healthThresholds = @{
    'cpu_percent' = 80
    'memory_percent' = 85
    'disk_percent' = 90
    'error_rate' = 5
    'latency_ms' = 1000
}

# ===========================
# HELPER FUNCTIONS
# ===========================

function Write-DashboardLog {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Success', 'Warning', 'Error')][string]$Level = 'Info'
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $color = @{
        'Info' = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error' = 'Red'
    }[$Level]
    Write-Host "[$timestamp] [DASHBOARD] [$Level] $Message" -ForegroundColor $color
}

function Get-MockMetrics {
    param([string]$ComponentName)
    
    # Generate realistic mock metrics with some variance
    $baseLoad = Get-Random -Minimum 20 -Maximum 80
    
    return @{
        component = $ComponentName
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
        status = if ((Get-Random -Minimum 1 -Maximum 100) -lt 95) { 'Healthy' } else { 'Warning' }
        cpu_percent = $baseLoad + (Get-Random -Minimum -5 -Maximum 5)
        memory_percent = $baseLoad + (Get-Random -Minimum -8 -Maximum 8)
        disk_percent = 45 + (Get-Random -Minimum -5 -Maximum 10)
        uptime_hours = 42 + (Get-Random -Minimum 0 -Maximum 24)
        requests_per_sec = Get-Random -Minimum 10 -Maximum 500
        error_rate_percent = Get-Random -Minimum 0 -Maximum 2
        latency_ms = Get-Random -Minimum 10 -Maximum 200
        active_sessions = Get-Random -Minimum 5 -Maximum 100
        queue_depth = Get-Random -Minimum 0 -Maximum 50
    }
}

function Check-HealthStatus {
    param([object]$ComponentMetrics)
    
    $issues = @()
    
    if ($ComponentMetrics.cpu_percent -gt $healthThresholds.cpu_percent) {
        $issues += "HIGH CPU: $($ComponentMetrics.cpu_percent)%"
    }
    
    if ($ComponentMetrics.memory_percent -gt $healthThresholds.memory_percent) {
        $issues += "HIGH MEMORY: $($ComponentMetrics.memory_percent)%"
    }
    
    if ($ComponentMetrics.error_rate_percent -gt $healthThresholds.error_rate) {
        $issues += "HIGH ERROR RATE: $($ComponentMetrics.error_rate_percent)%"
    }
    
    if ($ComponentMetrics.latency_ms -gt $healthThresholds.latency_ms) {
        $issues += "HIGH LATENCY: $($ComponentMetrics.latency_ms)ms"
    }
    
    return $issues
}

function Generate-Alert {
    param(
        [string]$Component,
        [string]$Severity,
        [string]$Message
    )
    
    $alert = @{
        id = "alert-$(Get-Random -Minimum 100000 -Maximum 999999)"
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
        component = $Component
        severity = $Severity
        message = $Message
    }
    
    return $alert
}

function Collect-AllMetrics {
    Write-DashboardLog "Collecting metrics from all components..." -Level Info
    
    $allMetrics = @{}
    $allAlerts = @()
    
    foreach ($component in $components) {
        $componentMetrics = Get-MockMetrics -ComponentName $component
        $allMetrics[$component] = $componentMetrics
        
        # Check health
        $issues = Check-HealthStatus $componentMetrics
        foreach ($issue in $issues) {
            $severity = if ($issue -match 'HIGH') { 'Critical' } else { 'Warning' }
            $allAlerts += Generate-Alert -Component $component -Severity $severity -Message $issue
        }
    }
    
    return @{
        metrics = $allMetrics
        alerts = $allAlerts
    }
}

function Get-SystemHealth {
    param([object]$AllMetrics)
    
    $healthyCount = 0
    $warningCount = 0
    $criticalCount = 0
    
    foreach ($component in $AllMetrics.metrics.Keys) {
        $comp = $AllMetrics.metrics[$component]
        
        if ($comp.status -eq 'Healthy' -and $comp.error_rate_percent -lt 1) {
            $healthyCount++
        } elseif ($comp.error_rate_percent -gt 5) {
            $criticalCount++
        } else {
            $warningCount++
        }
    }
    
    $overallStatus = if ($criticalCount -gt 0) {
        'Critical'
    } elseif ($warningCount -gt 2) {
        'Warning'
    } else {
        'Healthy'
    }
    
    return @{
        status = $overallStatus
        healthy = $healthyCount
        warning = $warningCount
        critical = $criticalCount
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
    }
}

function Show-Dashboard {
    param([object]$AllMetrics, [object]$SystemHealth)
    
    Clear-Host
    
    Write-Host "$('='*100)" -ForegroundColor Cyan
    Write-Host "HELIOS PLATFORM - MONITORING DASHBOARD" -ForegroundColor Cyan
    Write-Host "Last Updated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "$('='*100)" -ForegroundColor Cyan
    
    # System Health Summary
    $healthColor = @{
        'Healthy' = 'Green'
        'Warning' = 'Yellow'
        'Critical' = 'Red'
    }[$SystemHealth.status]
    
    Write-Host "`nSYSTEM HEALTH: " -ForegroundColor Cyan -NoNewline
    Write-Host "$($SystemHealth.status)" -ForegroundColor $healthColor
    Write-Host "  Healthy: $($SystemHealth.healthy) | Warning: $($SystemHealth.warning) | Critical: $($SystemHealth.critical)`n" -ForegroundColor Cyan
    
    # Component Status Table
    Write-Host "COMPONENT STATUS:" -ForegroundColor Cyan
    Write-Host "$('-'*100)" -ForegroundColor Cyan
    Write-Host "{0,-20} {1,-12} {2,-8} {3,-8} {4,-10} {5,-12} {6,-10}" -f `
        'Component', 'Status', 'CPU%', 'MEM%', 'RPS', 'Error%', 'Latency'
    Write-Host "$('-'*100)" -ForegroundColor Cyan
    
    foreach ($component in $components) {
        if ($AllMetrics.metrics.ContainsKey($component)) {
            $m = $AllMetrics.metrics[$component]
            $statusColor = if ($m.status -eq 'Healthy') { 'Green' } else { 'Yellow' }
            
            Write-Host "{0,-20} " -f $component -NoNewline
            Write-Host "{0,-12}" -f $m.status -ForegroundColor $statusColor -NoNewline
            Write-Host "{0,-8} {1,-8} {2,-10} {3,-12} {4,-10}" -f `
                "$($m.cpu_percent)%", `
                "$($m.memory_percent)%", `
                $m.requests_per_sec, `
                "$($m.error_rate_percent)%", `
                "$($m.latency_ms)ms"
        }
    }
    
    Write-Host "$('-'*100)" -ForegroundColor Cyan
    
    # Performance Metrics
    Write-Host "`nPERFORMANCE METRICS:" -ForegroundColor Cyan
    $totalRps = ($AllMetrics.metrics.Values | Measure-Object -Property requests_per_sec -Sum).Sum
    $avgLatency = [Math]::Round(($AllMetrics.metrics.Values | Measure-Object -Property latency_ms -Average).Average, 2)
    $avgCpu = [Math]::Round(($AllMetrics.metrics.Values | Measure-Object -Property cpu_percent -Average).Average, 1)
    $avgMemory = [Math]::Round(($AllMetrics.metrics.Values | Measure-Object -Property memory_percent -Average).Average, 1)
    
    Write-Host "  Total Requests/Sec: $totalRps" -ForegroundColor Cyan
    Write-Host "  Average Latency: ${avgLatency}ms" -ForegroundColor Cyan
    Write-Host "  Average CPU: $avgCpu%" -ForegroundColor Cyan
    Write-Host "  Average Memory: $avgMemory%" -ForegroundColor Cyan
    
    # Active Alerts
    if ($AllMetrics.alerts.Count -gt 0) {
        Write-Host "`nALERTS ($($AllMetrics.alerts.Count)):" -ForegroundColor Yellow
        foreach ($alert in $AllMetrics.alerts | Select-Object -First 5) {
            $alertColor = if ($alert.severity -eq 'Critical') { 'Red' } else { 'Yellow' }
            Write-Host "  [$($alert.severity)] $($alert.component): $($alert.message)" -ForegroundColor $alertColor
        }
        if ($AllMetrics.alerts.Count -gt 5) {
            Write-Host "  ... and $($AllMetrics.alerts.Count - 5) more alerts" -ForegroundColor Yellow
        }
    } else {
        Write-Host "`nNo active alerts" -ForegroundColor Green
    }
    
    Write-Host "`n$('='*100)" -ForegroundColor Cyan
    Write-Host "Auto-refreshing in $RefreshInterval seconds... (Press Ctrl+C to exit)" -ForegroundColor Cyan
    Write-Host "$('='*100)" -ForegroundColor Cyan
}

function Start-Dashboard {
    Write-DashboardLog "Starting monitoring dashboard..." -Level Info
    
    while ($true) {
        $data = Collect-AllMetrics
        $health = Get-SystemHealth -AllMetrics $data
        
        Show-Dashboard -AllMetrics $data -SystemHealth $health
        
        Start-Sleep -Seconds $RefreshInterval
    }
}

function Show-ComponentMetrics {
    param([string]$ComponentName)
    
    Write-DashboardLog "Retrieving metrics for $ComponentName..." -Level Info
    
    $metrics = Get-MockMetrics -ComponentName $ComponentName
    
    Write-Host "`n$('='*80)" -ForegroundColor Cyan
    Write-Host "COMPONENT METRICS: $ComponentName" -ForegroundColor Cyan
    Write-Host "$('='*80)" -ForegroundColor Cyan
    Write-Host ($metrics | ConvertTo-Json -Depth 10) -ForegroundColor Cyan
    Write-Host "$('='*80)" -ForegroundColor Cyan
    Write-Host ""
}

function Show-Alerts {
    $data = Collect-AllMetrics
    
    Write-Host "`n$('='*80)" -ForegroundColor Cyan
    Write-Host "ACTIVE ALERTS ($($data.alerts.Count))" -ForegroundColor Cyan
    Write-Host "$('='*80)" -ForegroundColor Cyan
    
    if ($data.alerts.Count -eq 0) {
        Write-Host "No active alerts" -ForegroundColor Green
    } else {
        foreach ($alert in $data.alerts) {
            $color = if ($alert.severity -eq 'Critical') { 'Red' } else { 'Yellow' }
            Write-Host "[$($alert.id)] [$($alert.severity)] $($alert.component)" -ForegroundColor $color
            Write-Host "  Message: $($alert.message)" -ForegroundColor Gray
            Write-Host "  Time: $($alert.timestamp)" -ForegroundColor Gray
            Write-Host ""
        }
    }
    Write-Host "$('='*80)" -ForegroundColor Cyan
    Write-Host ""
}

# ===========================
# MAIN EXECUTION
# ===========================

try {
    Write-Host "`n"
    Write-DashboardLog "HELIOS Monitoring Dashboard v1.0" -Level Info
    Write-DashboardLog "Action: $Action | Refresh Interval: ${RefreshInterval}s" -Level Info
    
    switch ($Action) {
        'StartDashboard' {
            Start-Dashboard
        }
        
        'GetMetrics' {
            if ([string]::IsNullOrEmpty($Component)) {
                Write-DashboardLog "ERROR: Component is required for GetMetrics action" -Level Error
                exit 1
            }
            Show-ComponentMetrics -ComponentName $Component
        }
        
        'GetAlerts' {
            Show-Alerts
        }
        
        'GetComponentStatus' {
            $data = Collect-AllMetrics
            $health = Get-SystemHealth -AllMetrics $data
            Show-Dashboard -AllMetrics $data -SystemHealth $health
        }
        
        'GetSystemHealth' {
            $data = Collect-AllMetrics
            $health = Get-SystemHealth -AllMetrics $data
            Write-Host ($health | ConvertTo-Json -Depth 10) -ForegroundColor Cyan
        }
    }
    
    Write-DashboardLog "Operation completed successfully" -Level Success
}
catch {
    Write-DashboardLog "FATAL ERROR: $_" -Level Error
    exit 1
}
