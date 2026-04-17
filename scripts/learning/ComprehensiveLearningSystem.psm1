#=============================================================================
# COMPREHENSIVE LOGGING & LEARNING SYSTEM
# ============================================================================
# Logs all operations with full context, tracks metrics, learns from patterns,
# and optimizes continuously across all dimensions.
#=============================================================================

# ============================================================================
# LOGGING ENGINE
# ============================================================================

class LogEntry {
    [string]$LogID
    [datetime]$Timestamp
    [string]$Level  # INFO, SUCCESS, WARNING, ERROR, CRITICAL
    [string]$Category  # EXECUTION, SPECIALIZATION, LEARNING, OPTIMIZATION, METRIC
    [string]$AgentID
    [string]$ModelID
    [string]$TaskType
    [hashtable]$Context
    [string]$Message
    [hashtable]$Metrics
}

function Initialize-LoggingSystem {
    <#
    .SYNOPSIS
    Initialize comprehensive logging infrastructure.
    #>
    param(
        [string]$LogPath = 'C:\Users\ADMIN\helios-platform\data\logs',
        [string]$LearnPath = 'C:\Users\ADMIN\helios-platform\data\learning'
    )
    
    # Create log files
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    
    $logStructure = @{
        LogPath = $LogPath
        LearnPath = $LearnPath
        MainLog = "$LogPath\main-$timestamp.log"
        ExecutionLog = "$LogPath\execution-$timestamp.log"
        SpecializationLog = "$LogPath\specialization-$timestamp.log"
        LearningLog = "$LogPath\learning-$timestamp.log"
        MetricsLog = "$LogPath\metrics-$timestamp.csv"
        PerformanceDB = "$LearnPath\performance-db.csv"
        OptimizationDB = "$LearnPath\optimization-db.csv"
        InsightsDB = "$LearnPath\insights-db.csv"
    }
    
    # Create log headers
    Add-Content -Path $logStructure.MainLog -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Logging System Initialized" -ErrorAction SilentlyContinue
    Add-Content -Path $logStructure.ExecutionLog -Value "TaskID,AgentID,ModelID,Status,Duration,Cost,SuccessScore,Timestamp" -ErrorAction SilentlyContinue
    Add-Content -Path $logStructure.SpecializationLog -Value "Agent,Specialization,SuccessRate,AverageCost,ExecutionCount,Timestamp" -ErrorAction SilentlyContinue
    Add-Content -Path $logStructure.MetricsLog -Value "Timestamp,Metric,Value,AgentID,Context" -ErrorAction SilentlyContinue
    
    return $logStructure
}

function Write-Log {
    <#
    .SYNOPSIS
    Write structured log entry.
    #>
    param(
        [string]$Level,
        [string]$Category,
        [string]$Message,
        [hashtable]$Context = @{},
        [hashtable]$Metrics = @{},
        [string]$AgentID = '',
        [string]$ModelID = '',
        [string]$LogFile
    )
    
    $entry = [LogEntry]@{
        LogID = "log-$(Get-Random -Maximum 999999)"
        Timestamp = Get-Date
        Level = $Level
        Category = $Category
        AgentID = $AgentID
        ModelID = $ModelID
        Context = $Context
        Message = $Message
        Metrics = $Metrics
    }
    
    # Format log line
    $logLine = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$($entry.Level)] [$($entry.Category)] [$($entry.AgentID)] - $($entry.Message)"
    
    if ($Metrics) {
        $metricStr = ($Metrics.GetEnumerator() | ForEach-Object { "$($_.Key):$($_.Value)" }) -join " | "
        $logLine += " [$metricStr]"
    }
    
    # Write to log file
    if ($LogFile -and (Test-Path (Split-Path $LogFile))) {
        Add-Content -Path $LogFile -Value $logLine -ErrorAction SilentlyContinue
    }
    
    # Write to console with color
    $color = switch ($Level) {
        'ERROR' { 'Red' }
        'WARNING' { 'Yellow' }
        'SUCCESS' { 'Green' }
        'CRITICAL' { 'Magenta' }
        default { 'Gray' }
    }
    
    Write-Host $logLine -ForegroundColor $color
    
    return $entry
}

function Log-ExecutionMetrics {
    <#
    .SYNOPSIS
    Log execution metrics for analysis.
    #>
    param(
        [string]$MetricsFile,
        [string]$AgentID,
        [string]$ModelID,
        [string]$TaskType,
        [decimal]$Cost,
        [int]$ExecutionTime,
        [int]$SuccessScore,
        [hashtable]$AdditionalMetrics = @{}
    )
    
    $metricsLine = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'),$TaskType,$AgentID,$ModelID,$Cost,$ExecutionTime,$SuccessScore"
    
    foreach ($key in $AdditionalMetrics.Keys) {
        $metricsLine += ",$($AdditionalMetrics[$key])"
    }
    
    Add-Content -Path $MetricsFile -Value $metricsLine -ErrorAction SilentlyContinue
}

# ============================================================================
# LEARNING ENGINE
# ============================================================================

