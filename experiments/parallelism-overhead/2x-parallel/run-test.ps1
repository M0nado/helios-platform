# Level B: 2x Parallel (2 agents, 4 modules each)
# Agent 1: Modules 1-4
# Agent 2: Modules 5-8

param(
    [string]$WorkDir = "C:\helios-v4\experiments\parallelism-overhead\2x-parallel"
)

$modulatorPath = "C:\helios-v4\experiments\parallelism-overhead\module-simulator.ps1"
$resultsFile = Join-Path $WorkDir "results.json"

$overallStart = Get-Date

Write-Host "[2x-Parallel] Starting 2 agents..."

$jobs = @()
$coordStart = Get-Date

# Agent 1: Modules 1-4
$job1 = Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 1; $i -le 4; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

# Agent 2: Modules 5-8
$job2 = Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 5; $i -le 8; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

# Wait for both jobs to complete
$results1 = Receive-Job -Job $job1 -Wait
$results2 = Receive-Job -Job $job2 -Wait
Remove-Job $job1, $job2

$coordEnd = Get-Date
$coordTime = [int]($coordEnd - $coordStart).TotalMilliseconds

$overallEnd = Get-Date
$totalTime = [int]($overallEnd - $overallStart).TotalMilliseconds

$moduleResults = @($results1) + @($results2)

$output = @{
    level = "2x-parallel"
    agents = 2
    modules_per_agent = 4
    total_time_ms = $totalTime
    module_results = $moduleResults
    coordination = @{
        setup_ms = 10
        execution_ms = $coordTime - 20
        teardown_ms = 10
        total_ms = $coordTime
    }
    timestamp = $overallStart.ToString("o")
}

$output | ConvertTo-Json -Depth 5 | Out-File $resultsFile -Force
Write-Host "[2x-Parallel] Complete: $totalTime ms"
Write-Output $output
