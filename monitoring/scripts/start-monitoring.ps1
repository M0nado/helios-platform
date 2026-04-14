<#
.SYNOPSIS
Start HELIOS Monitoring System - Master Control Script

.DESCRIPTION
Orchestrates startup of all monitoring components:
1. Database initialization
2. Health check collector
3. Performance metrics collector
4. Alert evaluator
5. SLA calculator
6. Anomaly detector
7. Incident manager
8. Web dashboard

.PARAMETER Mode
Execution mode: 'Single' (run once), 'Continuous' (run indefinitely), 'Dashboard' (web only)

.PARAMETER Port
Port for web dashboard (default: 8080)

.EXAMPLE
.\start-monitoring.ps1 -Mode Continuous -Port 8080
#>

param(
    [ValidateSet("Single", "Continuous", "Dashboard")]
    [string]$Mode = "Continuous",
    [int]$Port = 8080,
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\monitoring\data\helios_monitoring.db",
    [bool]$InitializeDB = $true
)

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommandPath
$MonitoringDir = Split-Path -Path $ScriptRoot -Parent

Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║                  HELIOS PLATFORM MONITORING                    ║
║             Comprehensive Monitoring & Alerting System         ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host "`nStartup Configuration:"
Write-Host "  Mode: $Mode"
Write-Host "  Database: $DatabasePath"
Write-Host "  Dashboard Port: $Port"
Write-Host "  Monitoring Directory: $MonitoringDir"

# Initialize database if needed
if ($InitializeDB) {
    Write-Host "`n[1/8] Initializing database..."
    & "$ScriptRoot\00-init-database.ps1" -DatabasePath $DatabasePath
    Start-Sleep -Seconds 2
}

# Function to start background job safely
function Start-MonitoringJob {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [hashtable]$Parameters
    )
    
    try {
        $ParamString = ""
        if ($Parameters) {
            foreach ($Key in $Parameters.Keys) {
                $Value = $Parameters[$Key]
                if ($Value -is [string]) {
                    $ParamString += " -$Key '$Value'"
                } else {
                    $ParamString += " -$Key $Value"
                }
            }
        }
        
        $JobScript = "& '$ScriptPath'$ParamString"
        $Job = Start-Job -ScriptBlock ([scriptblock]::Create($JobScript)) -Name $Name
        
        Write-Host "  ✓ Started: $Name (Job ID: $($Job.Id))"
        return $Job
    } catch {
        Write-Error "Failed to start $Name : $_"
        return $null
    }
}

# Start all monitoring components
Write-Host "`n[Starting Monitoring Components]"

$Jobs = @()

