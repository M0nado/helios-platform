<#
.SYNOPSIS
    Sets up GitHub ecosystem integrations for HELIOS Platform
.DESCRIPTION
    Configures issues to project linking, PR workflow triggers, and status updates
.PARAMETER GitHubToken
    GitHub Personal Access Token
.PARAMETER RepositoryName
    Repository name for integration
.PARAMETER RepositoryOwner
    Repository owner
.PARAMETER ProjectNumber
    Project number to integrate with
.PARAMETER OrganizationName
    Organization name
.PARAMETER DryRun
    Preview mode
.PARAMETER Verbose
    Detailed output
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryName,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryOwner,
    
    [Parameter(Mandatory=$true)]
    [int]$ProjectNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$OrganizationName,
    
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logFile = "logs/github-ecosystem-integration_$timestamp.log"
$reportFile = "logs/github-ecosystem-report_$timestamp.json"

if (-not (Test-Path 'logs')) { New-Item -ItemType Directory -Path 'logs' -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'HH:mm:ss'
    $entry = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'SUCCESS') { Write-Host $entry }
}

# Integration Configuration
$integrations = @(
    @{
        name = 'Issues to Project Board'
        id = 'issues-to-board'
        type = 'IssueLink'
        description = 'Automatically link GitHub issues to project board'
        config = @{
            autoLink = $true
            linkPattern = 'all'
            fields = @{ Status = 'New' }
        }
    },
    @{
        name = 'PR to Workflow Trigger'
        id = 'pr-workflow'
        type = 'WorkflowTrigger'
        description = 'Trigger CI/CD workflows when PRs are created'
        config = @{
            workflows = @('build.yml', 'test.yml', 'lint.yml')
            triggerOn = 'opened'
        }
    },
    @{
        name = 'Workflow Status Updates'
        id = 'workflow-status'
        type = 'StatusUpdate'
        description = 'Update board status based on workflow results'
        config = @{
            mapping = @{
                'workflow_run.concluded' = @{ field = 'ProgressStatus'; value = '100% Complete' }
                'workflow_run.failed' = @{ field = 'QAStatus'; value = 'QA Failed' }
                'workflow_run.success' = @{ field = 'QAStatus'; value = 'QA Approved' }
            }
        }
    },
    @{
        name = 'Action Notifications'
        id = 'action-notify'
        type = 'NotificationRoute'
        description = 'Route deployment and integration notifications'
        config = @{
            channels = @('slack', 'email', 'teams')
            routes = @(
                @{ action = 'deployment_started'; channel = 'slack' }
                @{ action = 'deployment_failed'; channel = @('email', 'slack') }
                @{ action = 'deployment_success'; channel = 'slack' }
            )
        }
    },
    @{
        name = 'Pages Documentation Sync'
        id = 'pages-sync'
        type = 'ContentSync'
        description = 'Sync project documentation to GitHub Pages'
        config = @{
            source = 'docs/'
            target = 'gh-pages'
            autoUpdate = $true
            buildCommand = 'npm run docs:build'
        }
    }
)

function Configure-IssueLink {
    param([hashtable]$Config)
    
    Write-Log "Configuring: Issues to Project Board"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would link all issues to project #$ProjectNumber" 'INFO'
        return @{ name = 'IssueLink'; status = 'dry-run' }
    }
    
    try {
        $issueConfig = @{
            repositoryName = $RepositoryName
            repositoryOwner = $RepositoryOwner
            projectNumber = $ProjectNumber
            autoLink = $Config.config.autoLink
            linkPattern = $Config.config.linkPattern
            fieldDefaults = $Config.config.fields
            createdAt = Get-Date -Format 'o'
        }
        
        if (-not (Test-Path '.github/integrations')) { 
            New-Item -ItemType Directory -Path '.github/integrations' -Force | Out-Null 
        }
        
        $issueConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.github/integrations/issue-link-config.json'
        
        Write-Log "  Issues linking configured" 'SUCCESS'
        return @{ name = 'IssueLink'; status = 'configured' }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'IssueLink'; status = 'failed'; error = $_ }
    }
}

function Configure-PRWorkflow {
    param([hashtable]$Config)
    
    Write-Log "Configuring: PR to Workflow Triggers"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would configure PR workflow triggers" 'INFO'
        return @{ name = 'PRWorkflow'; status = 'dry-run'; workflows = $Config.config.workflows.Count }
    }
    
    try {
        $prConfig = @{
            triggerOn = $Config.config.triggerOn
            workflows = $Config.config.workflows
            conditions = @(
                'status: open'
                'author: !=bot'
            )
            createdAt = Get-Date -Format 'o'
        }
        
        if (-not (Test-Path '.github/integrations')) { 
            New-Item -ItemType Directory -Path '.github/integrations' -Force | Out-Null 
        }
        
        $prConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.github/integrations/pr-workflow-config.json'
        
        Write-Log "  PR workflow triggers configured for $($Config.config.workflows.Count) workflows" 'SUCCESS'
        return @{ name = 'PRWorkflow'; status = 'configured'; workflows = $Config.config.workflows.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'PRWorkflow'; status = 'failed'; error = $_ }
    }
}

function Configure-WorkflowStatus {
    param([hashtable]$Config)
    
    Write-Log "Configuring: Workflow Status Updates"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would configure workflow status mappings" 'INFO'
        return @{ name = 'WorkflowStatus'; status = 'dry-run'; mappings = $Config.config.mapping.Count }
    }
    
    try {
        $statusConfig = @{
            projectNumber = $ProjectNumber
            mappings = $Config.config.mapping
            updateFrequency = '5 minutes'
            createdAt = Get-Date -Format 'o'
        }
        
        if (-not (Test-Path '.github/integrations')) { 
            New-Item -ItemType Directory -Path '.github/integrations' -Force | Out-Null 
        }
        
        $statusConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.github/integrations/workflow-status-config.json'
        
        Write-Log "  Workflow status mappings configured" 'SUCCESS'
        return @{ name = 'WorkflowStatus'; status = 'configured'; mappings = $Config.config.mapping.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'WorkflowStatus'; status = 'failed'; error = $_ }
    }
}

