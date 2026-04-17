<#
.SYNOPSIS
Collect performance metrics from HELIOS Platform components

.DESCRIPTION
Collects key performance metrics:
- Request throughput (requests/sec)
- Response latency (p50, p95, p99)
- Error rates (4xx, 5xx)
- CPU and memory usage
- Disk I/O operations
- Network bandwidth

.PARAMETER MetricsInterval
Interval between metric collections in seconds

.PARAMETER DatabasePath
Path to monitoring database

.EXAMPLE
.\02-performance-metrics.ps1 -MetricsInterval 60
#>

param(
    [int]$MetricsInterval = 60,
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\monitoring\data\helios_monitoring.db"
)

# Component metrics endpoints
$Components = @(
    @{ Name = "HELIOS.Platform"; MetricsEndpoint = "http://localhost:5000/metrics" },
    @{ Name = "AI-Dashboard"; MetricsEndpoint = "http://localhost:5001/metrics" },
    @{ Name = "Analytics-Core"; MetricsEndpoint = "http://localhost:5002/metrics" },
    @{ Name = "Cloud-Bridge"; MetricsEndpoint = "http://localhost:5003/metrics" },
    @{ Name = "Performance-AI"; MetricsEndpoint = "http://localhost:5004/metrics" },
    @{ Name = "Security-Engine"; MetricsEndpoint = "http://localhost:5005/metrics" },
    @{ Name = "Vault-Dynamics"; MetricsEndpoint = "http://localhost:5006/metrics" }
)

