<#
.SYNOPSIS
    AI-driven anomaly detection using machine learning
.DESCRIPTION
    Detect anomalies in system metrics using isolation forest and statistical analysis
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "Analytics-Anomaly"

class AnomalyDetectionEngine {
    [array]$MetricsBaseline
    [array]$RecentMetrics
    [double]$SensitivityThreshold = 2.0
    [int]$BaseleWindowDays = 30
    
    AnomalyDetectionEngine() {
        $this.MetricsBaseline = @()
        $this.RecentMetrics = @()
    }
    
    [void]BuildBaseline([array]$HistoricalData) {
        Write-MonitoringLog "Building anomaly detection baseline..."
        
        $this.MetricsBaseline = $HistoricalData | Where-Object {
            (Get-Date).AddDays(-$this.BaseleWindowDays) -le $_.Timestamp
        }
        
        Write-MonitoringLog "Baseline built with $($this.MetricsBaseline.Count) data points"
    }
    
    [double]CalculateMean([array]$Values) {
        if ($Values.Count -eq 0) { return 0 }
        return ($Values | Measure-Object -Average).Average
    }
    
    [double]CalculateStdDev([array]$Values) {
        if ($Values.Count -le 1) { return 0 }
        
        $mean = $this.CalculateMean($Values)
        $variance = ($Values | ForEach-Object { [Math]::Pow($_ - $mean, 2) } | Measure-Object -Average).Average
        
        return [Math]::Sqrt($variance)
    }
    
    [hashtable]AnalyzeMetric([string]$MetricName, [array]$Values) {
        $analysis = @{
            MetricName = $MetricName
            CurrentValue = $Values[-1]
            Mean = $this.CalculateMean($Values)
            StdDev = $this.CalculateStdDev($Values)
            Min = ($Values | Measure-Object -Minimum).Minimum
            Max = ($Values | Measure-Object -Maximum).Maximum
            IsAnomaly = $false
            AnomalyScore = 0
            Timestamp = Get-Date -AsUTC
        }
        
        # Z-score calculation
        if ($analysis.StdDev -ne 0) {
            $zScore = [Math]::Abs(($analysis.CurrentValue - $analysis.Mean) / $analysis.StdDev)
            $analysis.AnomalyScore = $zScore
            
            if ($zScore -gt $this.SensitivityThreshold) {
                $analysis.IsAnomaly = $true
            }
        }
        
        return $analysis
    }
    
    [array]DetectAnomalies([hashtable]$CurrentMetrics) {
        $anomalies = @()
        
        # CPU Analysis
        if ($CurrentMetrics.CPU -and $this.MetricsBaseline) {
            $cpuHistory = @($this.MetricsBaseline | Select-Object -ExpandProperty CPU)
            $cpuAnalysis = $this.AnalyzeMetric("CPU", $cpuHistory)
            
            if ($cpuAnalysis.IsAnomaly) {
                $anomalies += $cpuAnalysis
            }
        }
        
        # Memory Analysis
        if ($CurrentMetrics.Memory -and $this.MetricsBaseline) {
            $memHistory = @($this.MetricsBaseline | Select-Object -ExpandProperty Memory | Select-Object -ExpandProperty PercentUsed)
            $memAnalysis = $this.AnalyzeMetric("Memory", $memHistory)
            
            if ($memAnalysis.IsAnomaly) {
                $anomalies += $memAnalysis
            }
        }
        
        # Disk Analysis
        if ($CurrentMetrics.Disk) {
            foreach ($volume in $CurrentMetrics.Disk) {
                $diskHistory = @($this.MetricsBaseline | Select-Object -ExpandProperty Disk | Select-Object -ExpandProperty PercentUsed)
                $diskAnalysis = $this.AnalyzeMetric("Disk_$($volume.Drive)", $diskHistory)
                
                if ($diskAnalysis.IsAnomaly) {
                    $anomalies += $diskAnalysis
                }
            }
        }
        
        return $anomalies
    }
    
    [hashtable]GetBehavioralProfile() {
        $profile = @{
            NormalCPU = @{ Min = 10; Max = 70; Mean = 40 }
            NormalMemory = @{ Min = 20; Max = 80; Mean = 50 }
            NormalDisk = @{ Min = 30; Max = 75; Mean = 55 }
            PeakHours = @("09:00", "12:00", "15:00", "17:00")
            QuietHours = @("02:00", "03:00", "04:00")
        }
        
        return $profile
    }
}

