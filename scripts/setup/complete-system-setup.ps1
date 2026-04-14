<#
.SYNOPSIS
    Complete system setup orchestrator for HELIOS Platform
.DESCRIPTION
    Master script that orchestrates board setup, integration, and optimization
.PARAMETER GitHubToken
    GitHub Personal Access Token
.PARAMETER ProjectNumber
    Project number
.PARAMETER OrganizationName
    Organization name
.PARAMETER RepositoryName
    Repository name
.PARAMETER RepositoryOwner
    Repository owner
.PARAMETER DryRun
    Preview mode
.PARAMETER SkipValidation
    Skip final validation
.PARAMETER Verbose
    Detailed output
.EXAMPLE
    .\complete-system-setup.ps1 -GitHubToken $token -ProjectNumber 1 -OrganizationName "helios" -RepositoryName "platform" -RepositoryOwner "helios-org"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$true)]
    [int]$ProjectNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$OrganizationName,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryName,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryOwner,
    
    [switch]$DryRun,
    [switch]$SkipValidation,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logFile = "logs/complete-setup_$timestamp.log"
$masterReportFile = "logs/complete-setup-report_$timestamp.json"

if (-not (Test-Path 'logs')) { New-Item -ItemType Directory -Path 'logs' -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'HH:mm:ss'
    $entry = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'SUCCESS') { Write-Host $entry }
}

function Show-Progress {
    param([int]$Current, [int]$Total, [string]$Step)
    $percentage = [math]::Round(($Current / $Total) * 100)
    Write-Host "`n[Step $Current/$Total] $Step ($percentage%)" -ForegroundColor Cyan
}

function Invoke-OrchestratedStep {
    param(
        [string]$StepName,
        [string]$ScriptPath,
        [hashtable]$Parameters,
        [int]$StepNumber,
        [int]$TotalSteps
    )
    
    Show-Progress -Current $StepNumber -Total $TotalSteps -Step $StepName
    
    Write-Log "[$StepNumber/$TotalSteps] Executing: $StepName"
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Log "  Script not found: $ScriptPath" 'ERROR'
        return @{ name = $StepName; status = 'failed'; error = 'Script not found' }
    }
    
    try {
        $params = @{
            GitHubToken = $GitHubToken
        }
        
        # Add step-specific parameters
        foreach ($key in $Parameters.Keys) {
            $params[$key] = $Parameters[$key]
        }
        
        if ($DryRun) { $params['DryRun'] = $true }
        if ($Verbose) { $params['Verbose'] = $true }
        
        $result = & $ScriptPath @params
        
        Write-Log "  Completed: $StepName" 'SUCCESS'
        
        return @{
            name = $StepName
            status = 'completed'
            result = $result
        }
    }
    catch {
        Write-Log "  Failed: $StepName - $_" 'ERROR'
        return @{
            name = $StepName
            status = 'failed'
            error = $_.ToString()
        }
    }
}

function Generate-MasterReport {
    param([array]$StepResults)
    
    $report = @{
        timestamp = $timestamp
        organization = $OrganizationName
        projectNumber = $ProjectNumber
        repository = "$RepositoryOwner/$RepositoryName"
        dryRun = $DryRun
        startTime = $script:startTime
        endTime = Get-Date
        duration = $((Get-Date) - $script:startTime).ToString()
        steps = $StepResults
        summary = @{
            total = $StepResults.Count
            completed = ($StepResults | Where-Object { $_.status -eq 'completed' }).Count
            failed = ($StepResults | Where-Object { $_.status -eq 'failed' }).Count
            skipped = ($StepResults | Where-Object { $_.status -eq 'skipped' }).Count
        }
        success = (($StepResults | Where-Object { $_.status -eq 'failed' }).Count -eq 0)
    }
    
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $masterReportFile
    Write-Log "Master report saved: $masterReportFile" 'SUCCESS'
    
    return $report
}