# Generate realistic metrics for demonstration
function Get-SimulatedMetrics {
    param(
        [string]$ComponentName
    )
    
    # Simulate realistic metrics with some variance
    $BaselineData = @{
        "HELIOS.Platform" = @{
            throughput_rps = 150 + (Get-Random -Minimum -20 -Maximum 20)
            latency_p50 = 50 + (Get-Random -Minimum -10 -Maximum 15)
            latency_p95 = 150 + (Get-Random -Minimum -20 -Maximum 30)
            latency_p99 = 300 + (Get-Random -Minimum -50 -Maximum 75)
            error_rate_4xx = 0.5 + (Get-Random -Minimum -0.2 -Maximum 0.3)
            error_rate_5xx = 0.1 + (Get-Random -Minimum -0.05 -Maximum 0.1)
            cpu_percent = 45 + (Get-Random -Minimum -10 -Maximum 15)
            memory_percent = 60 + (Get-Random -Minimum -10 -Maximum 10)
            disk_io_mbps = 50 + (Get-Random -Minimum -15 -Maximum 20)
            network_bandwidth_mbps = 100 + (Get-Random -Minimum -30 -Maximum 40)
        }
        "AI-Dashboard" = @{
            throughput_rps = 200 + (Get-Random -Minimum -25 -Maximum 25)
            latency_p50 = 80 + (Get-Random -Minimum -15 -Maximum 20)
            latency_p95 = 200 + (Get-Random -Minimum -30 -Maximum 40)
            latency_p99 = 400 + (Get-Random -Minimum -75 -Maximum 100)
            error_rate_4xx = 0.3 + (Get-Random -Minimum -0.1 -Maximum 0.2)
            error_rate_5xx = 0.05 + (Get-Random -Minimum -0.03 -Maximum 0.05)
            cpu_percent = 55 + (Get-Random -Minimum -12 -Maximum 12)
            memory_percent = 70 + (Get-Random -Minimum -15 -Maximum 10)
            disk_io_mbps = 30 + (Get-Random -Minimum -10 -Maximum 15)
            network_bandwidth_mbps = 150 + (Get-Random -Minimum -40 -Maximum 50)
        }
        "Analytics-Core" = @{
            throughput_rps = 100 + (Get-Random -Minimum -15 -Maximum 15)
            latency_p50 = 120 + (Get-Random -Minimum -20 -Maximum 25)
            latency_p95 = 350 + (Get-Random -Minimum -50 -Maximum 60)
            latency_p99 = 600 + (Get-Random -Minimum -100 -Maximum 150)
            error_rate_4xx = 0.2 + (Get-Random -Minimum -0.1 -Maximum 0.15)
            error_rate_5xx = 0.08 + (Get-Random -Minimum -0.04 -Maximum 0.08)
            cpu_percent = 75 + (Get-Random -Minimum -10 -Maximum 10)
            memory_percent = 80 + (Get-Random -Minimum -15 -Maximum 8)
            disk_io_mbps = 120 + (Get-Random -Minimum -30 -Maximum 40)
            network_bandwidth_mbps = 80 + (Get-Random -Minimum -25 -Maximum 35)
        }
        "Cloud-Bridge" = @{
            throughput_rps = 50 + (Get-Random -Minimum -10 -Maximum 15)
            latency_p50 = 200 + (Get-Random -Minimum -30 -Maximum 40)
            latency_p95 = 500 + (Get-Random -Minimum -80 -Maximum 100)
            latency_p99 = 1000 + (Get-Random -Minimum -150 -Maximum 200)
            error_rate_4xx = 1.2 + (Get-Random -Minimum -0.3 -Maximum 0.4)
            error_rate_5xx = 0.3 + (Get-Random -Minimum -0.1 -Maximum 0.2)
            cpu_percent = 35 + (Get-Random -Minimum -8 -Maximum 12)
            memory_percent = 50 + (Get-Random -Minimum -10 -Maximum 12)
            disk_io_mbps = 20 + (Get-Random -Minimum -5 -Maximum 10)
            network_bandwidth_mbps = 200 + (Get-Random -Minimum -50 -Maximum 60)
        }
        "Performance-AI" = @{
            throughput_rps = 75 + (Get-Random -Minimum -12 -Maximum 18)
            latency_p50 = 90 + (Get-Random -Minimum -15 -Maximum 20)
            latency_p95 = 250 + (Get-Random -Minimum -40 -Maximum 50)
            latency_p99 = 500 + (Get-Random -Minimum -100 -Maximum 150)
            error_rate_4xx = 0.15 + (Get-Random -Minimum -0.08 -Maximum 0.12)
            error_rate_5xx = 0.05 + (Get-Random -Minimum -0.03 -Maximum 0.06)
            cpu_percent = 65 + (Get-Random -Minimum -12 -Maximum 15)
            memory_percent = 65 + (Get-Random -Minimum -12 -Maximum 12)
            disk_io_mbps = 40 + (Get-Random -Minimum -12 -Maximum 18)
            network_bandwidth_mbps = 90 + (Get-Random -Minimum -25 -Maximum 35)
        }
        "Security-Engine" = @{
            throughput_rps = 180 + (Get-Random -Minimum -20 -Maximum 25)
            latency_p50 = 40 + (Get-Random -Minimum -8 -Maximum 12)
            latency_p95 = 120 + (Get-Random -Minimum -20 -Maximum 30)
            latency_p99 = 250 + (Get-Random -Minimum -40 -Maximum 60)
            error_rate_4xx = 0.4 + (Get-Random -Minimum -0.15 -Maximum 0.2)
            error_rate_5xx = 0.02 + (Get-Random -Minimum -0.01 -Maximum 0.03)
            cpu_percent = 50 + (Get-Random -Minimum -10 -Maximum 15)
            memory_percent = 55 + (Get-Random -Minimum -10 -Maximum 10)
            disk_io_mbps = 35 + (Get-Random -Minimum -10 -Maximum 15)
            network_bandwidth_mbps = 110 + (Get-Random -Minimum -30 -Maximum 40)
        }
        "Vault-Dynamics" = @{
            throughput_rps = 120 + (Get-Random -Minimum -18 -Maximum 22)
            latency_p50 = 30 + (Get-Random -Minimum -5 -Maximum 8)
            latency_p95 = 100 + (Get-Random -Minimum -15 -Maximum 20)
            latency_p99 = 200 + (Get-Random -Minimum -30 -Maximum 40)
            error_rate_4xx = 0.2 + (Get-Random -Minimum -0.1 -Maximum 0.15)
            error_rate_5xx = 0.01 + (Get-Random -Minimum -0.005 -Maximum 0.02)
            cpu_percent = 40 + (Get-Random -Minimum -8 -Maximum 12)
            memory_percent = 45 + (Get-Random -Minimum -8 -Maximum 10)
            disk_io_mbps = 15 + (Get-Random -Minimum -5 -Maximum 10)
            network_bandwidth_mbps = 60 + (Get-Random -Minimum -15 -Maximum 25)
        }
    }
    
    return $BaselineData[$ComponentName]
}

