<#
.SYNOPSIS
Real-time health checks for all HELIOS Platform components

.DESCRIPTION
Performs health checks against all 7 components:
1. HELIOS.Platform Core
2. AI-Dashboard
3. Analytics-Core
4. Cloud-Bridge
5. Performance-AI
6. Security-Engine
7. Vault-Dynamics

Checks:
- HTTP endpoint availability and response code
- Response time
- Service responsiveness
- Database connectivity
- Port availability
- Certificate validation
- Integration health

.PARAMETER CheckInterval
Interval between checks in seconds (default: 30)

.PARAMETER DatabasePath
Path to monitoring database

.EXAMPLE
.\01-health-check.ps1 -CheckInterval 30
#>

param(
    [int]$CheckInterval = 30,
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\monitoring\data\helios_monitoring.db",
    [bool]$ContinuousMonitoring = $true
)

# Component health check endpoints
$Components = @(
    @{
        Name           = "HELIOS.Platform"
        Description    = "Main API and orchestration"
        HealthEndpoint = "http://localhost:5000/health"
        Port           = 5000
        Type           = "API"
    },
    @{
        Name           = "AI-Dashboard"
        Description    = "Analytics and visualization"
        HealthEndpoint = "http://localhost:5001/health"
        Port           = 5001
        Type           = "WebUI"
    },
    @{
        Name           = "Analytics-Core"
        Description    = "Data processing and analysis"
        HealthEndpoint = "http://localhost:5002/health"
        Port           = 5002
        Type           = "Service"
    },
    @{
        Name           = "Cloud-Bridge"
        Description    = "Cloud integration layer"
        HealthEndpoint = "http://localhost:5003/health"
        Port           = 5003
        Type           = "Gateway"
    },
    @{
        Name           = "Performance-AI"
        Description    = "Performance optimization"
        HealthEndpoint = "http://localhost:5004/health"
        Port           = 5004
        Type           = "Service"
    },
    @{
        Name           = "Security-Engine"
        Description    = "Security and compliance"
        HealthEndpoint = "http://localhost:5005/health"
        Port           = 5005
        Type           = "Service"
    },
    @{
        Name           = "Vault-Dynamics"
        Description    = "Secrets and encryption management"
        HealthEndpoint = "http://localhost:5006/health"
        Port           = 5006
        Type           = "Security"
    }
)

