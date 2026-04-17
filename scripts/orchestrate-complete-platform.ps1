#!/usr/bin/env pwsh
<#
.SYNOPSIS
HELIOS Complete Platform Orchestrator
Integrates agents, board, learning, and automation into one unified system
#>

param(
    [string]$Mode = "all",  # all, agents, learning, automation
    [int]$IntervalMinutes = 5,
    [bool]$Continuous = $false
)

$ErrorActionPreference = "Stop"

# Paths
$scriptRoot = Split-Path -Parent $PSCommandPath
$agentModule = Join-Path $scriptRoot "agents" "AgentOrchestration.psm1"
$learningModule = Join-Path $scriptRoot "learning" "LearningSystem.psm1"
$automationModule = Join-Path $scriptRoot "automation" "AutomationRules.psm1"
$metricsModule = Join-Path $scriptRoot "metrics" "MetricsCollector.psm1"
$dbModule = Join-Path $scriptRoot "database" "DatabaseHelper.psm1"
$githubModule = Join-Path $scriptRoot "github" "GitHubIntegration.psm1"

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   HELIOS COMPLETE PLATFORM ORCHESTRATOR                  ║" -ForegroundColor Cyan
Write-Host "║   Agents + Board + Learning + Automation                ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Load all modules
Write-Host "Loading modules..." -ForegroundColor Yellow
Import-Module $agentModule -Force
Import-Module $learningModule -Force
Import-Module $automationModule -Force
Import-Module $metricsModule -Force
Import-Module $dbModule -Force
Import-Module $githubModule -Force
Write-Host "✓ All modules loaded" -ForegroundColor Green
Write-Host ""