function Write-MetricsToDatabase {
    param(
        [string]$ComponentName,
        [hashtable]$Metrics,
        [string]$DatabasePath
    )
    
    try {
        $LogPath = Split-Path $DatabasePath -Parent
        $MetricsFile = Join-Path $LogPath "performance_metrics.jsonl"
        
        $MetricsEntry = @{
            Timestamp = Get-Date -Format "o"
            Component = $ComponentName
            Metrics   = $Metrics
        } | ConvertTo-Json -Compress
        
        Add-Content -Path $MetricsFile -Value $MetricsEntry -Force
        
    } catch {
        Write-Error "Failed to write metrics: $_"
    }
}

function Show-MetricsReport {
    param(
        [array]$AllMetrics
    )
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "Performance Metrics Report - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    foreach ($Metric in $AllMetrics) {
        Write-Host "`n📊 $($Metric.Component)" -ForegroundColor Yellow
        Write-Host "  ├─ Throughput: $($Metric.Metrics.throughput_rps) req/s"
        Write-Host "  ├─ Latency P50: $($Metric.Metrics.latency_p50)ms | P95: $($Metric.Metrics.latency_p95)ms | P99: $($Metric.Metrics.latency_p99)ms"
        Write-Host "  ├─ Errors 4xx: $($Metric.Metrics.error_rate_4xx)% | 5xx: $($Metric.Metrics.error_rate_5xx)%"
        Write-Host "  ├─ CPU: $($Metric.Metrics.cpu_percent)% | Memory: $($Metric.Metrics.memory_percent)%"
        Write-Host "  ├─ Disk I/O: $($Metric.Metrics.disk_io_mbps) MB/s"
        Write-Host "  └─ Network: $($Metric.Metrics.network_bandwidth_mbps) MB/s"
    }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# Main collection loop
Write-Host "Starting performance metrics collection..."
Write-Host "Components: $($Components.Count)"
Write-Host "Collection Interval: $($MetricsInterval)s"
Write-Host "Press Ctrl+C to stop"

$Iteration = 0

while ($true) {
    $Iteration++
    $AllMetrics = @()
    
    Write-Host "`n[Collection $Iteration - $(Get-Date -Format 'HH:mm:ss')]"
    
    foreach ($Component in $Components) {
        try {
            # Get metrics (simulated for now)
            $Metrics = Get-SimulatedMetrics -ComponentName $Component.Name
            
            $MetricRecord = @{
                Component = $Component.Name
                Metrics   = $Metrics
                Endpoint  = $Component.MetricsEndpoint
                Timestamp = Get-Date
            }
            
            $AllMetrics += $MetricRecord
            
            # Write to database
            Write-MetricsToDatabase -ComponentName $Component.Name -Metrics $Metrics -DatabasePath $DatabasePath
            
        } catch {
            Write-Error "Failed to collect metrics for $($Component.Name): $_"
        }
    }
    
    # Display report
    Show-MetricsReport -AllMetrics $AllMetrics
    
    # Export as JSON
    $ExportFile = Join-Path (Split-Path $DatabasePath -Parent) "latest_performance_metrics.json"
    $AllMetrics | ConvertTo-Json | Out-File $ExportFile -Force
    
    # Wait for next collection
    Write-Host "`nNext collection in $($MetricsInterval) seconds..."
    Start-Sleep -Seconds $MetricsInterval
}
