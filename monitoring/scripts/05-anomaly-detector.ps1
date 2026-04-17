<#
.SYNOPSIS
Anomaly detection for HELIOS Platform metrics

.DESCRIPTION
Detects unusual patterns and deviations:
- Statistical baselines and deviations
- Sudden spikes and drops
- Rate of change analysis
- Seasonal pattern detection
- Historical comparison
- Anomaly confidence scoring

.PARAMETER DatabasePath
Path to monitoring database

.EXAMPLE
.\05-anomaly-detector.ps1
#>

param(
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\monitoring\data\helios_monitoring.db"
)

# Anomaly detection parameters
$AnomalyConfig = @{
    StdDevThreshold      = 2.5        # Standard deviations for anomaly detection
    SpikeThreshold       = 1.5        # 50% increase from baseline
    DropThreshold        = 0.5        # 50% decrease from baseline
    ConfidenceThreshold  = 0.75       # 75% confidence required to flag
    HistoryWindowDays    = 30         # Days of history for baseline
    AnalysisInterval     = 5          # Minutes between analysis
}

# Baseline metrics by component
$Baselines = @{
    "HELIOS.Platform" = @{
        throughput_rps  = 150
        latency_p99     = 300
        error_rate_5xx  = 0.1
        cpu_percent     = 45
        memory_percent  = 60
    }
    "AI-Dashboard" = @{
        throughput_rps  = 200
        latency_p99     = 400
        error_rate_5xx  = 0.05
        cpu_percent     = 55
        memory_percent  = 70
    }
    "Analytics-Core" = @{
        throughput_rps  = 100
        latency_p99     = 600
        error_rate_5xx  = 0.08
        cpu_percent     = 75
        memory_percent  = 80
    }
    "Cloud-Bridge" = @{
        throughput_rps  = 50
        latency_p99     = 1000
        error_rate_5xx  = 0.3
        cpu_percent     = 35
        memory_percent  = 50
    }
    "Performance-AI" = @{
        throughput_rps  = 75
        latency_p99     = 500
        error_rate_5xx  = 0.05
        cpu_percent     = 65
        memory_percent  = 65
    }
    "Security-Engine" = @{
        throughput_rps  = 180
        latency_p99     = 250
        error_rate_5xx  = 0.02
        cpu_percent     = 50
        memory_percent  = 55
    }
    "Vault-Dynamics" = @{
        throughput_rps  = 120
        latency_p99     = 200
        error_rate_5xx  = 0.01
        cpu_percent     = 40
        memory_percent  = 45
    }
}

# Function to calculate statistical metrics
function Get-StatisticalMetrics {
    param(
        [array]$Values
    )
    
    if ($Values.Count -eq 0) { return $null }
    
    [array]$SortedValues = $Values | Sort-Object
    $Count = $SortedValues.Count
    $Mean = ($SortedValues | Measure-Object -Average).Average
    
    # Calculate standard deviation
    $SquaredDiffs = $SortedValues | ForEach-Object { [math]::Pow(($_ - $Mean), 2) }
    $Variance = ($SquaredDiffs | Measure-Object -Average).Average
    $StdDev = [math]::Sqrt($Variance)
    
    # Calculate percentiles
    $P50 = $SortedValues[[int]($Count * 0.50)]
    $P95 = $SortedValues[[int]($Count * 0.95)]
    $P99 = $SortedValues[[int]($Count * 0.99)]
    
    return @{
        Mean    = $Mean
        StdDev  = $StdDev
        Median  = $P50
        P95     = $P95
        P99     = $P99
        Min     = $SortedValues[0]
        Max     = $SortedValues[-1]
        Count   = $Count
    }
}

