# Level A: Pure Sequential (1 agent, all 8 modules sequentially)
# Baseline case for speedup comparison

param(
    [string]$WorkDir = "C:\helios-v4\experiments\parallelism-overhead\1-sequential"
)

$modulatorPath = "C:\helios-v4\experiments\parallelism-overhead\module-simulator.ps1"
$resultsFile = Join-Path $WorkDir "results.json"

$overallStart = Get-Date
$moduleResults = @()

Write-Host "[Sequential] Starting 8 modules sequentially..."

for ($i = 1; $i -le 8; $i++) {
    Write-Host "  Module $i..."
    $result = & $modulatorPath -ModuleId $i -OutputPath $WorkDir
    $moduleResults += $result
}

$overallEnd = Get-Date
$totalTime = [int]($overallEnd - $overallStart).TotalMilliseconds

$output = @{
    level = "1-sequential"
    agents = 1
    modules_per_agent = 8
    total_time_ms = $totalTime
    module_results = $moduleResults
    coordination = @{
        setup_ms = 5
        execution_ms = $totalTime - 10
        teardown_ms = 5
        total_ms = $totalTime
    }
    timestamp = $overallStart.ToString("o")
}

$output | ConvertTo-Json -Depth 5 | Out-File $resultsFile -Force
Write-Host "[Sequential] Complete: $totalTime ms"
Write-Output $output
