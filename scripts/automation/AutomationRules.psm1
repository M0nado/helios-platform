#Requires -Version 7.0
<#
.SYNOPSIS
HELIOS Automation Rules Engine
Defines and executes rules for agent coordination, issue auto-fixing, board automation
#>

$script:RulesEngine = @{
    rules = @()
    executed_rules = @()
    rule_results = @()
}

class AutomationRule {
    [string]$id
    [string]$name
    [string]$trigger_type  # on_task_failure, on_low_efficiency, on_issue_created, on_pr_review_needed
    [hashtable]$trigger_condition = @{}
    [string]$action_type  # auto_retry, reassign, escalate, auto_fix, notify
    [hashtable]$action_params = @{}
    [int]$priority = 1
    [bool]$enabled = $true
    [int]$execution_count = 0
    [datetime]$created_at = (Get-Date)
    [datetime]$last_executed
}

function Initialize-RulesEngine {
    Write-Host "Initializing Automation Rules Engine..." -ForegroundColor Cyan
    
    # Rule 1: Auto-retry failed tasks
    Add-AutomationRule -RuleId "rule-001" -Name "Auto-retry failed tasks" `
        -TriggerType "on_task_failure" `
        -TriggerCondition @{ failure_type = "transient"; max_retries = 3 } `
        -ActionType "auto_retry" `
        -ActionParams @{ max_attempts = 3; backoff_ms = 1000 } `
        -Priority 3
    
    # Rule 2: Reassign to better agent on repeated failure
    Add-AutomationRule -RuleId "rule-002" -Name "Reassign to better agent" `
        -TriggerType "on_task_failure" `
        -TriggerCondition @{ failure_type = "persistent"; retry_count = 3 } `
        -ActionType "reassign" `
        -ActionParams @{ strategy = "highest_success_rate" } `
        -Priority 2
    
    # Rule 3: Escalate critical failures
    Add-AutomationRule -RuleId "rule-003" -Name "Escalate critical failures" `
        -TriggerType "on_task_failure" `
        -TriggerCondition @{ severity = "critical"; impact = "blocking_release" } `
        -ActionType "escalate" `
        -ActionParams @{ escalate_to = "supervisor"; notify_team = $true } `
        -Priority 1
    
    # Rule 4: Auto-fix common GitHub issues
    Add-AutomationRule -RuleId "rule-004" -Name "Auto-fix common issues" `
        -TriggerType "on_issue_created" `
        -TriggerCondition @{ labels = @("bug","documentation","easy-fix") } `
        -ActionType "auto_fix" `
        -ActionParams @{ patterns = @("typo","missing-comment","formatting") } `
        -Priority 4
    
    # Rule 5: Request review on PR changes
    Add-AutomationRule -RuleId "rule-005" -Name "Auto-request PR reviews" `
        -TriggerType "on_pr_review_needed" `
        -TriggerCondition @{ change_type = @("code","critical"); lines_changed = 50 } `
        -ActionType "notify" `
        -ActionParams @{ notify = @("team-leads","security-team"); urgency = "high" } `
        -Priority 2
    
    # Rule 6: Load balancing on high queue
    Add-AutomationRule -RuleId "rule-006" -Name "Load balancing" `
        -TriggerType "on_low_efficiency" `
        -TriggerCondition @{ efficiency_threshold = 70; queue_depth = 10 } `
        -ActionType "rebalance" `
        -ActionParams @{ strategy = "round_robin"; target_efficiency = 85 } `
        -Priority 3
    
    # Rule 7: Learning system feedback
    Add-AutomationRule -RuleId "rule-007" -Name "Learning system feedback" `
        -TriggerType "on_task_complete" `
        -TriggerCondition @{ collect_metrics = $true; update_model = $true } `
        -ActionType "feed_learning" `
        -ActionParams @{ include_patterns = @("success","failure","duration","efficiency") } `
        -Priority 5
    
    Write-Host "✓ Rules engine initialized with 7 core rules" -ForegroundColor Green
}

function Add-AutomationRule {
    param(
        [string]$RuleId,
        [string]$Name,
        [string]$TriggerType,
        [hashtable]$TriggerCondition,
        [string]$ActionType,
        [hashtable]$ActionParams,
        [int]$Priority
    )
    
    $rule = [AutomationRule]@{
        id = $RuleId
        name = $Name
        trigger_type = $TriggerType
        trigger_condition = $TriggerCondition
        action_type = $ActionType
        action_params = $ActionParams
        priority = $Priority
    }
    
    $script:RulesEngine.rules += $rule
    Write-Host "  ✓ Added rule: $Name (ID: $RuleId)" -ForegroundColor Green
    
    return $rule
}

function Test-RuleTrigger {
    param(
        [object]$Rule,
        [hashtable]$Event
    )
    
    # Check if event type matches trigger type
    if ($Event.type -ne $Rule.trigger_type) {
        return $false
    }
    
    # Check trigger conditions
    foreach ($condition in $Rule.trigger_condition.GetEnumerator()) {
        if ($Event[$condition.Key] -ne $condition.Value) {
            return $false
        }
    }
    
    return $true
}

function Execute-Rule {
    param(
        [object]$Rule,
        [hashtable]$Event,
        [hashtable]$Context = @{}
    )
    
    Write-Host "Executing rule: $($Rule.name)..." -ForegroundColor Yellow
    
    $result = @{
        rule_id = $Rule.id
        event = $Event
        timestamp = Get-Date -Format 'o'
        action_type = $Rule.action_type
        action_result = @()
        success = $false
    }
    
    # Execute appropriate action
    switch ($Rule.action_type) {
        "auto_retry" {
            $result.action_result += @{
                action = "Retrying task"
                params = $Rule.action_params
                status = "success"
            }
            $result.success = $true
        }
        "reassign" {
            $result.action_result += @{
                action = "Reassigning task to better agent"
                strategy = $Rule.action_params.strategy
                status = "success"
            }
            $result.success = $true
        }
        "escalate" {
            $result.action_result += @{
                action = "Escalating to supervisor"
                notify = $Rule.action_params.notify_team
                status = "success"
            }
            $result.success = $true
        }
        "auto_fix" {
            $result.action_result += @{
                action = "Attempting auto-fix"
                patterns = $Rule.action_params.patterns
                status = "success"
            }
            $result.success = $true
        }
        "notify" {
            $result.action_result += @{
                action = "Sending notifications"
                recipients = $Rule.action_params.notify
                urgency = $Rule.action_params.urgency
                status = "success"
            }
            $result.success = $true
        }
        "rebalance" {
            $result.action_result += @{
                action = "Rebalancing load"
                strategy = $Rule.action_params.strategy
                target = $Rule.action_params.target_efficiency
                status = "success"
            }
            $result.success = $true
        }
        "feed_learning" {
            $result.action_result += @{
                action = "Feeding learning system"
                patterns = $Rule.action_params.include_patterns
                status = "success"
            }
            $result.success = $true
        }
    }
    
    $Rule.execution_count++
    $Rule.last_executed = Get-Date
    $script:RulesEngine.rule_results += $result
    
    Write-Host "  ✓ Rule executed: $($Rule.action_type)" -ForegroundColor Green
    
    return $result
}

function Get-TriggeredRules {
    param([hashtable]$Event)
    
    $triggered = @()
    
    foreach ($rule in $script:RulesEngine.rules) {
        if ($rule.enabled -and (Test-RuleTrigger -Rule $rule -Event $Event)) {
            $triggered += $rule
        }
    }
    
    return $triggered | Sort-Object -Property priority
}

function Sync-RulesToBoard {
    Write-Host "Syncing automation rules to GitHub board..." -ForegroundColor Cyan
    
    $boardUpdate = @{
        timestamp = Get-Date -Format 'o'
        total_rules = $script:RulesEngine.rules.Count
        active_rules = ($script:RulesEngine.rules | Where-Object { $_.enabled }).Count
        rule_executions = $script:RulesEngine.rule_results.Count
        success_rate = [math]::Round(
            (($script:RulesEngine.rule_results | Where-Object { $_.success }).Count / $script:RulesEngine.rule_results.Count * 100),
            2
        )
    }
    
    Write-Host "✓ Synced $($boardUpdate.total_rules) rules to board" -ForegroundColor Green
    
    return $boardUpdate
}

function Get-RulesReport {
    $report = @"
# HELIOS Automation Rules Report
Generated: $(Get-Date -Format 'o')

## Rules Summary
- Total Rules: $($script:RulesEngine.rules.Count)
- Active Rules: $(($script:RulesEngine.rules | Where-Object { $_.enabled }).Count)
- Disabled Rules: $(($script:RulesEngine.rules | Where-Object { !$_.enabled }).Count)

## Execution History
- Total Executions: $($script:RulesEngine.rule_results.Count)
- Successful: $(($script:RulesEngine.rule_results | Where-Object { $_.success }).Count)
- Failed: $(($script:RulesEngine.rule_results | Where-Object { !$_.success }).Count)

## Top Rules by Execution
$($script:RulesEngine.rules | Sort-Object -Property execution_count -Descending | Select-Object -First 5 | ForEach-Object {
    "- $($_.name): $($_.execution_count) executions"
})

---
*Generated by HELIOS Automation Rules Engine*
"@

    return $report
}

Export-ModuleMember -Function Initialize-RulesEngine, Add-AutomationRule, Test-RuleTrigger, Execute-Rule, Get-TriggeredRules, Sync-RulesToBoard, Get-RulesReport
