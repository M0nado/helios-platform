# HELIOS v4.0 - Experiment 7: Load Testing & Scalability Limits
# Pure PowerShell Implementation

param(
    [int]$TestDuration = 60,
    [double]$NetworkErrorRate = 0.01,
    [string]$OutputDir = "$PSScriptRoot\results"
)

# Metrics Collector
class MetricsCollector {
    [System.Collections.ArrayList]$Requests = @()
    [System.Collections.ArrayList]$MemorySnapshots = @()
    [datetime]$StartTime
    [datetime]$EndTime
    [bool]$Started = $false
    
    [void] Start() {
        $this.StartTime = Get-Date
        $this.Started = $true
    }
    
    [void] Stop() {
        $this.EndTime = Get-Date
        $this.Started = $false
    }
    
    [void] RecordRequest([double]$LatencyMs, [int]$StatusCode, [string]$ErrorType = $null) {
        if (-not $this.Started) { return }
        $this.Requests.Add(@{
            Timestamp = Get-Date
            Latency = $LatencyMs
            StatusCode = $StatusCode
            Error = $ErrorType
        }) | Out-Null
    }
    
    [void] RecordMemory() {
        if (-not $this.Started) { return }
        $mem = Get-Process -Id $PID
        $this.MemorySnapshots.Add(@{
            Timestamp = Get-Date
            WorkingSet = $mem.WorkingSet64 / 1MB
        }) | Out-Null
    }
    
    [hashtable] GetStats() {
        if ($this.Requests.Count -eq 0) {
            return @{
                TotalRequests = 0
                SuccessfulRequests = 0
                FailedRequests = 0
                ErrorRate = 0
                Throughput = 0
                Latencies = @{}
                Memory = @{}
            }
        }
        
        $duration = ($this.EndTime - $this.StartTime).TotalSeconds
        if ($duration -eq 0) { $duration = 1 }
        
        $successful = @($this.Requests | Where-Object { $_.Error -eq $null }).Count
        $failed = $this.Requests.Count - $successful
        
        $latencies = @($this.Requests | Select-Object -ExpandProperty Latency | Sort-Object)
        
        $p50idx = [math]::Floor($latencies.Count * 0.5)
        $p95idx = [math]::Floor($latencies.Count * 0.95)
        $p99idx = [math]::Floor($latencies.Count * 0.99)
        $p999idx = [math]::Floor($latencies.Count * 0.999)
        
        return @{
            TotalRequests = $this.Requests.Count
            SuccessfulRequests = $successful
            FailedRequests = $failed
            ErrorRate = if ($this.Requests.Count -gt 0) { [math]::Round(($failed / $this.Requests.Count * 100), 2) } else { 0 }
            Throughput = [math]::Round(($this.Requests.Count / $duration), 0)
            Latencies = @{
                Min = $latencies[0]
                Max = $latencies[-1]
                Avg = [math]::Round(($latencies | Measure-Object -Average).Average, 2)
                P50 = $latencies[$p50idx]
                P95 = $latencies[$p95idx]
                P99 = $latencies[$p99idx]
                P999 = $latencies[$p999idx]
            }
            Memory = @{
                MaxMB = if ($this.MemorySnapshots.Count -gt 0) { [math]::Round(($this.MemorySnapshots | Measure-Object -Property WorkingSet -Maximum).Maximum, 2) } else { 0 }
            }
            Duration = $duration
        }
    }
}

# Request Simulator
class RequestSimulator {
    [int]$RPS
    [int]$Duration
    [double]$ErrorRate
    [MetricsCollector]$Collector
    [int]$RequestCount = 0
    [bool]$Running = $false
    
    RequestSimulator([int]$RPS, [int]$Duration, [double]$ErrorRate) {
        $this.RPS = $RPS
        $this.Duration = $Duration
        $this.ErrorRate = $ErrorRate
        $this.Collector = [MetricsCollector]::new()
    }
    
