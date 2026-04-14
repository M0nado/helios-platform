# Master Orchestrator: Run all parallelism levels and collect metrics

$baseDir = "C:\helios-v4\experiments\parallelism-overhead"
$results = @()

Write-Host "=== HELIOS Parallelism Overhead Analysis ==="
Write-Host "Starting experiment at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n"

# Run each level
$levels = @(
    @{name = "1-sequential"; label = "Sequential (1 agent)"},
    @{name = "2x-parallel"; label = "2x Parallel (2 agents)"},
    @{name = "4x-parallel"; label = "4x Parallel (4 agents)"},
    @{name = "8x-parallel"; label = "8x Parallel (8 agents)"},
    @{name = "16x-parallel"; label = "16x Parallel (16 agents)"}
)

foreach ($level in $levels) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Running: $($level.label)"
    
    $testScript = Join-Path $baseDir $level.name "run-test.ps1"
    $result = & $testScript
    
    $results += @{
        level = $level.name
        label = $level.label
        agents = $result.agents
        modules_per_agent = $result.modules_per_agent
        total_time_ms = $result.total_time_ms
        coordination_ms = $result.coordination.total_ms
        setup_ms = $result.coordination.setup_ms
        execution_ms = $result.coordination.execution_ms
        teardown_ms = $result.coordination.teardown_ms
    }
    
    Write-Host "  ✓ Completed in $($result.total_time_ms) ms`n"
}

# Calculate metrics
$baseline = $results[0].total_time_ms
$metrics = @()

foreach ($result in $results) {
    $speedup = [Math]::Round($baseline / $result.total_time_ms, 3)
    $efficiency = [Math]::Round(($speedup / $result.agents) * 100, 1)
    $agentHours = [Math]::Round(($result.total_time_ms / 1000) * $result.agents / 3600, 4)
    
    $metrics += @{
        agents = $result.agents
        level = $result.label
        time_ms = $result.total_time_ms
        speedup = $speedup
        efficiency_percent = $efficiency
        agent_hours = $agentHours
        setup_ms = $result.setup_ms
        coordination_ms = $result.coordination_ms
        teardown_ms = $result.teardown_ms
    }
}

# Export metrics CSV
$csvPath = Join-Path $baseDir "METRICS.csv"
$metrics | Select-Object agents, level, time_ms, speedup, efficiency_percent, agent_hours, setup_ms, coordination_ms, teardown_ms |
    ConvertTo-Csv -NoTypeInformation | Out-File $csvPath -Force

Write-Host "=== RESULTS ==="
Write-Host ""
Write-Host "Parallelism Level | Time (ms) | Speedup | Efficiency | Setup | Overhead"
Write-Host "$(('─' * 80))"
foreach ($m in $metrics) {
    $padding = " " * (20 - $m.agents.ToString().Length)
    $timeStr = "$($m.time_ms)".PadRight(10)
    $speedupStr = "$($m.speedup)x".PadRight(10)
    $effStr = "$($m.efficiency_percent)%".PadRight(12)
    $setupStr = "$($m.setup_ms)ms".PadRight(10)
    $coordStr = "$($m.coordination_ms)ms"
    Write-Host "$($m.agents)-agent$padding | $timeStr | $speedupStr | $effStr | $setupStr | $coordStr"
}

Write-Host ""
Write-Host "Metrics saved to: $csvPath"

# Export metrics for visualization
$metricsJson = @{
    baseline_ms = $baseline
    levels = $metrics
    timestamp = (Get-Date -Format "o")
}

$jsonPath = Join-Path $baseDir "measurements.json"
$metricsJson | ConvertTo-Json | Out-File $jsonPath -Force

Write-Host "Measurements saved to: $jsonPath"

$metricsJson
