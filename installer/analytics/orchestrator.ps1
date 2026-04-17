<#
.SYNOPSIS
    HELIOS Analytics System - Master Orchestration Script
    
.DESCRIPTION
    Coordinates and executes the complete analytics pipeline.
    Manages all learning-engine, insight-generator, visualization, and ML modules.
    
.PARAMETER Mode
    Execution mode: 'full' (all), 'learning', 'insights', 'viz', 'ml', or 'quick'
    
.PARAMETER Parallel
    Run modules in parallel (faster but more resource-intensive)
    
.NOTES
    Version: 1.0
    Run this as the main entry point for analytics system
#>

param(
    [ValidateSet('full', 'learning', 'insights', 'viz', 'ml', 'quick')]
    [string]$Mode = 'quick',
    
    [switch]$Parallel,
    
    [int]$Timeout = 300
)

$ErrorActionPreference = "Continue"

class AnalyticsOrchestrator {
    [string]$RootPath
    [string]$Mode
    [bool]$Parallel
    [int]$Timeout
    [hashtable]$Results
    [datetime]$StartTime
    
    AnalyticsOrchestrator([string]$root, [string]$mode, [bool]$parallel, [int]$timeout) {
        $this.RootPath = $root
        $this.Mode = $mode
        $this.Parallel = $parallel
        $this.Timeout = $timeout
        $this.Results = @{}
        $this.StartTime = Get-Date
    }
    
    [void] ExecuteLearningEngine() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║          LEARNING ENGINE - Core Analytics & Extraction           ║" -ForegroundColor Cyan
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        
        $scripts = @(
            "pattern-extractor.ps1",
            "behavior-analyzer.ps1",
            "cost-analyzer.ps1",
            "performance-profiler.ps1",
            "reliability-tracker.ps1"
        )
        