    [double] SimulateRequest() {
        $startTime = [datetime]::Now
        
        try {
            $rand = [System.Random]::new()
            if ($rand.NextDouble() -lt $this.ErrorRate) {
                $this.Collector.RecordRequest(0, 500, "network_timeout")
                return 0
            }
            
            $operationType = @("cache", "db", "compute")[$rand.Next(3)]
            $latency = $this._SimulateOperation($operationType)
            
            $this.Collector.RecordRequest($latency, 200)
            $this.RequestCount++
            
            return $latency
        }
        catch {
            $this.Collector.RecordRequest(0, 500, "error")
            return 0
        }
    }
    
    [double] _SimulateOperation([string]$Type) {
        $random = [System.Random]::new()
        $val = $random.NextDouble()
        
        if ($Type -eq "cache") {
            if ($val -lt 0.6) {
                $ms = 1 + [int]($random.NextDouble() * 19)
                Start-Sleep -Milliseconds $ms
                return $ms
            } elseif ($val -lt 0.9) {
                $ms = 20 + [int]($random.NextDouble() * 30)
                Start-Sleep -Milliseconds $ms
                return $ms
            } else {
                $ms = 50 + [int]($random.NextDouble() * 50)
                Start-Sleep -Milliseconds $ms
                return $ms
            }
        } elseif ($Type -eq "db") {
            if ($val -lt 0.5) {
                $ms = 10 + [int]($random.NextDouble() * 90)
                Start-Sleep -Milliseconds $ms
                return $ms
            } elseif ($val -lt 0.8) {
                $ms = 100 + [int]($random.NextDouble() * 100)
                Start-Sleep -Milliseconds $ms
                return $ms
            } else {
                $ms = 200 + [int]($random.NextDouble() * 100)
                Start-Sleep -Milliseconds $ms
                return $ms
            }
        } else {
            $duration = 50 + [int]($random.NextDouble() * 150)
            $endTime = (Get-Date).AddMilliseconds($duration)
            while ((Get-Date) -lt $endTime) {
                [System.Math]::Sqrt($random.NextDouble()) | Out-Null
            }
            return $duration
        }
    }
    
    [void] RunLoadTest() {
        Write-Host "`n[LOAD TEST] Starting load test..."
        Write-Host "  Target RPS: $($this.RPS)"
        Write-Host "  Duration: $($this.Duration)s"
        Write-Host "  Network Error Rate: $($this.ErrorRate * 100)%"
        
        $this.Collector.Start()
        $this.Running = $true
        
        $startTime = Get-Date
        $intervalMs = 1000 / $this.RPS
        $lastRequestTime = $startTime
        
        while ($this.Running) {
            $now = Get-Date
            if (($now - $startTime).TotalSeconds -ge $this.Duration) {
                $this.Running = $false
                break
            }
            
            $msElapsed = ($now - $lastRequestTime).TotalMilliseconds
            $requestsToQueue = [math]::Max(1, [int]($msElapsed / $intervalMs))
            
            for ($i = 0; $i -lt $requestsToQueue; $i++) {
                [void]$this.SimulateRequest()
            }
            
            if (($now - $lastRequestTime).TotalMilliseconds -ge 5000) {
                $this.Collector.RecordMemory()
            }
            
            $lastRequestTime = $now
            Start-Sleep -Milliseconds 1
        }
        
        $this.Collector.Stop()
    }
    
    [hashtable] GetStats() {
        $stats = $this.Collector.GetStats()
        $stats['TargetRPS'] = $this.RPS
        return $stats
    }
}

# Load Test Coordinator
class LoadTestCoordinator {
    [string]$OutputDir
    [System.Collections.ArrayList]$Results = @()
    [int[]]$LoadLevels = @(100, 500, 1000, 5000)
    [int]$TestDuration
    [double]$NetworkErrorRate
    
    LoadTestCoordinator([string]$OutputDir, [int]$Duration, [double]$ErrorRate) {
        $this.OutputDir = $OutputDir
        $this.TestDuration = $Duration
        $this.NetworkErrorRate = $ErrorRate
    }
    
