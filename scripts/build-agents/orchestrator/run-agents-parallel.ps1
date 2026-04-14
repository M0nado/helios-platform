<#
.SYNOPSIS
Execute build agents in parallel where dependencies allow

.DESCRIPTION
Analyzes agent dependencies and executes agents in parallel batches
to optimize orchestration speed while respecting dependency constraints.

.PARAMETER MaxParallelJobs
Maximum number of parallel jobs to run simultaneously (default: 4)

.PARAMETER TimeoutSeconds
Timeout for each agent job in seconds (default: 3600)

.PARAMETER DryRun
Simulate execution without making changes

.EXAMPLE
.\run-agents-parallel.ps1
.\run-agents-parallel.ps1 -MaxParallelJobs 8 -TimeoutSeconds 7200

.NOTES
Uses PowerShell jobs for parallel execution
Monitors all running jobs and handles failures gracefully
#>

param(
    [int]$MaxParallelJobs = 4,
    [int]$TimeoutSeconds = 3600,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

# Define paths
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
$logFile = Join-Path $LogsPath "orchestrator_parallel_$timestamp.log"
$statusFile = Join-Path $TempPath "orchestrator_parallel_status.json"
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

# Build dependency graph
function Build-DependencyGraph {
    $graph = @{}
    
    for ($i = 1; $i -le 11; $i++) {
        $agentName = "agent-$i"
        $deps = $agentDependencies.dependencies | Where-Object { $_.agent -eq $agentName }
        $graph[$agentName] = if ($deps) { $deps.dependsOn -split "," } else { @() }
    }
    
    return $graph
}

# Find executable agents (no pending dependencies)
function Get-ExecutableAgents {
    param(
        [hashtable]$Graph,
        [hashtable]$Completed,
        [int[]]$SkipAgents = @()
    )
    
    $executable = @()
    
    $Graph.Keys | ForEach-Object {
        $agentName = $_
        $agentId = [int]($agentName -replace "agent-", "")
        
        if ($agentId -in $SkipAgents -or $Completed.Contains($agentName)) {
            return
        }
        
        $dependencies = $Graph[$agentName]
        $allSatisfied = $true
        
        foreach ($dep in $dependencies) {
            if (-not $Completed.Contains($dep)) {
                $allSatisfied = $false
                break
            }
        }
        
        if ($allSatisfied) {
            $executable += $agentName
        }
    }
    
    return $executable
}

# Execute agent as job
function Start-AgentJob {
    param(
        [string]$AgentName,
        [object]$AgentConfig
    )
    
    $agentId = [int]($AgentName -replace "agent-", "")
    $agentPath = Join-Path $AgentTemplatesPath "$AgentName.ps1"
    $logPath = Join-Path $LogsPath "${AgentName}_job_$timestamp.log"
    
    $job = Start-Job -ScriptBlock {
        param($Path, $LogPath, $DryRun)
        & $Path -LogPath $LogPath -DryRun:$DryRun -Verbose
    } -ArgumentList @($agentPath, $logPath, $DryRun) -Name $AgentName
    
    return $job
}

# Monitor job completion
function Wait-JobCompletion {
    param(
        [System.Management.Automation.Job[]]$Jobs,
        [int]$TimeoutSeconds
    )
    
    $startTime = Get-Date
    $completed = @{}
    $failed = @{}
    
    while ($Jobs.Count -gt 0) {
        $currentTime = Get-Date
        $elapsed = ($currentTime - $startTime).TotalSeconds
        
        if ($elapsed -gt $TimeoutSeconds) {
            Write-Log "Timeout exceeded ($TimeoutSeconds seconds)" "ERROR"
            $Jobs | Stop-Job -Force
            return $completed, $failed
        }
        
        $Jobs | ForEach-Object {
            if ($_.State -eq "Completed") {
                $result = Receive-Job -Job $_
                $completed[$_.Name] = $true
                Write-Log "Job $($_.Name) completed" "SUCCESS"
            }
            elseif ($_.State -eq "Failed") {
                $result = Receive-Job -Job $_ -ErrorAction SilentlyContinue
                $failed[$_.Name] = $result
                Write-Log "Job $($_.Name) failed" "ERROR"
            }
        }
        
        $Jobs = $Jobs | Where-Object { $_.State -eq "Running" }
        
        if ($Jobs.Count -gt 0) {
            Start-Sleep -Seconds 5
        }
    }
    
    return $completed, $failed
}

# Main parallel orchestration
function Invoke-ParallelOrchestration {
    Write-Log "================================================" "INFO"
    Write-Log "Build Agent Orchestration Started (Parallel)" "INFO"
    Write-Log "Max Parallel Jobs: $MaxParallelJobs" "INFO"
    Write-Log "Timeout: $TimeoutSeconds seconds" "INFO"
    Write-Log "DryRun: $DryRun" "INFO"
    Write-Log "================================================" "INFO"
    
    $graph = Build-DependencyGraph
    $completed = @{}
    $failed = @{}
    $runningJobs = @()
    $batchNumber = 0
    
    while ($completed.Count -lt 11) {
        $batchNumber++
        $executable = Get-ExecutableAgents -Graph $graph -Completed $completed
        
        if ($executable.Count -eq 0) {
            Write-Log "No executable agents - stopping" "WARN"
            break
        }
        
        Write-Log "========== Batch $batchNumber ==========" "INFO"
        Write-Log "Executable agents: $($executable -join ', ')" "INFO"
        
        # Start new jobs up to max parallel
        $jobsToStart = $executable[0..([Math]::Min($MaxParallelJobs - $runningJobs.Count, $executable.Count - 1))]
        
        foreach ($agentName in $jobsToStart) {
            $agentId = [int]($agentName -replace "agent-", "")
            $agentConfig = $agentsConfig.agents | Where-Object { $_.id -eq $agentId }
            
            Write-Log "Starting job: $agentName - $($agentConfig.name)" "INFO"
            $job = Start-AgentJob -AgentName $agentName -AgentConfig $agentConfig
            $runningJobs += $job
        }
        
        # Wait for at least one job to complete
        while ($runningJobs.Count -ge $MaxParallelJobs) {
            $completedJobs, $failedJobs = Wait-JobCompletion -Jobs $runningJobs -TimeoutSeconds $TimeoutSeconds
            
            $completed += $completedJobs
            $failed += $failedJobs
            
            $runningJobs = $runningJobs | Where-Object { $_.State -eq "Running" }
        }
    }
    
    # Wait for remaining jobs
    if ($runningJobs.Count -gt 0) {
        $completedJobs, $failedJobs = Wait-JobCompletion -Jobs $runningJobs -TimeoutSeconds $TimeoutSeconds
        $completed += $completedJobs
        $failed += $failedJobs
    }
    
    # Summary
    Write-Log "================================================" "INFO"
    Write-Log "Parallel Orchestration Completed" "INFO"
    Write-Log "Completed: $($completed.Count), Failed: $($failed.Count)" "INFO"
    Write-Log "================================================" "INFO"
    
    return $failed.Count -eq 0
}

# Execute
try {
    $result = Invoke-ParallelOrchestration
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log "Orchestration failed: $_" "ERROR"
    exit 1
}