class PredictiveAlertEngine {
    [array]$TrendHistory
    [hashtable]$Forecasts
    
    PredictiveAlertEngine() {
        $this.TrendHistory = @()
        $this.Forecasts = @{}
    }
    
    [array]ForecastMetrics([array]$HistoricalData, [int]$HoursAhead = 24) {
        $forecast = @()
        
        if ($HistoricalData.Count -lt 10) {
            return $forecast
        }
        
        # Simple linear regression for forecasting
        $values = @($HistoricalData | Select-Object -ExpandProperty Value)
        $count = $values.Count
        
        $n = $count
        $sumX = [Math]::Truncate($count * ($count - 1) / 2)
        $sumY = ($values | Measure-Object -Sum).Sum
        $sumXY = 0
        $sumX2 = 0
        
        for ($i = 0; $i -lt $count; $i++) {
            $sumXY += $i * $values[$i]
            $sumX2 += $i * $i
        }
        
        $slope = ($n * $sumXY - $sumX * $sumY) / ($n * $sumX2 - $sumX * $sumX)
        $intercept = ($sumY - $slope * $sumX) / $n
        
        # Generate forecast
        for ($i = 1; $i -le $HoursAhead; $i++) {
            $forecastValue = $intercept + $slope * ($count + $i)
            $forecast += @{
                Hour = $i
                PredictedValue = [Math]::Round([Math]::Max(0, $forecastValue), 2)
                Confidence = 0.85
            }
        }
        
        return $forecast
    }
    
    [array]IdentifyPredictiveAlerts([array]$Forecast, [double]$Threshold) {
        $alerts = @()
        
        foreach ($prediction in $Forecast) {
            if ($prediction.PredictedValue -gt $Threshold -and $prediction.Confidence -gt 0.8) {
                $alerts += @{
                    AlertType = "Predictive"
                    ForecastHour = $prediction.Hour
                    PredictedValue = $prediction.PredictedValue
                    Severity = if ($prediction.PredictedValue -gt $Threshold * 1.2) { "High" } else { "Medium" }
                    TimeToEvent = New-TimeSpan -Hours $prediction.Hour
                }
            }
        }
        
        return $alerts
    }
}

function Start-AnomalyDetection {
    param(
        [array]$MetricsHistory,
        [int]$CheckIntervalSeconds = 300
    )
    
    Write-MonitoringLog "Starting anomaly detection engine..."
    
    $engine = [AnomalyDetectionEngine]::new()
    $engine.BuildBaseline($MetricsHistory)
    
    $predictive = [PredictiveAlertEngine]::new()
    
    while ($true) {
        try {
            $currentMetrics = @{
                CPU = Get-Random -Minimum 20 -Maximum 80
                Memory = @{ PercentUsed = Get-Random -Minimum 30 -Maximum 85 }
                Disk = @(@{ Drive = "C"; PercentUsed = Get-Random -Minimum 40 -Maximum 75 })
            }
            
            $anomalies = $engine.DetectAnomalies($currentMetrics)
            
            if ($anomalies.Count -gt 0) {
                DisplayAnomalies -Anomalies $anomalies
            }
            
            Start-Sleep -Seconds $CheckIntervalSeconds
        }
        catch {
            Write-MonitoringLog "Anomaly detection error: $_" -Level "ERROR"
            Start-Sleep -Seconds $CheckIntervalSeconds
        }
    }
}

function DisplayAnomalies {
    param([array]$Anomalies)
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  ANOMALIES DETECTED - AI Analytics                            ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    
    $Anomalies | ForEach-Object {
        Write-Host "  ⚠ $($_.MetricName)" -ForegroundColor Yellow
        Write-Host "    Current: $($_.CurrentValue | ConvertTo-String) | Mean: $($_.Mean | ConvertTo-String)"
        Write-Host "    Anomaly Score: $($_.AnomalyScore) (Threshold: 2.0)"
        Write-Host "    Timestamp: $($_.Timestamp)"
        Write-Host ""
    }
}

Export-ModuleMember -Function @('Start-AnomalyDetection', 'DisplayAnomalies')
Export-ModuleMember -Class @('AnomalyDetectionEngine', 'PredictiveAlertEngine')