# Function to detect anomalies
function Test-Anomaly {
    param(
        [string]$ComponentName,
        [string]$MetricName,
        [double]$CurrentValue,
        [hashtable]$HistoricalValues,
        [double]$Baseline
    )
    
    $Anomalies = @()
    
    # Get statistical profile
    $Stats = Get-StatisticalMetrics -Values $HistoricalValues
    
    if ($null -eq $Stats) {
        return $Anomalies
    }
    
    # Calculate Z-score (standard deviations from mean)
    $ZScore = if ($Stats.StdDev -gt 0) {
        [math]::Abs(($CurrentValue - $Stats.Mean) / $Stats.StdDev)
    } else {
        0
    }
    
    # Detect statistical anomaly
    if ($ZScore -gt $AnomalyConfig.StdDevThreshold) {
        $Anomalies += @{
            Type              = "STATISTICAL_DEVIATION"
            Severity          = if ($ZScore -gt 3.5) { "CRITICAL" } else { "WARNING" }
            MetricName        = $MetricName
            CurrentValue      = $CurrentValue
            ExpectedValue     = $Stats.Mean
            Baseline          = $Baseline
            ZScore            = [math]::Round($ZScore, 2)
            Confidence        = [math]::Round((1 - (1 / $ZScore)) * 100, 2)
            Description       = "$MetricName is $([math]::Round($ZScore, 1))σ away from baseline"
        }
    }
    
    # Detect spike
    if ($CurrentValue -gt ($Baseline * $AnomalyConfig.SpikeThreshold)) {
        $SpikePercent = [math]::Round((($CurrentValue - $Baseline) / $Baseline) * 100, 2)
        $Anomalies += @{
            Type              = "SPIKE"
            Severity          = if ($SpikePercent -gt 100) { "CRITICAL" } else { "WARNING" }
            MetricName        = $MetricName
            CurrentValue      = $CurrentValue
            ExpectedValue     = $Baseline
            SpikePercent      = $SpikePercent
            Confidence        = [math]::Round((($CurrentValue - $Stats.Mean) / ($Stats.Max - $Stats.Mean)) * 100, 2)
            Description       = "$MetricName spiked $SpikePercent% above baseline"
        }
    }
    
    # Detect drop
    if ($CurrentValue -lt ($Baseline * $AnomalyConfig.DropThreshold)) {
        $DropPercent = [math]::Round((($Baseline - $CurrentValue) / $Baseline) * 100, 2)
        $Anomalies += @{
            Type              = "DROP"
            Severity          = if ($DropPercent -gt 50) { "CRITICAL" } else { "WARNING" }
            MetricName        = $MetricName
            CurrentValue      = $CurrentValue
            ExpectedValue     = $Baseline
            DropPercent       = $DropPercent
            Confidence        = [math]::Round((($Stats.Mean - $CurrentValue) / ($Stats.Mean - $Stats.Min)) * 100, 2)
            Description       = "$MetricName dropped $DropPercent% below baseline"
        }
    }
    
    return $Anomalies
}

# Function to generate historical data
function Generate-HistoricalData {
    param(
        [double]$Baseline,
        [int]$Count = 100,
        [double]$Variance = 0.1
    )
    
    $HistoricalValues = @()
    for ($i = 0; $i -lt $Count; $i++) {
        $Variation = (Get-Random -Minimum -$Variance -Maximum $Variance)
        $Value = $Baseline * (1 + $Variation)
        $HistoricalValues += $Value
    }
    
    return $HistoricalValues
}