    [void] RunFullTest() {
        Write-Host ("`n" + ("="*80))
        Write-Host "HELIOS v4.0 - EXPERIMENT 7: LOAD TESTING & SCALABILITY LIMITS"
        Write-Host ("="*80)
        
        foreach ($rps in $this.LoadLevels) {
            $this.RunLoadLevel($rps)
        }
    }
    
    [void] RunLoadLevel([int]$RPS) {
        $padding = "--"*8
        Write-Host ("`n" + $padding + " LOAD LEVEL: $RPS req/sec " + $padding)
        
        $simulator = [RequestSimulator]::new($RPS, $this.TestDuration, $this.NetworkErrorRate)
        $simulator.RunLoadTest()
        
        $stats = $simulator.GetStats()
        $this.Results.Add($stats) | Out-Null
        
        $this._PrintResults($RPS, $stats)
    }
    
    [void] _PrintResults([int]$RPS, [hashtable]$Stats) {
        Write-Host "`n✓ Test Complete: $RPS req/sec"
        Write-Host ("  Requests: " + $Stats.TotalRequests + " total (" + $Stats.SuccessfulRequests + " success, " + $Stats.FailedRequests + " failed)")
        Write-Host ("  Throughput: " + $Stats.Throughput + " req/sec (requested: $RPS)")
        Write-Host ("  Error Rate: " + $Stats.ErrorRate + "%")
        Write-Host "  Latency:"
        Write-Host ("    - Min: " + $Stats.Latencies.Min + "ms")
        Write-Host ("    - Avg: " + $Stats.Latencies.Avg + "ms")
        Write-Host ("    - p50: " + $Stats.Latencies.P50 + "ms")
        Write-Host ("    - p95: " + $Stats.Latencies.P95 + "ms")
        Write-Host ("    - p99: " + $Stats.Latencies.P99 + "ms")
        if ($Stats.Memory.MaxMB -gt 0) {
            Write-Host "  Memory:"
            Write-Host ("    - Max: " + $Stats.Memory.MaxMB + "MB")
        }
    }
    
    [void] ExportResults() {
        New-Item -ItemType Directory -Path $this.OutputDir -Force | Out-Null
        
        # JSON
        $jsonPath = Join-Path $this.OutputDir "load-test-results.json"
        $this.Results | ConvertTo-Json | Set-Content $jsonPath
        Write-Host ("`n✓ Results exported to: " + $jsonPath)
        
        # CSV
        $this._ExportCSV()
        
        # Analysis
        $this._GenerateAnalysis()
        
        # Dashboard
        $this._GenerateDashboard()
    }
    
    [void] _ExportCSV() {
        $csvPath = Join-Path $this.OutputDir "load-curve.csv"
        
        $headers = "Load Level (req/sec),Total Requests,Successful Requests,Failed Requests,Error Rate (%),Actual Throughput (req/sec),Min Latency (ms),Avg Latency (ms),p50 Latency (ms),p95 Latency (ms),p99 Latency (ms),Max Memory (MB)"
        
        $csv = @($headers)
        
        foreach ($result in $this.Results) {
            $row = "$($result.TargetRPS),$($result.TotalRequests),$($result.SuccessfulRequests),$($result.FailedRequests),$($result.ErrorRate),$($result.Throughput),$($result.Latencies.Min),$($result.Latencies.Avg),$($result.Latencies.P50),$($result.Latencies.P95),$($result.Latencies.P99),$($result.Memory.MaxMB)"
            $csv += $row
        }
        
        $csv | Set-Content $csvPath
        Write-Host ("✓ CSV exported to: " + $csvPath)
    }
    
