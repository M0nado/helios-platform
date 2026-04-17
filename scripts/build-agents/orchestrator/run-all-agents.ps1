<#
.SYNOPSIS
Execute all build agents sequentially with dependency management

.DESCRIPTION
Orchestrates the execution of all build agents in the correct sequence,
handling dependencies, error states, and logging all actions.

.PARAMETER SkipAgents
Array of agent numbers to skip (e.g., @(4,5))

.PARAMETER SkipRollback
Skip rollback procedures on failure

.PARAMETER DryRun
Simulate execution without making changes

.EXAMPLE
.\run-all-agents.ps1
.\run-all-agents.ps1 -SkipAgents @(5,6) -DryRun

.NOTES
All agent output is logged to logs/ directory
Requires agents-config.json and agent-dependencies.json
#>

param(
    [int[]]$SkipAgents = @(),
    [switch]$SkipRollback,
    [switch]$DryRun
)

# Set error action preference
$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

# Define base paths
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommandPath
$BaseDir = Split-Path -Parent $ScriptRoot
$AgentTemplatesPath = Join-Path $BaseDir "agent-templates"
$ConfigPath = Join-Path $BaseDir "config"
$LogsPath = Join-Path $BaseDir "logs"
$TempPath = Join-Path $BaseDir "temp"

