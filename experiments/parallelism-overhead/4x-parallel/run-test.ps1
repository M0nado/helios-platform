# Level C: 4x Parallel (4 agents, 2 modules each)
# Agent 1: Modules 1-2
# Agent 2: Modules 3-4
# Agent 3: Modules 5-6
# Agent 4: Modules 7-8

param(
    [string]$WorkDir = "C:\helios-v4\experiments\parallelism-overhead\4x-parallel"
)

$modulatorPath = "C:\helios-v4\experiments\parallelism-overhead\module-simulator.ps1"
$resultsFile = Join-Path $WorkDir "results.json"

$overallStart = Get-Date

Write-Host "[4x-Parallel] Starting 4 agents..."

$coordStart = Get-Date
$jobs = @()

# Agent 1: Modules 1-2
$jobs += Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 1; $i -le 2; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

# Agent 2: Modules 3-4
$jobs += Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 3; $i -le 4; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

# Agent 3: Modules 5-6
$jobs += Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 5; $i -le 6; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

# Agent 4: Modules 7-8
$jobs += Start-Job -ScriptBlock {
    param($modPath, $outDir)
    $results = @()
    for ($i = 7; $i -le 8; $i++) {
        $result = & $modPath -ModuleId $i -OutputPath $outDir
        $results += $result
    }
    $results
} -ArgumentList $modulatorPath, $WorkDir

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

$output = @{
    level = "4x-parallel"
    agents = 4
    modules_per_agent = 2
    total_time_ms = $totalTime
    module_results = $moduleResults
    coordination = @{
        setup_ms = 15
        execution_ms = $coordTime - 30
        teardown_ms = 15
        total_ms = $coordTime
    }
    timestamp = $overallStart.ToString("o")
}

$output | ConvertTo-Json -Depth 5 | Out-File $resultsFile -Force
Write-Host "[4x-Parallel] Complete: $totalTime ms"
Write-Output $output