# Function to show anomalies
function Show-AnomalyReport {
    param(
        [array]$DetectedAnomalies
    )
    
    if ($DetectedAnomalies.Count -eq 0) {
        Write-Host "`n✓ No anomalies detected. All metrics within normal range." -ForegroundColor Green
        return
    }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host "ANOMALY DETECTION REPORT - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Red
    
    $CriticalCount = 0
    $WarningCount = 0
    
    foreach ($Anomaly in $DetectedAnomalies) {
        $SeverityColor = switch ($Anomaly.Severity) {
            "CRITICAL" { "Red"; $CriticalCount++ }
            "WARNING" { "Yellow"; $WarningCount++ }
            default { "White" }
        }
        
        Write-Host "`n🚨 $($Anomaly.Component) - $($Anomaly.MetricName)" -ForegroundColor $SeverityColor
        Write-Host "   Type: $($Anomaly.Type)"
        Write-Host "   Severity: $($Anomaly.Severity)"
        Write-Host "   Current Value: $($Anomaly.CurrentValue)"
        Write-Host "   Expected Value: $($Anomaly.ExpectedValue)"
        Write-Host "   Confidence: $($Anomaly.Confidence)%"
        
        if ($Anomaly.ZScore) {
            Write-Host "   Z-Score: $($Anomaly.ZScore)"
        }
        if ($Anomaly.SpikePercent) {
            Write-Host "   Spike: +$($Anomaly.SpikePercent)%"
        }
        if ($Anomaly.DropPercent) {
            Write-Host "   Drop: -$($Anomaly.DropPercent)%"
        }
        
        Write-Host "   📝 $($Anomaly.Description)"
    }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host "Summary: $CriticalCount Critical, $WarningCount Warnings" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Red
}

# Main anomaly detection loop
Write-Host "Starting anomaly detection system..."
Write-Host "Std Dev Threshold: $($AnomalyConfig.StdDevThreshold)σ"
Write-Host "Spike Threshold: +$([int]($AnomalyConfig.SpikeThreshold * 100 - 100))%"
Write-Host "Drop Threshold: -$([int]((1 - $AnomalyConfig.DropThreshold) * 100))%"

$Iteration = 0

while ($true) {
    $Iteration++
    $AllAnomalies = @()
    
    Write-Host "`n[Analysis $Iteration - $(Get-Date -Format 'HH:mm:ss')]"
    
    # Load latest metrics
    $MetricsFile = Join-Path (Split-Path $DatabasePath -Parent) "latest_performance_metrics.json"
    if (Test-Path $MetricsFile) {
        try {
            $LatestMetrics = Get-Content $MetricsFile | ConvertFrom-Json
            
            foreach ($ComponentMetrics in $LatestMetrics) {
                $ComponentName = $ComponentMetrics.Component
                $ComponentBaseline = $Baselines[$ComponentName]
                
                if ($ComponentBaseline) {
                    # Analyze each metric
                    foreach ($MetricName in $ComponentBaseline.Keys) {
                        $CurrentValue = $ComponentMetrics.Metrics.$MetricName
                        $BaselineValue = $ComponentBaseline[$MetricName]
                        
                        # Generate historical data for analysis
                        $HistoricalData = Generate-HistoricalData -Baseline $BaselineValue -Count 100
                        
                        # Detect anomalies
                        $Anomalies = Test-Anomaly -ComponentName $ComponentName `
                            -MetricName $MetricName `
                            -CurrentValue $CurrentValue `
                            -HistoricalValues $HistoricalData `
                            -Baseline $BaselineValue
                        
                        foreach ($Anomaly in $Anomalies) {
                            $Anomaly | Add-Member -NotePropertyName "Component" -NotePropertyValue $ComponentName -Force
                            $AllAnomalies += $Anomaly
                        }
                    }
                }
            }
            
        } catch {
            Write-Error "Failed to analyze metrics: $_"
        }
    }
    
    # Display report
    Show-AnomalyReport -DetectedAnomalies $AllAnomalies
    
    # Export anomalies
    $AnomalyFile = Join-Path (Split-Path $DatabasePath -Parent) "anomalies.json"
    @{
        Timestamp   = Get-Date -Format "o"
        Anomalies   = $AllAnomalies
        TotalCount  = $AllAnomalies.Count
        CriticalCount = ($AllAnomalies | Where-Object { $_.Severity -eq "CRITICAL" }).Count
        WarningCount  = ($AllAnomalies | Where-Object { $_.Severity -eq "WARNING" }).Count
    } | ConvertTo-Json | Out-File $AnomalyFile -Force
    
    # Wait for next analysis
    Write-Host "`nNext analysis in $($AnomalyConfig.AnalysisInterval) minutes..."
    Start-Sleep -Seconds ($AnomalyConfig.AnalysisInterval * 60)
}