# Ensure paths exist
@($LogsPath, $TempPath) | ForEach-Object {
    if (-not (Test-Path $_)) { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
}

# Configuration
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logFile = Join-Path $LogsPath "orchestrator_$timestamp.log"
$statusFile = Join-Path $TempPath "orchestrator_status.json"
$agentsConfig = Get-Content (Join-Path $ConfigPath "agents-config.json") | ConvertFrom-Json
$agentDependencies = Get-Content (Join-Path $ConfigPath "agent-dependencies.json") | ConvertFrom-Json

# Logging function
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    Add-Content -Path $logFile -Value $logMessage -ErrorAction SilentlyContinue
    
    $color = @{
        "INFO"    = "White"
        "WARN"    = "Yellow"
        "ERROR"   = "Red"
        "SUCCESS" = "Green"
    }
    
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

# Status tracking
$executionStatus = @{
    StartTime     = Get-Date
    Agents        = @{}
    FailedAgents  = @()
    SkippedAgents = @()
    RolledBackAgents = @()
    Status        = "In Progress"
}

# Save status to file
function Update-StatusFile {
    $executionStatus | ConvertTo-Json -Depth 10 | Set-Content -Path $statusFile -Force
}

# Load status from file
function Get-StatusFile {
    if (Test-Path $statusFile) {
        return Get-Content $statusFile | ConvertFrom-Json
    }
    return $null
}

# Get agent dependencies
function Get-AgentDependencies {
    param([int]$AgentId)
    
    $deps = $agentDependencies.dependencies | Where-Object { $_.agent -eq "agent-$AgentId" }
    if ($deps) {
        return $deps.dependsOn -split ","
    }
    return @()
}

# Check if dependencies are satisfied
function Test-DependenciesSatisfied {
    param([int]$AgentId)
    
    $dependencies = Get-AgentDependencies -AgentId $AgentId
    
    foreach ($dep in $dependencies) {
        $depAgentId = [int]($dep -replace "agent-", "")
        
        if ($depAgentId -in $SkipAgents) {
            Write-Log "Dependency agent-$depAgentId for agent-$AgentId is skipped" "WARN"
            return $false
        }
        
        $depStatus = $executionStatus.Agents["agent-$depAgentId"]
        if ($null -eq $depStatus -or $depStatus.Status -ne "Completed") {
            return $false
        }
    }
    
    return $true
}

# Execute single agent
function Invoke-Agent {
    param(
        [int]$AgentId,
        [object]$AgentConfig
    )
    
    $agentName = "agent-$AgentId"
    $agentPath = Join-Path $AgentTemplatesPath "$agentName.ps1"
    
    if (-not (Test-Path $agentPath)) {
        Write-Log "Agent script not found: $agentPath" "ERROR"
        return $false
    }
    
    Write-Log "========================================" "INFO"
    Write-Log "Starting $agentName - $($AgentConfig.name)" "INFO"
    Write-Log "Description: $($AgentConfig.description)" "INFO"
    Write-Log "========================================" "INFO"
    
    $executionStatus.Agents[$agentName] = @{
        Status    = "Running"
        StartTime = Get-Date
        LogFile   = Join-Path $LogsPath "${agentName}_$timestamp.log"
    }
    Update-StatusFile
    
    try {
        if ($DryRun) {
            Write-Log "DRY RUN: Would execute $agentName" "WARN"
            Start-Sleep -Milliseconds 500
        }
        else {
            # Execute agent with parameters
            & $agentPath -LogPath ($executionStatus.Agents[$agentName].LogFile) `
                         -DryRun:$DryRun `
                         -Verbose
        }
        
        $executionStatus.Agents[$agentName].Status = "Completed"
        $executionStatus.Agents[$agentName].EndTime = Get-Date
        Write-Log "$agentName completed successfully" "SUCCESS"
        return $true
        
    }
    catch {
        Write-Log "$agentName failed with error: $_" "ERROR"
        $executionStatus.Agents[$agentName].Status = "Failed"
        $executionStatus.Agents[$agentName].Error = $_.Exception.Message
        $executionStatus.FailedAgents += $agentName
        return $false
    }
    finally {
        Update-StatusFile
    }
}

# Rollback agent
function Invoke-AgentRollback {
    param([int]$AgentId)
    
    $agentName = "agent-$AgentId"
    $rollbackConfig = $agentDependencies.rollbackProcedures | Where-Object { $_.agent -eq $agentName }
    
    if ($null -eq $rollbackConfig) {
        Write-Log "No rollback procedure defined for $agentName" "WARN"
        return
    }
    
    Write-Log "Rolling back $agentName" "WARN"
    
    try {
        # Execute rollback steps
        foreach ($step in $rollbackConfig.steps) {
            Write-Log "Executing rollback step: $($step.description)" "WARN"
            Invoke-Expression $step.command
        }
        
        $executionStatus.RolledBackAgents += $agentName
        Write-Log "$agentName rolled back successfully" "SUCCESS"
    }
    catch {
        Write-Log "Rollback of $agentName failed: $_" "ERROR"
    }
}

# Main execution
function Invoke-Orchestration {
    Write-Log "================================================" "INFO"
    Write-Log "Build Agent Orchestration Started" "INFO"
    Write-Log "Mode: Sequential" "INFO"
    Write-Log "DryRun: $DryRun" "INFO"
    Write-Log "================================================" "INFO"
    
    $agentOrder = @(1..11)
    $successCount = 0
    $failureCount = 0
    
    foreach ($agentId in $agentOrder) {
        $agentName = "agent-$agentId"
        
        if ($agentId -in $SkipAgents) {
            Write-Log "Skipping $agentName (in skip list)" "WARN"
            $executionStatus.SkippedAgents += $agentName
            Update-StatusFile
            continue
        }
        
        # Check dependencies
        if (-not (Test-DependenciesSatisfied -AgentId $agentId)) {
            Write-Log "Dependencies not satisfied for $agentName - skipping" "WARN"
            $executionStatus.SkippedAgents += $agentName
            Update-StatusFile
            continue
        }
        
        $agentConfig = $agentsConfig.agents | Where-Object { $_.id -eq $agentId }
        
        if ($null -eq $agentConfig) {
            Write-Log "Configuration not found for $agentName" "ERROR"
            continue
        }
        
        if (Invoke-Agent -AgentId $agentId -AgentConfig $agentConfig) {
            $successCount++
        }
        else {
            $failureCount++
            
            if (-not $SkipRollback) {
                Invoke-AgentRollback -AgentId $agentId
            }
            
            # Determine if orchestration should continue
            if ($agentConfig.critical) {
                Write-Log "$agentName is critical - stopping orchestration" "ERROR"
                $executionStatus.Status = "Failed"
                Update-StatusFile
                throw "Critical agent $agentName failed"
            }
        }
    }
    
    Write-Log "================================================" "INFO"
    Write-Log "Build Agent Orchestration Completed" "INFO"
    Write-Log "Successful: $successCount, Failed: $failureCount" "INFO"
    Write-Log "Skipped: $($executionStatus.SkippedAgents.Count)" "INFO"
    Write-Log "Rolled Back: $($executionStatus.RolledBackAgents.Count)" "INFO"
    Write-Log "================================================" "INFO"
    
    $executionStatus.Status = if ($failureCount -eq 0) { "Success" } else { "Partial Failure" }
    $executionStatus.EndTime = Get-Date
    Update-StatusFile
    
    return $failureCount -eq 0
}

# Execute orchestration
try {
    $result = Invoke-Orchestration
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log "Orchestration failed with exception: $_" "ERROR"
    exit 1
}