function Display-MasterSummary {
    param([hashtable]$Report)
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║         HELIOS PLATFORM - COMPLETE SETUP SUMMARY            ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "`nConfiguration:" -ForegroundColor Green
    Write-Host "  Organization: $($Report.organization)"
    Write-Host "  Project #: $($Report.projectNumber)"
    Write-Host "  Repository: $($Report.repository)"
    Write-Host "  Duration: $($Report.duration)"
    
    Write-Host "`nExecution Summary:" -ForegroundColor Green
    Write-Host "  Total Steps: $($Report.summary.total)"
    Write-Host "  Completed: $($Report.summary.completed)" -ForegroundColor $(if ($Report.summary.completed -gt 0) { 'Green' } else { 'Yellow' })
    Write-Host "  Failed: $($Report.summary.failed)" -ForegroundColor $(if ($Report.summary.failed -eq 0) { 'Green' } else { 'Red' })
    Write-Host "  Skipped: $($Report.summary.skipped)" -ForegroundColor $(if ($Report.summary.skipped -eq 0) { 'Green' } else { 'Yellow' })
    
    Write-Host "`nStep Results:" -ForegroundColor Green
    foreach ($step in $Report.steps) {
        $icon = switch ($step.status) {
            'completed' { '✓' }
            'failed' { '✗' }
            'skipped' { '⊘' }
            default { '?' }
        }
        $color = switch ($step.status) {
            'completed' { 'Green' }
            'failed' { 'Red' }
            'skipped' { 'Yellow' }
            default { 'White' }
        }
        Write-Host "  $icon $($step.name)" -ForegroundColor $color
    }
    
    Write-Host "`nOverall Status: " -NoNewline
    if ($Report.success) {
        Write-Host "✓ SUCCESS" -ForegroundColor Green
    }
    else {
        Write-Host "✗ FAILED" -ForegroundColor Red
    }
    
    Write-Host "`nNext Steps:" -ForegroundColor Green
    Write-Host "  1. Verify configuration in GitHub"
    Write-Host "  2. Review detailed logs:"
    Write-Host "     - logs/complete-setup_$timestamp.log"
    Write-Host "     - logs/complete-setup-report_$timestamp.json"
    
    if (-not $DryRun) {
        Write-Host "  3. Configure team access and permissions"
        Write-Host "  4. Customize automation rules as needed"
        Write-Host "  5. Train team on new board workflows"
    }
    else {
        Write-Host "  ⚠️  DRY RUN MODE - Review output before running with actual changes"
    }
    
    Write-Host "`n"
}

# Main execution
$script:startTime = Get-Date

try {
    Write-Log '╔════════════════════════════════════════════════════════════════╗'
    Write-Log '║    HELIOS PLATFORM - COMPLETE SYSTEM SETUP ORCHESTRATOR       ║'
    Write-Log '╚════════════════════════════════════════════════════════════════╝'
    Write-Log "Started: $timestamp"
    Write-Log "Configuration:"
    Write-Log "  Organization: $OrganizationName"
    Write-Log "  Project: $ProjectNumber"
    Write-Log "  Repository: $RepositoryOwner/$RepositoryName"
    
    if ($DryRun) {
        Write-Log 'DRY RUN MODE - No actual changes will be applied' 'INFO'
    }
    
    $steps = @()
    $stepCount = 1
    $totalSteps = 4
    
    # Step 1: Board Setup
    $steps += Invoke-OrchestratedStep -StepName 'Board Setup' `
        -ScriptPath 'scripts/board-setup/setup-board.ps1' `
        -Parameters @{
            ProjectNumber = $ProjectNumber
            OrganizationName = $OrganizationName
            BoardName = 'HELIOS Platform'
        } `
        -StepNumber $stepCount++ `
        -TotalSteps $totalSteps
    
    # Step 2: GitHub Ecosystem Integration
    $steps += Invoke-OrchestratedStep -StepName 'GitHub Ecosystem Integration' `
        -ScriptPath 'scripts/integration/setup-github-ecosystem.ps1' `
        -Parameters @{
            RepositoryName = $RepositoryName
            RepositoryOwner = $RepositoryOwner
            ProjectNumber = $ProjectNumber
            OrganizationName = $OrganizationName
        } `
        -StepNumber $stepCount++ `
        -TotalSteps $totalSteps
    
    # Step 3: Monitoring Setup
    $steps += Invoke-OrchestratedStep -StepName 'Monitoring & Alerting Setup' `
        -ScriptPath 'scripts/optimization/setup-monitoring.ps1' `
        -Parameters @{
            ProjectNumber = $ProjectNumber
            OrganizationName = $OrganizationName
            RepositoryName = $RepositoryName
            RepositoryOwner = $RepositoryOwner
        } `
        -StepNumber $stepCount++ `
        -TotalSteps $totalSteps
    
    if (-not $SkipValidation) {
        # Step 4: Validation
        $steps += Invoke-OrchestratedStep -StepName 'Board Validation' `
            -ScriptPath 'scripts/board-setup/validate-board.ps1' `
            -Parameters @{
                ProjectNumber = $ProjectNumber
                OrganizationName = $OrganizationName
                GenerateReport = $true
            } `
            -StepNumber $stepCount++ `
            -TotalSteps $totalSteps
    }
    
    $report = Generate-MasterReport -StepResults $steps
    Display-MasterSummary -Report $report
    
    Write-Log '════════════════════════════════════════════════════════════════'
    Write-Log "Completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" 'SUCCESS'
    
    # Return success/failure status
    if ($report.success) {
        exit 0
    }
    else {
        exit 1
    }
}
catch {
    Write-Log "ORCHESTRATION FAILED: $_" 'ERROR'
    Write-Host "`nSetup failed. Check logs at: $logFile" -ForegroundColor Red
    exit 1
}