    [void] _GenerateAnalysis() {
        $reportPath = Join-Path $this.OutputDir "breaking-point-analysis.md"
        
        $loadLevelsList = ($this.Results | ForEach-Object { $_.TargetRPS } | Join-String -Separator ", ")
        
        $report = "# HELIOS v4.0 - Load Testing Analysis Report`n`n"
        $report += "## Executive Summary`n`n"
        $report += "This report analyzes system behavior under increasing load levels.`n`n"
        $report += "## Test Configuration`n`n"
        $report += "- Test Duration: $($this.TestDuration) seconds per load level`n"
        $report += "- Network Error Rate: $($this.NetworkErrorRate * 100)%`n"
        $report += "- Load Levels Tested: $loadLevelsList req/sec`n`n"
        $report += "## Key Findings`n`n"
        
        $breakingPoint = $null
        foreach ($result in $this.Results) {
            if ($result.ErrorRate -gt 50) {
                $breakingPoint = $result.TargetRPS
                break
            }
        }
        
        if ($breakingPoint) {
            $report += "System breaking point detected at approximately $breakingPoint req/sec`n`n"
        } else {
            $report += "System remained stable across all tested load levels`n`n"
        }
        
        $report += "## Detailed Results`n`n"
        foreach ($result in $this.Results) {
            $report += "### Load Level: $($result.TargetRPS) req/sec`n`n"
            $report += "| Metric | Value |`n"
            $report += "|--------|-------|`n"
            $report += "| Total Requests | $($result.TotalRequests) |`n"
            $successRate = [math]::Round(100 - $result.ErrorRate, 2)
            $report += "| Success Rate | $successRate% |`n"
            $report += "| Actual Throughput | $($result.Throughput) req/sec |`n"
            $report += "| p99 Latency | $($result.Latencies.P99)ms |`n`n"
        }
        
        $report | Set-Content $reportPath
        Write-Host ("✓ Analysis report generated: " + $reportPath)
    }
    
    [void] _GenerateDashboard() {
        $dashboardPath = Join-Path $this.OutputDir "load-test-dashboard.html"
        
        $loadLevels = ConvertTo-Json @($this.Results | ForEach-Object { $_.TargetRPS })
        $throughputs = ConvertTo-Json @($this.Results | ForEach-Object { $_.Throughput })
        $p99Latencies = ConvertTo-Json @($this.Results | ForEach-Object { $_.Latencies.P99 })
        $errorRates = ConvertTo-Json @($this.Results | ForEach-Object { $_.ErrorRate })
        
        $tableRows = ""
        foreach ($result in $this.Results) {
            $successRate = [math]::Round(100 - $result.ErrorRate, 2)
            $tableRows += "                    <tr><td>$($result.TargetRPS) req/sec</td><td>$($result.TotalRequests)</td><td>$successRate`%</td><td>$($result.Latencies.P99)ms</td></tr>`n"
        }
        
        $html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>HELIOS v4.0 Load Testing</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js@3.9.1"><\/script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: sans-serif;
            background: linear-gradient(135deg, #1e1e2e 0%, #2d2d44 100%);
            color: #e0e0e0;
            padding: 40px 20px;
        }
        .container { max-width: 1400px; margin: 0 auto; }
        header { text-align: center; margin-bottom: 40px; border-bottom: 2px solid #00d4ff; padding-bottom: 20px; }
        h1 { color: #00d4ff; font-size: 2.5em; margin-bottom: 10px; }
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(500px, 1fr)); gap: 30px; margin-bottom: 40px; }
        .card { background: rgba(255, 255, 255, 0.05); border: 1px solid rgba(0, 212, 255, 0.2); border-radius: 8px; padding: 20px; }
        .card h2 { color: #00d4ff; margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid rgba(0, 212, 255, 0.1); }
        th { background: rgba(0, 212, 255, 0.1); color: #00d4ff; }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>⚡ HELIOS v4.0 Load Testing Results</h1>
            <p style="color: #888;">Experiment 7: Scalability Limits</p>
        </header>
        <div class="grid">
            <div class="card">
                <h2>Throughput</h2>
                <canvas id="throughputChart"><\/canvas>
            </div>
            <div class="card">
                <h2>p99 Latency</h2>
                <canvas id="latencyChart"><\/canvas>
            </div>
            <div class="card">
                <h2>Error Rate</h2>
                <canvas id="errorChart"><\/canvas>
            </div>
        </div>
        <div class="card">
            <h2>Results</h2>
            <table>
                <thead><tr><th>Load Level</th><th>Requests</th><th>Success Rate</th><th>p99 Latency</th></tr></thead>
                <tbody>
$tableRows                </tbody>
            </table>
        </div>
    </div>
    <script>
        new Chart(document.getElementById('throughputChart').getContext('2d'), {
            type: 'line',
            data: { labels: $loadLevels, datasets: [{ label: 'Throughput', data: $throughputs, borderColor: '#00d4ff', fill: true, backgroundColor: 'rgba(0, 212, 255, 0.1)', tension: 0.4 }] },
            options: { responsive: true, plugins: { legend: { labels: { color: '#e0e0e0' } } }, scales: { y: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } }, x: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } } } }
        });
        new Chart(document.getElementById('latencyChart').getContext('2d'), {
            type: 'line',
            data: { labels: $loadLevels, datasets: [{ label: 'p99 Latency', data: $p99Latencies, borderColor: '#ff4444', fill: true, backgroundColor: 'rgba(255, 68, 68, 0.1)', tension: 0.4 }] },
            options: { responsive: true, plugins: { legend: { labels: { color: '#e0e0e0' } } }, scales: { y: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } }, x: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } } } }
        });
        new Chart(document.getElementById('errorChart').getContext('2d'), {
            type: 'bar',
            data: { labels: $loadLevels, datasets: [{ label: 'Error Rate', data: $errorRates, backgroundColor: '#ffaa00' }] },
            options: { responsive: true, plugins: { legend: { labels: { color: '#e0e0e0' } } }, scales: { y: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } }, x: { ticks: { color: '#aaa' }, grid: { color: 'rgba(0, 212, 255, 0.1)' } } } }
        });
    </script>
