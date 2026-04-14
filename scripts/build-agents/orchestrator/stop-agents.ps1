<#
.SYNOPSIS
Stop running build agents

.DESCRIPTION
Terminates running agent jobs and optionally triggers rollback procedures.
Can stop all agents or specific agents by ID.

.PARAMETER All
Stop all running agents

.PARAMETER AgentIds
Array of agent IDs to stop (e.g., @(3,5,7))

.PARAMETER Force
Force kill without graceful shutdown

.PARAMETER Rollback
Execute rollback procedures for stopped agents

.EXAMPLE
.\stop-agents.ps1 -All
.\stop-agents.ps1 -AgentIds @(3,5) -Rollback
.\stop-agents.ps1 -AgentIds @(1) -Force

.NOTES
Graceful shutdown waits for agent to complete current task
Force shutdown immediately terminates the process
#>

param(
    [switch]$All,
    [int[]]$AgentIds = @(),
    [switch]$Force,
    [switch]$Rollback
)

$ErrorActionPreference = "Continue"

# Define paths
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommandPath
$BaseDir = Split-Path -Parent $ScriptRoot
$ConfigPath = Join-Path $BaseDir "config"
$TempPath = Join-Path $BaseDir "temp"
$LogsPath = Join-Path $BaseDir "logs"
$StatusFile = Join-Path $TempPath "orchestrator_status.json"

# Timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logFile = Join-Path $LogsPath "stop-agents_$timestamp.log"

# Logging
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )
    
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$ts] [$Level] $Message"
    
    Add-Content -Path $logFile -Value $logMessage -ErrorAction SilentlyContinue
    
    $color = @{
        "INFO"    = "White"
        "WARN"    = "Yellow"
        "ERROR"   = "Red"
        "SUCCESS" = "Green"
    }
    
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

# Get status
function Get-OrchestratorStatus {
    if (-not (Test-Path $StatusFile)) {
        Write-Log "No status file found" "WARN"
        return $null
    }
    
    return Get-Content $StatusFile | ConvertFrom-Json
}

# Update status
function Update-StatusFile {
    param($Status)
    $Status | ConvertTo-Json -Depth 10 | Set-Content -Path $StatusFile -Force
}

# Get agents to stop
function Get-AgentsToStop {
    $status = Get-OrchestratorStatus
    
    if ($null -eq $status) {
        return @()
    }
    
    $agents = @()
    
    if ($All) {
        $status.Agents.PSObject.Properties | ForEach-Object {
            if ($_.Value.Status -eq "Running") {
                $agents += $_.Name
            }
        }
    }
    else {
        foreach ($id in $AgentIds) {
            $agentName = "agent-$id"
            $agentStatus = $status.Agents.$agentName
            
            if ($null -ne $agentStatus -and $agentStatus.Status -eq "Running") {
                $agents += $agentName
            }
        }
    }
    
    return $agents
}

# Get running jobs
function Get-RunningJobs {
    param([string[]]$AgentNames)
    
    $jobs = @()
    
    foreach ($agentName in $AgentNames) {
        $job = Get-Job -Name $agentName -ErrorAction SilentlyContinue
        if ($null -ne $job -and $job.State -eq "Running") {
            $jobs += $job
        }
    }
    
    return $jobs
}

# Stop agent gracefully
function Stop-AgentGracefully {
    param(
        [string]$AgentName,
        [int]$TimeoutSeconds = 30
    )
    
    Write-Log "Stopping $AgentName gracefully (timeout: ${TimeoutSeconds}s)" "INFO"
    
    $job = Get-Job -Name $AgentName -ErrorAction SilentlyContinue
    
    if ($null -eq $job) {
        Write-Log "$AgentName job not found" "WARN"
        return $false
    }
    
    # Wait for job to complete
    $completed = Wait-Job -Job $job -Timeout $TimeoutSeconds -ErrorAction SilentlyContinue
    
    if ($null -ne $completed) {
        Write-Log "$AgentName stopped successfully" "SUCCESS"
        Receive-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force
        return $true
    }
    else {
        Write-Log "$AgentName did not stop within timeout" "WARN"
        return $false
    }
}

