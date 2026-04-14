# Module Simulator - Simulates building a module with realistic timing
# Each module: analysis (100ms) + processing (200ms) + codegen (150ms) = ~450ms base

param(
    [int]$ModuleId = 1,
    [string]$OutputPath = "."
)

$startTime = Get-Date
$moduleName = "Module_$ModuleId"

# Simulate realistic module work phases
function Measure-Phase {
    param([string]$Name, [int]$DurationMs)
    $phaseStart = Get-Date
    Start-Sleep -Milliseconds $DurationMs
    $phaseEnd = Get-Date
    return @{
        phase = $Name
        duration_ms = [int]($phaseEnd - $phaseStart).TotalMilliseconds
        module = $moduleName
    }
}

# Module work phases (realistic durations)
$phases = @(
    (Measure-Phase "analysis" 100),
    (Measure-Phase "dependency_resolution" 75),
    (Measure-Phase "processing" 200),
    (Measure-Phase "codegen" 150),
    (Measure-Phase "optimization" 80)
)

$endTime = Get-Date
$totalTime = [int]($endTime - $startTime).TotalMilliseconds

$result = @{
    module_id = $ModuleId
    module_name = $moduleName
    phases = $phases
    total_time_ms = $totalTime
    start_timestamp = $startTime.ToString("o")
    end_timestamp = $endTime.ToString("o")
}

$jsonPath = Join-Path $OutputPath "$moduleName.json"
$result | ConvertTo-Json | Out-File $jsonPath -Force

@{
    success = $true
    module = $moduleName
    time_ms = $totalTime
    output = $jsonPath
}