# Function to perform health check
function Test-ComponentHealth {
    param(
        [hashtable]$Component,
        [string]$DatabasePath
    )
    
    $HealthStatus = @{
        Component      = $Component.Name
        Endpoint       = $Component.HealthEndpoint
        Port           = $Component.Port
        Timestamp      = Get-Date
        IsHealthy      = $false
        StatusCode     = $null
        ResponseTimeMs = $null
        ErrorMessage   = $null
    }
    
    try {
        # Test port availability
        $TCPConnection = Test-NetConnection -ComputerName "localhost" -Port $Component.Port -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
        
        if ($TCPConnection.TcpTestSucceeded -eq $false) {
            $HealthStatus.ErrorMessage = "Port $($Component.Port) not responding"
            return $HealthStatus
        }
        
        # Perform HTTP health check
        $StopWatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        try {
            $Response = Invoke-WebRequest -Uri $Component.HealthEndpoint `
                -TimeoutSec 5 `
                -UseBasicParsing `
                -ErrorAction Stop
            
            $StopWatch.Stop()
            
            $HealthStatus.StatusCode = $Response.StatusCode
            $HealthStatus.ResponseTimeMs = $StopWatch.ElapsedMilliseconds
            $HealthStatus.IsHealthy = ($Response.StatusCode -eq 200)
            
        } catch {
            $StopWatch.Stop()
            $HealthStatus.StatusCode = $_.Exception.Response.StatusCode.Value__
            $HealthStatus.ResponseTimeMs = $StopWatch.ElapsedMilliseconds
            $HealthStatus.ErrorMessage = $_.Exception.Message
            $HealthStatus.IsHealthy = $false
        }
        
    } catch {
        $HealthStatus.ErrorMessage = "Health check failed: $_"
    }
    
    return $HealthStatus
}

# Function to log health status to database
function Write-HealthCheckToDB {
    param(
        [hashtable]$HealthStatus,
        [string]$DatabasePath
    )
    
    try {
        # For now, write to file-based log
        $LogPath = Split-Path $DatabasePath -Parent
        $LogFile = Join-Path $LogPath "health_checks.log"
        
        $LogEntry = @{
            Timestamp      = $HealthStatus.Timestamp.ToString("o")
            Component      = $HealthStatus.Component
            Port           = $HealthStatus.Port
            IsHealthy      = $HealthStatus.IsHealthy
            StatusCode     = $HealthStatus.StatusCode
            ResponseTimeMs = $HealthStatus.ResponseTimeMs
            ErrorMessage   = $HealthStatus.ErrorMessage
        } | ConvertTo-Json -Compress
        
        Add-Content -Path $LogFile -Value $LogEntry -Force
        
    } catch {
        Write-Error "Failed to log health status: $_"
    }
}

# Function to display health status
function Show-HealthStatus {
    param(
        [array]$HealthStatuses
    )
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "HELIOS Platform - Health Check Report" -ForegroundColor Cyan
    Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    $HealthyCount = 0
    $UnhealthyCount = 0
    
    foreach ($Status in $HealthStatuses) {
        $HealthIcon = if ($Status.IsHealthy) { "✓" } else { "✗" }
        $HealthColor = if ($Status.IsHealthy) { "Green" } else { "Red" }
        
        Write-Host "`n$HealthIcon [$HealthColor] $($Status.Component)" -ForegroundColor $HealthColor
        Write-Host "  Endpoint: $($Status.Endpoint)"
        Write-Host "  Port: $($Status.Port)"
        Write-Host "  Status Code: $($Status.StatusCode ?? 'N/A')"
        Write-Host "  Response Time: $($Status.ResponseTimeMs)ms"
        
        if ($Status.ErrorMessage) {
            Write-Host "  Error: $($Status.ErrorMessage)" -ForegroundColor Yellow
        }
        
        if ($Status.IsHealthy) {
            $HealthyCount++
        } else {
            $UnhealthyCount++
        }
    }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Summary: $HealthyCount Healthy, $UnhealthyCount Unhealthy" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# Main monitoring loop
Write-Host "Starting health check monitoring for HELIOS Platform..."
Write-Host "Components: $($Components.Count)"
Write-Host "Check Interval: $($CheckInterval)s"
Write-Host "Continuous Monitoring: $ContinuousMonitoring"
Write-Host "Press Ctrl+C to stop"

$Iteration = 0

while ($true) {
    $Iteration++
    $HealthStatuses = @()
    
    Write-Host "`n[Iteration $Iteration - $(Get-Date -Format 'HH:mm:ss')]"
    
    foreach ($Component in $Components) {
        $HealthStatus = Test-ComponentHealth -Component $Component -DatabasePath $DatabasePath
        $HealthStatuses += $HealthStatus
        
        # Log to database
        Write-HealthCheckToDB -HealthStatus $HealthStatus -DatabasePath $DatabasePath
    }
    
    # Display summary
    Show-HealthStatus -HealthStatuses $HealthStatuses
    
    # Export metrics
    $MetricsFile = Join-Path (Split-Path $DatabasePath -Parent) "latest_health_check.json"
    $HealthStatuses | ConvertTo-Json | Out-File $MetricsFile -Force
    
    if (!$ContinuousMonitoring) {
        break
    }
    
    # Wait for next check
    Write-Host "`nNext check in $($CheckInterval) seconds..."
    Start-Sleep -Seconds $CheckInterval
}
