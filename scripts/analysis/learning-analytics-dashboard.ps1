#=============================================================================
# LEARNING ANALYTICS DASHBOARD
# ============================================================================
# Real-time analytics dashboard for monitoring and visualizing system learning.
#=============================================================================

param(
    [string]$LogDirectory = 'C:\Users\ADMIN\helios-platform\data\logs',
    [string]$LearnDirectory = 'C:\Users\ADMIN\helios-platform\data\learning'
)

function Analyze-ExecutionLogs {
    <#
    .SYNOPSIS
    Analyze execution logs and generate analytics.
    #>
    param([string]$LogPath)
    
    if (-not (Test-Path $LogPath)) {
        Write-Host "Log file not found: $LogPath" -ForegroundColor Red
        return $null
    }
    
    $logs = @()
    $lines = Get-Content $LogPath -ErrorAction SilentlyContinue | Select-Object -Skip 1
    
    foreach ($line in $lines) {
        if ($line.Trim()) {
            $parts = $line -split ','
            if ($parts.Count -ge 8) {
                $logs += [PSCustomObject]@{
                    TaskID = $parts[0]
                    AgentID = $parts[1]
                    ModelID = $parts[2]
                    TaskType = $parts[3]
                    Cost = [decimal]$parts[4]
                    ExecutionTime = [int]$parts[5]
                    SuccessScore = [int]$parts[6]
                    Status = $parts[7]
                }
            }
        }
    }
    
    return $logs
}

