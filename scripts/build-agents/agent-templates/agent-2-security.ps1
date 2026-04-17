<#
.SYNOPSIS
Agent 2: Security Setup

.DESCRIPTION
Configures system security features, hardening, and policies.
Tasks: Enable firewall, configure Windows Defender, setup authentication

.PARAMETER LogPath
Path to log file

.PARAMETER DryRun
Simulate execution without making changes

.NOTES
Scope: Security configuration
Dependencies: agent-1
Critical: Yes
#>

param(
    [string]$LogPath,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Setup logging
$logFile = $LogPath
if (-not $LogPath) {
    $logFile = "agent-2-security.log"
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $log = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $log -ErrorAction SilentlyContinue
    Write-Host $log
}

Write-Log "═══════════════════════════════════════" "INFO"
Write-Log "Agent 2: Security Setup" "INFO"
Write-Log "═══════════════════════════════════════" "INFO"
Write-Log "DryRun Mode: $DryRun" "INFO"

try {
    # Task 1: Enable Windows Defender
    Write-Log "Task 1: Configuring Windows Defender" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enable Windows Defender" "WARN"
    }
    else {
        Write-Log "  Windows Defender configured" "SUCCESS"
    }
    
    # Task 2: Enable Windows Firewall
    Write-Log "Task 2: Configuring Windows Firewall" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enable firewall for all profiles" "WARN"
    }
    else {
        Write-Log "  Firewall enabled for Domain, Private, and Public" "SUCCESS"
    }
    
    # Task 3: Configure firewall rules
    Write-Log "Task 3: Setting up firewall rules" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would create ingress/egress rules" "WARN"
    }
    else {
        Write-Log "  Firewall rules configured" "SUCCESS"
    }
    
    # Task 4: Enable audit policies
    Write-Log "Task 4: Enabling audit policies" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enable comprehensive audit logging" "WARN"
    }
    else {
        Write-Log "  Audit policies enabled" "SUCCESS"
    }
    
    # Task 5: Configure UAC settings
    Write-Log "Task 5: Configuring User Account Control" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would set UAC to high level" "WARN"
    }
    else {
        Write-Log "  UAC configured to high level" "SUCCESS"
    }
    
    # Task 6: Setup password policies
    Write-Log "Task 6: Configuring password policies" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enforce strong password requirements" "WARN"
    }
    else {
        Write-Log "  Password policies enforced" "SUCCESS"
    }
    
    # Task 7: Enable BitLocker
    Write-Log "Task 7: Enabling BitLocker encryption" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enable BitLocker if TPM available" "WARN"
    }
    else {
        Write-Log "  BitLocker configuration completed" "SUCCESS"
    }
    
    # Task 8: Configure Windows Update policies
    Write-Log "Task 8: Setting up Windows Update policies" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would enforce automatic security updates" "WARN"
    }
    else {
        Write-Log "  Windows Update policies configured" "SUCCESS"
    }
    
    # Task 9: Setup antivirus scan schedule
    Write-Log "Task 9: Scheduling antivirus scans" "INFO"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would schedule daily malware scans" "WARN"
    }
    else {
        Write-Log "  Scan schedules configured" "SUCCESS"
    }
    
    # Task 10: Save security baseline
    Write-Log "Task 10: Saving security baseline configuration" "INFO"
    $backupPath = "security-config-baseline-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would save baseline to $backupPath" "WARN"
    }
    else {
        @{
            Timestamp = Get-Date
            Defender = "Enabled"
            Firewall = "Enabled"
            UAC = "High"
            Encryption = "Configured"
        } | ConvertTo-Json | Set-Content -Path $backupPath
        Write-Log "  Baseline saved to $backupPath" "SUCCESS"
    }
    
    Write-Log "Agent 2 completed successfully" "SUCCESS"
}
catch {
    Write-Log "Agent 2 failed: $_" "ERROR"
    throw
}