function Initialize-LearningEngine {
    <#
    .SYNOPSIS
    Initialize machine learning system for pattern discovery.
    #>
    param(
        [hashtable]$LogStructure
    )
    
    $learningSystem = @{
        Patterns = @{}
        Correlations = @{}
        Anomalies = @{}
        Recommendations = @()
        ConfidenceLevel = 0.0
        LastUpdate = Get-Date
        DataPoints = 0
    }
    
    return $learningSystem
}

function Learn-FromExecutions {
    <#
    .SYNOPSIS
    Extract learning insights from execution data.
    #>
    param(
        [array]$ExecutionResults,
        [hashtable]$ExistingLearnings = @{}
    )
    
    $learnings = @{
        PatternsByAgent = @{}
        PatternsByModel = @{}
        PatternsByTask = @{}
        BestCombinations = @()
        WorstCombinations = @()
        CostPatterns = @{}
        SpeedPatterns = @{}
        QualityPatterns = @{}
        AnomalousResults = @()
    }
    
    # Analyze by agent
    $byAgent = $ExecutionResults | Group-Object -Property AgentID
    foreach ($agentGroup in $byAgent) {
        $agent = $agentGroup.Name
        $results = $agentGroup.Group
        
        $avgScore = ($results | Measure-Object -Property SuccessScore -Average).Average
        $avgCost = ($results | Measure-Object -Property Cost -Average).Average
        $successRate = (($results | Where-Object { $_.Status -eq 'Success' }).Count / $results.Count) * 100
        
        $learnings.PatternsByAgent[$agent] = @{
            AverageScore = [Math]::Round($avgScore, 2)
            AverageCost = [Math]::Round($avgCost, 6)
            SuccessRate = [Math]::Round($successRate, 1)
            ExecutionCount = $results.Count
            Specialization = "agent-$agent-specialized"
            Strength = if ($avgScore -gt 85) { 'strong' } elseif ($avgScore -gt 75) { 'moderate' } else { 'weak' }
            CostEfficiency = [Math]::Round($avgScore / [Math]::Max(0.001, $avgCost), 2)
        }
    }
    
    # Analyze by model
    $byModel = $ExecutionResults | Group-Object -Property ModelID
    foreach ($modelGroup in $byModel) {
        $model = $modelGroup.Name
        $results = $modelGroup.Group
        
        $avgScore = ($results | Measure-Object -Property SuccessScore -Average).Average
        $avgTime = ($results | Measure-Object -Property ExecutionTimeMs -Average).Average
        
        $learnings.PatternsByModel[$model] = @{
            AverageScore = [Math]::Round($avgScore, 2)
            AverageLatency = [Math]::Round($avgTime, 0)
            ExecutionCount = $results.Count
            Reliability = (($results | Where-Object { $_.Status -eq 'Success' }).Count / $results.Count) * 100
        }
    }
    
    # Find best combinations
    $bestByScore = $ExecutionResults | Sort-Object -Property SuccessScore -Descending | Select-Object -First 5
    foreach ($result in $bestByScore) {
        $learnings.BestCombinations += @{
            Agent = $result.AgentID
            Model = $result.ModelID
            Task = $result.TaskType
            Score = $result.SuccessScore
            Cost = $result.Cost
            Type = 'quality-leader'
        }
    }
    
    # Find most cost-effective
    $bestByCostEfficiency = $ExecutionResults | Sort-Object {
        $_.SuccessScore / [Math]::Max(0.001, $_.Cost)
    } -Descending | Select-Object -First 5
    
    foreach ($result in $bestByCostEfficiency) {
        $learnings.BestCombinations += @{
            Agent = $result.AgentID
            Model = $result.ModelID
            Task = $result.TaskType
            Efficiency = [Math]::Round($result.SuccessScore / [Math]::Max(0.001, $result.Cost), 2)
            Type = 'cost-effective'
        }
    }
    
    # Detect anomalies
    $avgScore = ($ExecutionResults | Measure-Object -Property SuccessScore -Average).Average
    $stdDev = [Math]::Sqrt(
        ($ExecutionResults | Measure-Object -Property SuccessScore -Sum { ($_.SuccessScore - $avgScore) * ($_.SuccessScore - $avgScore) }).Average
    )
    
    $threshold = $avgScore - (2 * $stdDev)  # 2 standard deviations
    $anomalies = $ExecutionResults | Where-Object { $_.SuccessScore -lt $threshold }
    
    foreach ($anomaly in $anomalies) {
        $learnings.AnomalousResults += @{
            Agent = $anomaly.AgentID
            Model = $anomaly.ModelID
            Task = $anomaly.TaskType
            Score = $anomaly.SuccessScore
            Deviation = [Math]::Round($anomaly.SuccessScore - $avgScore, 2)
            Impact = if ($anomaly.Cost -gt 0.01) { 'high-cost-failure' } else { 'low-cost-failure' }
        }
    }
    
    return $learnings
}