if ($Mode -ne "Dashboard") {
    Write-Host "`n[2/8] Starting Health Checks..."
    $Jobs += Start-MonitoringJob -Name "HealthCheck" -ScriptPath "$ScriptRoot\01-health-check.ps1" `
        -Parameters @{ CheckInterval = 30; DatabasePath = $DatabasePath }
    
    Write-Host "`n[3/8] Starting Performance Metrics Collection..."
    $Jobs += Start-MonitoringJob -Name "PerformanceMetrics" -ScriptPath "$ScriptRoot\02-performance-metrics.ps1" `
        -Parameters @{ MetricsInterval = 60; DatabasePath = $DatabasePath }
    
    Write-Host "`n[4/8] Starting Alert Evaluator..."
    $Jobs += Start-MonitoringJob -Name "AlertEvaluator" -ScriptPath "$ScriptRoot\03-alert-evaluator.ps1" `
        -Parameters @{ DatabasePath = $DatabasePath }
    
    Write-Host "`n[5/8] Starting SLA Calculator..."
    $Jobs += Start-MonitoringJob -Name "SLACalculator" -ScriptPath "$ScriptRoot\04-sla-calculator.ps1" `
        -Parameters @{ DatabasePath = $DatabasePath }
    
    Write-Host "`n[6/8] Starting Anomaly Detector..."
    $Jobs += Start-MonitoringJob -Name "AnomalyDetector" -ScriptPath "$ScriptRoot\05-anomaly-detector.ps1" `
        -Parameters @{ DatabasePath = $DatabasePath }
    
    Write-Host "`n[7/8] Starting Incident Manager..."
    $Jobs += Start-MonitoringJob -Name "IncidentManager" -ScriptPath "$ScriptRoot\06-incident-manager.ps1" `
        -Parameters @{ DatabasePath = $DatabasePath }
}

# Start web dashboard
Write-Host "`n[8/8] Starting Web Dashboard Server..."
Write-Host "  Dashboard will be available at: http://localhost:$Port"

# Simple HTTP server for dashboard
$DashboardPath = "$MonitoringDir\dashboards\index.html"
Write-Host "  Dashboard file: $DashboardPath"

# Create a simple HTTP listener for the dashboard
if (Test-Path $DashboardPath) {
    $DashboardScript = {
        param($Port, $DashboardPath)
        
        $Listener = [System.Net.HttpListener]::new()
        $Listener.Prefixes.Add("http://localhost:$Port/")
        $Listener.Start()
        
        Write-Host "✓ Dashboard server listening on port $Port"
        
        while ($true) {
            $Context = $Listener.GetContext()
            $Request = $Context.Request
            $Response = $Context.Response
            
            if ($Request.Url.AbsolutePath -eq "/" -or $Request.Url.AbsolutePath -eq "/index.html") {
                $Content = Get-Content $DashboardPath -Raw
                $Response.ContentType = "text/html"
            } else {
                $Response.StatusCode = 404
                $Content = "Not Found"
            }
            
            [byte[]]$Buffer = [System.Text.Encoding]::UTF8.GetBytes($Content)
            $Response.ContentLength64 = $Buffer.Length
            $Response.OutputStream.Write($Buffer, 0, $Buffer.Length)
            $Response.Close()
        }
    }
    
    $DashboardJob = Start-Job -ScriptBlock $DashboardScript -ArgumentList $Port, $DashboardPath -Name "DashboardServer"
    $Jobs += $DashboardJob
    Write-Host "  ✓ Started: Dashboard Server (Job ID: $($DashboardJob.Id))"
}

# Display startup summary
Write-Host @"

╔════════════════════════════════════════════════════════════════╗
║                   STARTUP COMPLETE                             ║
╚════════════════════════════════════════════════════════════════╝

MONITORING COMPONENTS RUNNING:
"@ -ForegroundColor Green

foreach ($Job in $Jobs) {
    Write-Host "  ✓ $($Job.Name) - Job ID: $($Job.Id)"
}

Write-Host @"

DASHBOARDS & ACCESS POINTS:
  Web Dashboard: http://localhost:$Port
  Health Status: $MonitoringDir\data\latest_health_check.json
  Performance Metrics: $MonitoringDir\data\latest_performance_metrics.json
  Alerts: $MonitoringDir\data\alert_status.json
  Incidents: $MonitoringDir\data\incidents_status.json
  Anomalies: $MonitoringDir\data\anomalies.json

CONFIGURATION:
  Alert Rules: $MonitoringDir\config\alert-rules.json
  Components: $MonitoringDir\config\components.json
  Notifications: $MonitoringDir\config\notifications.json

LOGS & DATA:
  Logs: $MonitoringDir\logs\
  Data: $MonitoringDir\data\

CONTROL COMMANDS:
  Get-Job -Name "HealthCheck" | Format-List
  Stop-Job -Name "HealthCheck"
  Remove-Job -Name "HealthCheck"

NEXT STEPS:
1. Open dashboard: http://localhost:$Port
2. Monitor job status: Get-Job
3. View logs: Get-Content $MonitoringDir\logs\*.log

════════════════════════════════════════════════════════════════

Mode: $Mode
Press Ctrl+C to stop monitoring (jobs will continue running)
"@

# Monitor mode
if ($Mode -eq "Continuous") {
    Write-Host "`nMonitoring mode: Continuous. Press Ctrl+C to stop."
    Write-Host "Jobs will continue running. Use 'Get-Job' to view status."
    Write-Host "Use 'Stop-Job -Name <JobName>' to stop individual components."
    
    # Keep script running
    while ($true) {
        Start-Sleep -Seconds 10
        
        # Check if any jobs have failed
        $FailedJobs = Get-Job | Where-Object { $_.State -eq "Failed" }
        if ($FailedJobs) {
            Write-Host "`n⚠️ Failed jobs detected:" -ForegroundColor Yellow
            foreach ($Job in $FailedJobs) {
                Write-Host "  - $($Job.Name): $($Job.PSEndTime)"
            }
        }
    }
} elseif ($Mode -eq "Single") {
    Write-Host "`nSingle mode: Running collectors once. Waiting for completion..."
    Get-Job | Wait-Job
} else {
    Write-Host "`nDashboard-only mode. Press Enter to exit."
    Read-Host
}