</body>
</html>
"@
        
        $html | Set-Content $dashboardPath
        Write-Host ("✓ Dashboard generated: " + $dashboardPath)
    }
}

# Main
function Invoke-LoadTesting {
    Write-Host ("`n" + ("="*80))
    Write-Host ("█" + (" "*78) + "█")
    Write-Host ("█" + "  HELIOS v4.0 - EXPERIMENT 7: LOAD TESTING & SCALABILITY LIMITS".PadRight(79) + "█")
    Write-Host ("█" + (" "*78) + "█")
    Write-Host ("█" + ("="*78) + "█")
    Write-Host "`n📊 Objective: Determine system breaking points"
    Write-Host "📋 Configuration:"
    Write-Host "   - Load Levels: 100, 500, 1,000, 5,000 req/sec"
    Write-Host ("   - Test Duration: $TestDuration seconds per level")
    Write-Host ("   - Network Error Rate: $($NetworkErrorRate * 100)%`n")
    
    try {
        $coordinator = [LoadTestCoordinator]::new($OutputDir, $TestDuration, $NetworkErrorRate)
        
        $startTime = Get-Date
        $coordinator.RunFullTest()
        $coordinator.ExportResults()
        $duration = ((Get-Date) - $startTime).TotalMinutes
        
        Write-Host ("`n" + ("="*80))
        Write-Host "✓ EXPERIMENT 7 COMPLETE"
        Write-Host ("="*80)
        Write-Host "`n📁 Deliverables:"
        Write-Host "   - load-curve.csv"
        Write-Host "   - breaking-point-analysis.md"
        Write-Host "   - load-test-dashboard.html"
        Write-Host "   - load-test-results.json"
        Write-Host ("`n📈 Duration: " + [math]::Round($duration, 1) + " minutes")
        Write-Host ("📂 Location: " + $OutputDir)
        Write-Host ("`n" + ("="*80) + "`n")
    }
    catch {
        Write-Host ("`n✗ Error: " + $_) -ForegroundColor Red
        exit 1
    }
}

Invoke-LoadTesting