        foreach ($script in $scripts) {
            $scriptPath = "$($this.RootPath)\learning-engine\$script"
            Write-Host "Executing: $script..." -ForegroundColor Yellow
            
            try {
                $result = & $scriptPath
                $this.Results["learning-$script"] = @{
                    status = "success"
                    timestamp = Get-Date
                }
                Write-Host "✓ $script completed" -ForegroundColor Green
            } catch {
                $this.Results["learning-$script"] = @{
                    status = "failed"
                    error = $_.Exception.Message
                    timestamp = Get-Date
                }
                Write-Host "✗ $script failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    [void] ExecuteInsightGenerator() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
        Write-Host "║       INSIGHT GENERATOR - Advanced Pattern Analysis              ║" -ForegroundColor Magenta
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
        
        $scripts = @(
            "synergy-detector.ps1",
            "bottleneck-finder.ps1",
            "optimization-suggester.ps1",
            "trend-analyzer.ps1",
            "anomaly-detector.ps1"
        )
        
        foreach ($script in $scripts) {
            $scriptPath = "$($this.RootPath)\insight-generator\$script"
            Write-Host "Executing: $script..." -ForegroundColor Yellow
            
            try {
                $result = & $scriptPath
                $this.Results["insight-$script"] = @{
                    status = "success"
                    timestamp = Get-Date
                }
                Write-Host "✓ $script completed" -ForegroundColor Green
            } catch {
                $this.Results["insight-$script"] = @{
                    status = "failed"
                    error = $_.Exception.Message
                    timestamp = Get-Date
                }
                Write-Host "✗ $script failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    [void] ExecuteVisualization() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Blue
        Write-Host "║          VISUALIZATION - Dashboards & Reports                    ║" -ForegroundColor Blue
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Blue
        
        $scripts = @(
            "dashboard-generator.ps1",
            "report-builder.ps1",
            "chart-renderer.ps1",
            "timeline-visualizer.ps1",
            "dependency-mapper.ps1"
        )
        
        foreach ($script in $scripts) {
            $scriptPath = "$($this.RootPath)\visualization\$script"
            Write-Host "Executing: $script..." -ForegroundColor Yellow
            
            try {
                $result = & $scriptPath
                $this.Results["viz-$script"] = @{
                    status = "success"
                    timestamp = Get-Date
                }
                Write-Host "✓ $script completed" -ForegroundColor Green
            } catch {
                $this.Results["viz-$script"] = @{
                    status = "failed"
                    error = $_.Exception.Message
                    timestamp = Get-Date
                }
                Write-Host "✗ $script failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    [void] ExecuteMachineLearning() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║       MACHINE LEARNING - Prediction & Optimization              ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        
        $scripts = @(
            "model-training.ps1",
            "prediction-engine.ps1",
            "recommendation-engine.ps1",
            "adaptive-optimizer.ps1",
            "feedback-loop.ps1"
        )
        
        foreach ($script in $scripts) {
            $scriptPath = "$($this.RootPath)\machine-learning\$script"
            Write-Host "Executing: $script..." -ForegroundColor Yellow
            
            try {
                $result = & $scriptPath
                $this.Results["ml-$script"] = @{
                    status = "success"
                    timestamp = Get-Date
                }
                Write-Host "✓ $script completed" -ForegroundColor Green
            } catch {
                $this.Results["ml-$script"] = @{
                    status = "failed"
                    error = $_.Exception.Message
                    timestamp = Get-Date
                }
                Write-Host "✗ $script failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    [void] ExecuteQuickMode() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
        Write-Host "║            QUICK MODE - Essential Analytics Only                 ║" -ForegroundColor Yellow
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
        
        # Essential scripts for quick assessment
        $quickScripts = @(
            @{ path = "learning-engine"; script = "performance-profiler.ps1" },
            @{ path = "learning-engine"; script = "reliability-tracker.ps1" },
            @{ path = "insight-generator"; script = "bottleneck-finder.ps1" },
            @{ path = "insight-generator"; script = "anomaly-detector.ps1" },
            @{ path = "machine-learning"; script = "prediction-engine.ps1" }
        )
        
        foreach ($item in $quickScripts) {
            $scriptPath = "$($this.RootPath)\$($item.path)\$($item.script)"
            Write-Host "Executing: $($item.script)..." -ForegroundColor Yellow
            
            try {
                $result = & $scriptPath
                $this.Results["quick-$($item.script)"] = @{
                    status = "success"
                    timestamp = Get-Date
                }
                Write-Host "✓ $($item.script) completed" -ForegroundColor Green
            } catch {
                $this.Results["quick-$($item.script)"] = @{
                    status = "failed"
                    error = $_.Exception.Message
                    timestamp = Get-Date
                }
                Write-Host "✗ $($item.script) failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    [void] PrintExecutionSummary() {
        $elapsed = (Get-Date) - $this.StartTime
        $successful = ($this.Results.Values | Where-Object { $_.status -eq "success" }).Count
        $failed = ($this.Results.Values | Where-Object { $_.status -eq "failed" }).Count
        $total = $this.Results.Count
        
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor White
        Write-Host "║           HELIOS ANALYTICS EXECUTION SUMMARY                      ║" -ForegroundColor White
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor White
        
        Write-Host "`nExecution Mode: $($this.Mode)" -ForegroundColor Cyan
        Write-Host "Total Scripts Executed: $total" -ForegroundColor Cyan
        Write-Host "Successful: $successful ✓" -ForegroundColor Green
        Write-Host "Failed: $failed ✗" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
        Write-Host "Total Execution Time: $([Math]::Round($elapsed.TotalSeconds, 1)) seconds" -ForegroundColor Cyan
        
        Write-Host "`nKey Deliverables:" -ForegroundColor Yellow
        Write-Host "  📊 Dashboard: C:\HELIOS\analytics\dashboards\analytics-dashboard.html" -ForegroundColor White
        Write-Host "  📄 Reports: C:\HELIOS\analytics\reports\" -ForegroundColor White
        Write-Host "  📈 Charts: C:\HELIOS\analytics\charts\" -ForegroundColor White
        Write-Host "  ⏱️  Timelines: C:\HELIOS\analytics\timelines\" -ForegroundColor White
        Write-Host "  🗺️  Maps: C:\HELIOS\analytics\dependencies\" -ForegroundColor White
        Write-Host "  🤖 Models: C:\HELIOS\analytics\models\" -ForegroundColor White
        Write-Host "  💾 Databases: C:\HELIOS\analytics\learning-database\" -ForegroundColor White
        
        Write-Host "`nNext Steps:" -ForegroundColor Cyan
        Write-Host "  1. View dashboard: Open analytics-dashboard.html in browser" -ForegroundColor White
        Write-Host "  2. Review reports: Check reports folder for detailed analysis" -ForegroundColor White
        Write-Host "  3. Implement recommendations: Use optimization-suggester output" -ForegroundColor White
        Write-Host "  4. Schedule regular runs: Set up Windows Task Scheduler" -ForegroundColor White
        
        Write-Host "`n" -ForegroundColor White
    }
    
    [void] Execute() {
        Write-Host "`n╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║     HELIOS PLATFORM - ANALYTICS SYSTEM ORCHESTRATOR v1.0         ║" -ForegroundColor Cyan
        Write-Host "║                Starting Analytics Pipeline...                     ║" -ForegroundColor Cyan
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        
        switch ($this.Mode) {
            'full' {
                $this.ExecuteLearningEngine()
                $this.ExecuteInsightGenerator()
                $this.ExecuteVisualization()
                $this.ExecuteMachineLearning()
            }
            'learning' { $this.ExecuteLearningEngine() }
            'insights' { $this.ExecuteInsightGenerator() }
            'viz' { $this.ExecuteVisualization() }
            'ml' { $this.ExecuteMachineLearning() }
            'quick' { $this.ExecuteQuickMode() }
        }
        
        $this.PrintExecutionSummary()
    }
}

# Main execution
$orchestrator = [AnalyticsOrchestrator]::new("C:\HELIOS\analytics", $Mode, $Parallel, $Timeout)
$orchestrator.Execute()