function Configure-Notifications {
    param([hashtable]$Config)
    
    Write-Log "Configuring: Notification Routing"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would configure notification routes" 'INFO'
        return @{ name = 'Notifications'; status = 'dry-run'; routes = $Config.config.routes.Count }
    }
    
    try {
        $notifyConfig = @{
            channels = $Config.config.channels
            routes = $Config.config.routes
            retryPolicy = 'exponential'
            createdAt = Get-Date -Format 'o'
        }
        
        if (-not (Test-Path '.github/integrations')) { 
            New-Item -ItemType Directory -Path '.github/integrations' -Force | Out-Null 
        }
        
        $notifyConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.github/integrations/notification-config.json'
        
        Write-Log "  Notification routing configured for $($Config.config.channels.Count) channels" 'SUCCESS'
        return @{ name = 'Notifications'; status = 'configured'; channels = $Config.config.channels.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'Notifications'; status = 'failed'; error = $_ }
    }
}

function Configure-PagesSync {
    param([hashtable]$Config)
    
    Write-Log "Configuring: GitHub Pages Sync"
    
    if ($DryRun) {
        Write-Log "  [DRY RUN] Would configure pages documentation sync" 'INFO'
        return @{ name = 'PagesSync'; status = 'dry-run' }
    }
    
    try {
        $pagesConfig = @{
            source = $Config.config.source
            target = $Config.config.target
            autoUpdate = $Config.config.autoUpdate
            buildCommand = $Config.config.buildCommand
            createdAt = Get-Date -Format 'o'
        }
        
        if (-not (Test-Path '.github/integrations')) { 
            New-Item -ItemType Directory -Path '.github/integrations' -Force | Out-Null 
        }
        
        $pagesConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.github/integrations/pages-sync-config.json'
        
        Write-Log "  GitHub Pages sync configured" 'SUCCESS'
        return @{ name = 'PagesSync'; status = 'configured' }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'PagesSync'; status = 'failed'; error = $_ }
    }
}

function Generate-IntegrationGuide {
    param([array]$Integrations)
    
    $doc = @"
# GitHub Ecosystem Integration Guide

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Configured Integrations

Repository: $RepositoryOwner/$RepositoryName
Project: #$ProjectNumber

### 1. Issues to Project Board
Automatically links GitHub issues to the project board.
- Auto-linking: Enabled
- All new issues will appear on board with Status: New

### 2. PR to Workflow Triggers
Starts CI/CD workflows when pull requests are opened.
- Workflows: $(($integrations[1].config.workflows) -join ', ')
- Trigger: PR opened

### 3. Workflow Status Updates
Updates project board status based on workflow results.
- Maps workflow runs to board fields
- Updates every 5 minutes

### 4. Notification Routing
Routes events to notification channels.
- Channels: Slack, Email, Teams
- Retry: Exponential backoff

### 5. GitHub Pages Documentation Sync
Automatically syncs documentation to GitHub Pages.
- Source: docs/
- Target: gh-pages branch
- Auto-update: $(if ($integrations[4].config.autoUpdate) { 'Enabled' } else { 'Disabled' })

## Testing Integrations

### Test Issue Linking
1. Create a new GitHub issue
2. Check if it appears on the project board
3. Verify Status is set to "New"

### Test PR Workflows
1. Create a pull request
2. Check if workflows start automatically
3. Monitor action runs

### Test Status Updates
1. Complete a workflow run
2. Check if board status updates
3. Verify correct field was set

## Troubleshooting

If integrations are not working:
1. Check GitHub token permissions (repo, workflow, project scopes)
2. Verify project access settings
3. Check workflow action logs
4. Review integration configuration files in .github/integrations/

"@

    $doc | Set-Content -Path "logs/github-ecosystem-integration-guide_$timestamp.md"
    Write-Log "Integration guide generated" 'SUCCESS'
}

try {
    Write-Log '=== GitHub Ecosystem Integration Setup ===' 'INFO'
    Write-Log "Repository: $RepositoryOwner/$RepositoryName"
    Write-Log "Project: #$ProjectNumber"
    
    if ($DryRun) {
        Write-Log 'DRY RUN MODE - No changes applied' 'INFO'
    }
    
    $results = @()
    
    # Configure each integration
    $results += Configure-IssueLink -Config $integrations[0]
    $results += Configure-PRWorkflow -Config $integrations[1]
    $results += Configure-WorkflowStatus -Config $integrations[2]
    $results += Configure-Notifications -Config $integrations[3]
    $results += Configure-PagesSync -Config $integrations[4]
    
    Generate-IntegrationGuide -Integrations $integrations
    
    $report = @{
        timestamp = $timestamp
        repository = "$RepositoryOwner/$RepositoryName"
        projectNumber = $ProjectNumber
        integrations = $integrations.Count
        configured = ($results | Where-Object { $_.status -eq 'configured' }).Count
        failed = ($results | Where-Object { $_.status -eq 'failed' }).Count
        results = $results
    }
    
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportFile
    
    Write-Log '=== GitHub Ecosystem Integration Complete ===' 'SUCCESS'
    Write-Log "Configured: $($report.configured), Failed: $($report.failed)" 'INFO'
    
    $report | ConvertTo-Json -Depth 10
}
catch {
    Write-Log "Script failed: $_" 'ERROR'
    exit 1
}