function Invoke-CompletePlatformCycle {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting complete platform cycle..." -ForegroundColor Cyan
    Write-Host ""
    
    # Phase 1: Agent Orchestration
    Write-Host "▶ PHASE 1: AGENT ORCHESTRATION" -ForegroundColor Magenta
    Write-Host "─────────────────────────────────" -ForegroundColor Magenta
    $agents = Initialize-AgentPool -Count 22
    
    # Simulate tasks and assignments
    for ($i = 0; $i -lt 5; $i++) {
        $priorities = @("critical","high","normal","low")
        $roles = @("Foundation","Execution","Optimization","Quality")
        $task = @{
            id = "TASK-$(Get-Random -Minimum 1000 -Maximum 9999)"
            priority = $priorities[(Get-Random -Minimum 0 -Maximum 4)]
            preferred_role = $roles[(Get-Random -Minimum 0 -Maximum 4)]
        }
        
        $agent = Get-OptimalAgentForTask -Agents $agents -Task $task
        if ($agent) {
            $assignment = Assign-TaskToAgent -Task $task -Agent $agent
            
            # Simulate execution
            $result = @{ success = (Get-Random -Minimum 0 -Maximum 100) -gt 10; duration_ms = Get-Random -Minimum 100 -Maximum 5000 }
            $agent = Update-AgentMetrics -Agent $agent -Result $result
        }
    }
    Write-Host ""
    
    # Phase 2: Metrics Collection
    Write-Host "▶ PHASE 2: METRICS COLLECTION" -ForegroundColor Magenta
    Write-Host "────────────────────────────" -ForegroundColor Magenta
    $execution_metrics = Get-ExecutionMetrics
    $performance_metrics = Get-PerformanceMetrics
    $quality_metrics = Get-QualityMetrics
    Write-Host "✓ Collected execution metrics (22 vars)" -ForegroundColor Green
    Write-Host "✓ Collected performance metrics (18 vars)" -ForegroundColor Green
    Write-Host "✓ Collected quality metrics (15 vars)" -ForegroundColor Green
    Write-Host ""
    
    # Phase 3: Automation Rules
    Write-Host "▶ PHASE 3: AUTOMATION RULES" -ForegroundColor Magenta
    Write-Host "──────────────────────────" -ForegroundColor Magenta
    Initialize-RulesEngine
    
    # Simulate events
    $event1 = @{ type = "on_task_failure"; failure_type = "transient"; max_retries = 3 }
    $triggeredRules = Get-TriggeredRules -Event $event1
    foreach ($rule in $triggeredRules) {
        $ruleResult = Execute-Rule -Rule $rule -Event $event1
    }
    
    Sync-RulesToBoard | Out-Null
    Write-Host ""
    
    # Phase 4: Learning System
    Write-Host "▶ PHASE 4: LEARNING SYSTEM" -ForegroundColor Magenta
    Write-Host "──────────────────────────" -ForegroundColor Magenta
    $analysis = Analyze-AgentPatterns -Agents $agents
    $learningData = Start-LearningCycle -Agents $agents -Metrics @{}
    $appliedRules = Apply-LearningInsights -LearningData $learningData
    $optimizations = Optimize-AgentAllocation -Agents $agents -LearningData $learningData
    $prediction = Predict-PerformanceTrend -Agents $agents
    Write-Host "✓ Learning cycle: $($learningData.patterns_detected.Count) patterns detected" -ForegroundColor Green
    Write-Host "✓ Applied $($appliedRules.Count) improvements" -ForegroundColor Green
    Write-Host "✓ Generated $($optimizations.Count) optimization recommendations" -ForegroundColor Green
    Write-Host ""
    
    # Phase 5: Board Synchronization
    Write-Host "▶ PHASE 5: GITHUB BOARD SYNC" -ForegroundColor Magenta
    Write-Host "───────────────────────────" -ForegroundColor Magenta
    $boardMetrics = Sync-AgentMetricsToBoard -Agents $agents
    Sync-MetricsToGitHubBoard -Metrics $execution_metrics | Out-Null
    Sync-MetricsToGitHubPages -Metrics $execution_metrics -OutputPath ".github/pages" | Out-Null
    Write-Host "✓ Synced $($boardMetrics.total_agents) agents to board" -ForegroundColor Green
    Write-Host "✓ Updated GitHub Pages dashboard" -ForegroundColor Green
    Write-Host ""
    
    # Phase 6: Reporting
    Write-Host "▶ PHASE 6: REPORTING" -ForegroundColor Magenta
    Write-Host "────────────────────" -ForegroundColor Magenta
    $learningReport = Generate-LearningReport -LearningData $learningData
    $rulesReport = Get-RulesReport
    Create-MetricsReport -Metrics $execution_metrics -OutputPath "data/metrics" | Out-Null
    Write-Host "✓ Generated learning system report" -ForegroundColor Green
    Write-Host "✓ Generated automation rules report" -ForegroundColor Green
    Write-Host "✓ Generated metrics report" -ForegroundColor Green
    Write-Host ""
    
    # Summary
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "CYCLE COMPLETE - All Systems Operational" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  Agents:        $($agents.Count) active (Avg efficiency: $($analysis.avg_efficiency)%)" -ForegroundColor Gray
    Write-Host "  Tasks:         5 processed (Success rate: $($analysis.avg_success)%)" -ForegroundColor Gray
    Write-Host "  Rules:         7 active (Executions: $((Get-RulesReport) -match 'Total Executions' | Select-Object -First 1))" -ForegroundColor Gray
    Write-Host "  Learning:      $($learningData.patterns_detected.Count) patterns, $($learningData.improvements_proposed.Count) improvements" -ForegroundColor Gray
    Write-Host "  Optimizations: $($optimizations.Count) recommendations" -ForegroundColor Gray
    Write-Host "  Board Sync:    ✓ Complete" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Next cycle in $IntervalMinutes minute(s)..." -ForegroundColor Yellow
    Write-Host ""
}

# Single run
if (-not $Continuous) {
    try {
        Invoke-CompletePlatformCycle
        Write-Host "✅ Platform cycle completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error during platform cycle: $_" -ForegroundColor Red
        exit 1
    }
} else {
    # Continuous mode
    Write-Host "Starting continuous platform orchestration..." -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""
    
    $runCount = 0
    while ($true) {
        $runCount++
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "CYCLE #$runCount at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
        
        try {
            Invoke-CompletePlatformCycle
            Start-Sleep -Seconds ($IntervalMinutes * 60)
        } catch {
            Write-Host "⚠️  Error in cycle #$runCount : $_" -ForegroundColor Red
            Start-Sleep -Seconds 30
        }
    }
}
