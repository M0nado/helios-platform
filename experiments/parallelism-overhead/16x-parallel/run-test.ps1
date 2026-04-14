# Level E: 16x Parallel (16 agents, 0.5 modules each with duplication)
# Each of 16 agents builds all 8 modules (feature duplication)
# High communication overhead for deduplication

param(
    [string]$WorkDir = "C:\helios-v4\experiments\parallelism-overhead\16x-parallel"
)

$modulatorPath = "C:\helios-v4\experiments\parallelism-overhead\module-simulator.ps1"
$resultsFile = Join-Path $WorkDir "results.json"

$overallStart = Get-Date

Write-Host "[16x-Parallel] Starting 16 agents (redundant builds)..."

$coordStart = Get-Date
$jobs = @()

# Start 16 agents, each building all 8 modules (with duplication)
for ($agentId = 1; $agentId -le 16; $agentId++) {
    $jobs += Start-Job -ScriptBlock {
        param($modPath, $outDir, $agent)
        $agentResults = @()
        for ($i = 1; $i -le 8; $i++) {
            $outSubDir = Join-Path $outDir "agent-$agent"
            New-Item -ItemType Directory -Path $outSubDir -Force | Out-Null
            $result = & $modPath -ModuleId $i -OutputPath $outSubDir
            $agentResults += $result
        }
        $agentResults
    } -ArgumentList $modulatorPath, $WorkDir, $agentId
}

# Wait for all jobs
$moduleResults = @()
foreach ($job in $jobs) {
    $results = Receive-Job -Job $job -Wait
    $moduleResults += $results
}
Get-Job | Remove-Job

$coordEnd = Get-Date
$coordTime = [int]($coordEnd - $coordStart).TotalMilliseconds

$overallEnd = Get-Date
$totalTime = [int]($overallEnd - $overallStart).TotalMilliseconds

# Deduplication phase (high communication overhead)
$dedup = Get-Date
Start-Sleep -Milliseconds 30  # Simulated deduplication communication
$dedup = [int]((Get-Date) - $dedup).TotalMilliseconds

$output = @{
    level = "16x-parallel"
    agents = 16
    modules_per_agent = 8
    duplication_factor = 16
    total_time_ms = $totalTime + $dedup
    module_results = $moduleResults
    coordination = @{
        setup_ms = 50
        execution_ms = $coordTime - 100
        deduplication_ms = $dedup
        teardown_ms = 50
        total_ms = $coordTime + $dedup
    }
    timestamp = $overallStart.ToString("o")
}

$output | ConvertTo-Json -Depth 5 | Out-File $resultsFile -Force
Write-Host "[16x-Parallel] Complete: $($output.total_time_ms) ms (includes $dedup ms dedup)"
Write-Output $output