# Stop agent forcefully
function Stop-AgentForce {
    param([string]$AgentName)
    
    Write-Log "Force stopping $AgentName" "WARN"
    
    $job = Get-Job -Name $AgentName -ErrorAction SilentlyContinue
    
    if ($null -eq $job) {
        Write-Log "$AgentName job not found" "WARN"
        return $false
    }
    
    Stop-Job -Job $job -PassThru -ErrorAction SilentlyContinue
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    
    Write-Log "$AgentName force stopped" "SUCCESS"
    return $true
}

# Rollback agent
function Invoke-AgentRollback {
    param([string]$AgentName)
    
    $agentId = [int]($AgentName -replace "agent-", "")
    
    Write-Log "Rolling back $AgentName" "INFO"
    
    $rollbackConfig = $agentDependencies.rollbackProcedures | Where-Object { $_.agent -eq $AgentName }
    
    if ($null -eq $rollbackConfig) {
        Write-Log "No rollback procedure for $AgentName" "WARN"
        return
    }
    
    try {
        foreach ($step in $rollbackConfig.steps) {
            Write-Log "Executing: $($step.description)" "INFO"
            Invoke-Expression $step.command
        }
        Write-Log "$AgentName rollback completed" "SUCCESS"
    }
    catch {
        Write-Log "Rollback failed: $_" "ERROR"
    }
}

# Main stop orchestration
function Invoke-StopOrchestration {
    Write-Log "================================================" "INFO"
    Write-Log "Agent Stop Orchestration Started" "INFO"
    Write-Log "Force: $Force, Rollback: $Rollback" "INFO"
    Write-Log "================================================" "INFO"
    
    $agentsToStop = Get-AgentsToStop
    
    if ($agentsToStop.Count -eq 0) {
        Write-Log "No agents to stop" "INFO"
        return $true
    }
    
    Write-Log "Agents to stop: $($agentsToStop -join ', ')" "INFO"
    
    $agentDependencies = Get-Content (Join-Path $ConfigPath "agent-dependencies.json") | ConvertFrom-Json
    
    # Stop agents
    foreach ($agentName in $agentsToStop) {
        if ($Force) {
            Stop-AgentForce -AgentName $agentName
        }
        else {
            Stop-AgentGracefully -AgentName $agentName -TimeoutSeconds 30
        }
        
        # Rollback if requested
        if ($Rollback) {
            Invoke-AgentRollback -AgentName $agentName
        }
    }
    
    # Update status
    $status = Get-OrchestratorStatus
    if ($null -ne $status) {
        foreach ($agentName in $agentsToStop) {
            if ($status.Agents.$agentName) {
                $status.Agents.$agentName.Status = "Stopped"
                $status.Agents.$agentName.StoppedTime = Get-Date
            }
        }
        Update-StatusFile -Status $status
    }
    
    Write-Log "================================================" "INFO"
    Write-Log "Agent Stop Orchestration Completed" "INFO"
    Write-Log "Stopped Agents: $($agentsToStop.Count)" "INFO"
    Write-Log "================================================" "INFO"
    
    return $true
}

# Validate parameters
if (-not $All -and $AgentIds.Count -eq 0) {
    Write-Host "Usage: .\stop-agents.ps1 -All [-Force] [-Rollback]" -ForegroundColor Yellow
    Write-Host "       .\stop-agents.ps1 -AgentIds @(1,3,5) [-Force] [-Rollback]" -ForegroundColor Yellow
    exit 1
}

# Execute
try {
    $result = Invoke-StopOrchestration
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log "Orchestration failed: $_" "ERROR"
    exit 1
}