function Generate-AnalyticsReport {
    <#
    .SYNOPSIS
    Generate comprehensive analytics report.
    #>
    param([array]$ExecutionData)
    
    if (-not $ExecutionData -or $ExecutionData.Count -eq 0) {
        Write-Host "No execution data to analyze" -ForegroundColor Yellow
        return
    }
    
    Write-Host "`n" + ("=" * 120) -ForegroundColor Cyan
    Write-Host "LEARNING ANALYTICS DASHBOARD" -ForegroundColor Green
    Write-Host ("=" * 120) + "`n" -ForegroundColor Cyan
    
    # Overall metrics
    $totalTasks = $ExecutionData.Count
    $successful = ($ExecutionData | Where-Object { $_.Status -eq 'Success' }).Count
    $failed = $totalTasks - $successful
    $successRate = ($successful / $totalTasks) * 100
    $avgScore = ($ExecutionData | Measure-Object -Property SuccessScore -Average).Average
    $totalCost = ($ExecutionData | Measure-Object -Property Cost -Sum).Sum
    $avgLatency = ($ExecutionData | Measure-Object -Property ExecutionTime -Average).Average
    
    Write-Host "[OVERALL METRICS]" -ForegroundColor Yellow
    Write-Host "  Total Tasks: $totalTasks" -ForegroundColor Cyan
    Write-Host "  Successful: $successful ($([Math]::Round($successRate, 1))%)" -ForegroundColor Green
    Write-Host "  Failed: $failed ($([Math]::Round((($failed/$totalTasks)*100), 1))%)" -ForegroundColor Red
    Write-Host "  Average Score: $([Math]::Round($avgScore, 1))%" -ForegroundColor Cyan
    Write-Host "  Total Cost: \$$([Math]::Round($totalCost, 4))" -ForegroundColor Cyan
    Write-Host "  Average Latency: $([Math]::Round($avgLatency, 0))ms" -ForegroundColor Cyan
    
    # Agent analysis
    Write-Host "`n[AGENT PERFORMANCE]" -ForegroundColor Yellow
    $agentMetrics = $ExecutionData | Group-Object AgentID | ForEach-Object {
        $agent = $_.Name
        $data = $_.Group
        $agentSuccess = ($data | Where-Object { $_.Status -eq 'Success' }).Count
        @{
            Agent = $agent
            Tasks = $data.Count
            SuccessRate = ($agentSuccess / $data.Count) * 100
            AvgScore = ($data | Measure-Object -Property SuccessScore -Average).Average
            AvgCost = ($data | Measure-Object -Property Cost -Average).Average
            AvgLatency = ($data | Measure-Object -Property ExecutionTime -Average).Average
        }
    }
    
    $agentMetrics | Sort-Object SuccessRate -Descending | Select-Object -First 5 | ForEach-Object {
        Write-Host "  ★ $($_.Agent): $([Math]::Round($_.SuccessRate, 1))% success | Score $([Math]::Round($_.AvgScore, 1))% | Cost \$$([Math]::Round($_.AvgCost, 4)) | Latency $([Math]::Round($_.AvgLatency, 0))ms" -ForegroundColor Green
    }
    
    # Model analysis
    Write-Host "`n[MODEL PERFORMANCE]" -ForegroundColor Yellow
    $modelMetrics = $ExecutionData | Group-Object ModelID | ForEach-Object {
        $model = $_.Name
        $data = $_.Group
        $modelSuccess = ($data | Where-Object { $_.Status -eq 'Success' }).Count
        @{
            Model = $model
            Tasks = $data.Count
            SuccessRate = ($modelSuccess / $data.Count) * 100
            AvgScore = ($data | Measure-Object -Property SuccessScore -Average).Average
            AvgLatency = ($data | Measure-Object -Property ExecutionTime -Average).Average
            AvgCost = ($data | Measure-Object -Property Cost -Average).Average
        }
    }
    
    $modelMetrics | Sort-Object SuccessRate -Descending | Select-Object -First 5 | ForEach-Object {
        Write-Host "  ★ $($_.Model): $([Math]::Round($_.SuccessRate, 1))% success | Score $([Math]::Round($_.AvgScore, 1))% | Latency $([Math]::Round($_.AvgLatency, 0))ms" -ForegroundColor Green
    }
    
    # Task type analysis
    Write-Host "`n[TASK TYPE PERFORMANCE]" -ForegroundColor Yellow
    $taskMetrics = $ExecutionData | Group-Object TaskType | ForEach-Object {
        $task = $_.Name
        $data = $_.Group
        $taskSuccess = ($data | Where-Object { $_.Status -eq 'Success' }).Count
        @{
            TaskType = $task
            Tasks = $data.Count
            SuccessRate = ($taskSuccess / $data.Count) * 100
            AvgScore = ($data | Measure-Object -Property SuccessScore -Average).Average
            AvgCost = ($data | Measure-Object -Property Cost -Average).Average
        }
    }
    
    $taskMetrics | Sort-Object SuccessRate -Descending | ForEach-Object {
        Write-Host "  ★ $($_.TaskType): $([Math]::Round($_.SuccessRate, 1))% success | Score $([Math]::Round($_.AvgScore, 1))% | Cost \$$([Math]::Round($_.AvgCost, 4))" -ForegroundColor Green
    }
    
    # Cost-efficiency analysis
    Write-Host "`n[COST EFFICIENCY]" -ForegroundColor Yellow
    $costEfficiency = $ExecutionData | Group-Object { "$($_.AgentID)-$($_.ModelID)" } | ForEach-Object {
        $pair = $_.Name
        $data = $_.Group
        @{
            Pairing = $pair
            Tasks = $data.Count
            AvgScore = ($data | Measure-Object -Property SuccessScore -Average).Average
            AvgCost = ($data | Measure-Object -Property Cost -Average).Average
            Efficiency = ($data | Measure-Object -Property SuccessScore -Average).Average / ($data | Measure-Object -Property Cost -Average).Average
        }
    }
    
    $costEfficiency | Sort-Object Efficiency -Descending | Select-Object -First 5 | ForEach-Object {
        Write-Host "  ✓ $($_.Pairing): Efficiency $([Math]::Round($_.Efficiency, 2)) | Score $([Math]::Round($_.AvgScore, 1))% | Cost \$$([Math]::Round($_.AvgCost, 4))" -ForegroundColor Cyan
    }
    
    # Recommendations
    Write-Host "`n[AI-DRIVEN RECOMMENDATIONS]" -ForegroundColor Yellow
    
    # Best performer
    $best = $agentMetrics | Sort-Object SuccessRate -Descending | Select-Object -First 1
    if ($best) {
        Write-Host "  1. AGENT DEPLOYMENT: Use $($best.Agent) as primary - $([Math]::Round($best.SuccessRate, 1))% success rate" -ForegroundColor Green
    }
    
    # Best model
    $bestModel = $modelMetrics | Sort-Object SuccessRate -Descending | Select-Object -First 1
    if ($bestModel) {
        Write-Host "  2. MODEL SELECTION: Prioritize $($bestModel.Model) - $([Math]::Round($bestModel.SuccessRate, 1))% success rate" -ForegroundColor Green
    }
    
    # Cost optimization
    $cheapestEffective = $costEfficiency | Where-Object { $_.AvgScore -gt 75 } | Sort-Object AvgCost | Select-Object -First 1
    if ($cheapestEffective) {
        Write-Host "  3. COST OPTIMIZATION: Deploy $($cheapestEffective.Pairing) - \$$([Math]::Round($cheapestEffective.AvgCost, 4))/task with $([Math]::Round($cheapestEffective.AvgScore, 1))% quality" -ForegroundColor Green
    }
    
    # Specialization
    $bestTask = $taskMetrics | Sort-Object SuccessRate -Descending | Select-Object -First 1
    if ($bestTask) {
        Write-Host "  4. SPECIALIZATION: Master $($bestTask.TaskType) tasks - $([Math]::Round($bestTask.SuccessRate, 1))% success" -ForegroundColor Green
    }
    
    Write-Host "`n" + ("=" * 120) -ForegroundColor Cyan
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host "`nLoading latest execution logs..." -ForegroundColor Yellow

$latestLog = Get-ChildItem "$LogDirectory\execution-*.log" -ErrorAction SilentlyContinue | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

if ($latestLog) {
    Write-Host "Processing: $($latestLog.Name)" -ForegroundColor Cyan
    $data = Analyze-ExecutionLogs -LogPath $latestLog.FullName
    
    if ($data) {
        Generate-AnalyticsReport -ExecutionData $data
    }
} else {
    Write-Host "No execution logs found. Run the orchestrator first." -ForegroundColor Yellow
}