function Generate-LearningInsights {
    <#
    .SYNOPSIS
    Generate actionable insights from learned patterns.
    #>
    param(
        [hashtable]$Learnings,
        [int]$ExecutionCount
    )
    
    $insights = @{
        Timestamp = Get-Date
        ExecutionCount = $ExecutionCount
        Recommendations = @()
        Warnings = @()
        Opportunities = @()
        OptimizationActions = @()
        ConfidenceLevel = if ($ExecutionCount -gt 100) { 'high' } elseif ($ExecutionCount -gt 50) { 'medium' } else { 'low' }
    }
    
    # Generate recommendations
    $strongAgents = $Learnings.PatternsByAgent.GetEnumerator() | 
        Where-Object { $_.Value.Strength -eq 'strong' } |
        Select-Object -ExpandProperty Name
    
    if ($strongAgents) {
        $insights.Recommendations += "AGENT ALLOCATION: Prioritize agents: $($strongAgents -join ', ') - consistently high performance"
    }
    
    # Cost insights
    $cheapestEffective = $Learnings.BestCombinations | 
        Where-Object { $_.Type -eq 'cost-effective' } | 
        Select-Object -First 3
    
    if ($cheapestEffective) {
        $insights.Recommendations += "COST OPTIMIZATION: Deploy cost-effective combinations: $($cheapestEffective -join ', ')"
    }
    
    # Quality insights
    $qualityLeaders = $Learnings.BestCombinations | 
        Where-Object { $_.Type -eq 'quality-leader' } | 
        Select-Object -First 3
    
    if ($qualityLeaders) {
        $insights.Recommendations += "QUALITY FOCUS: Use quality leaders for critical tasks"
    }
    
    # Anomaly warnings
    if ($Learnings.AnomalousResults.Count -gt 5) {
        $insights.Warnings += "ANOMALY ALERT: $($Learnings.AnomalousResults.Count) unusual failures detected - investigate patterns"
    }
    
    # Optimization opportunities
    $insights.OptimizationActions += "DYNAMIC SPECIALIZATION: Reassign agents based on learned strengths"
    $insights.OptimizationActions += "MULTI-PROVIDER FALLBACK: Implement failover chains using top performers"
    $insights.OptimizationActions += "LOAD BALANCING: Distribute tasks to highest-performing agent-model pairs"
    
    return $insights
}

# ============================================================================
# CONTINUOUS OPTIMIZATION
# ============================================================================

function Optimize-Based-OnLearnings {
    <#
    .SYNOPSIS
    Dynamically optimize system based on learned patterns.
    #>
    param(
        [hashtable]$Learnings,
        [hashtable]$CurrentAssignments,
        [array]$Agents
    )
    
    $optimizations = @{}
    
    # Optimize agent specializations
    foreach ($agent in $Agents) {
        $agentLearning = $Learnings.PatternsByAgent[$agent]
        
        if ($agentLearning) {
            # Assign specialization based on strength
            if ($agentLearning.Strength -eq 'strong') {
                $optimizations[$agent] = @{
                    Action = 'UPGRADE'
                    CurrentSpecialization = $CurrentAssignments[$agent]
                    RecommendedSpecialization = "$($agent)-expert"
                    Confidence = 0.9
                    Impact = "Use for complex tasks, high-priority work"
                }
            }
            elseif ($agentLearning.Strength -eq 'weak') {
                $optimizations[$agent] = @{
                    Action = 'REASSIGN'
                    CurrentSpecialization = $CurrentAssignments[$agent]
                    RecommendedSpecialization = "$($agent)-support"
                    Confidence = 0.7
                    Impact = "Use for simple tasks, batch processing"
                }
            }
            else {
                $optimizations[$agent] = @{
                    Action = 'MAINTAIN'
                    Confidence = 0.8
                    Impact = "Continue current assignment with monitoring"
                }
            }
        }
    }
    
    return $optimizations
}

function Build-AdaptiveRoutingTable {
    <#
    .SYNOPSIS
    Build dynamic routing table based on all learned patterns.
    #>
    param(
        [hashtable]$Learnings,
        [array]$TaskTypes = @('classification', 'reasoning', 'extraction', 'summarization')
    )
    
    $routingTable = @{}
    
    foreach ($taskType in $TaskTypes) {
        # Find best agent-model combinations for this task
        $taskResults = $Learnings.BestCombinations | 
            Where-Object { $_.Task -eq $taskType } |
            Sort-Object -Property Score -Descending |
            Select-Object -First 3
        
        if ($taskResults) {
            $routingTable[$taskType] = @{
                Primary = $taskResults[0]
                Secondary = if ($taskResults.Count -gt 1) { $taskResults[1] } else { $null }
                Tertiary = if ($taskResults.Count -gt 2) { $taskResults[2] } else { $null }
                Confidence = 'high'
                LastUpdated = Get-Date
            }
        }
    }
    
    return $routingTable
}

# ============================================================================
# EXPORTS
# ============================================================================

Export-ModuleMember -Function @(
    'Initialize-LoggingSystem',
    'Write-Log',
    'Log-ExecutionMetrics',
    'Initialize-LearningEngine',
    'Learn-FromExecutions',
    'Generate-LearningInsights',
    'Optimize-Based-OnLearnings',
    'Build-AdaptiveRoutingTable'
) -Variable 'LogEntry'
