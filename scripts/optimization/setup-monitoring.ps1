<#
.SYNOPSIS
    Monitoring and alerting setup for HELIOS Platform
.DESCRIPTION
    Configures performance metrics, health checks, alerts, and reporting
.PARAMETER ProjectNumber
    Project number
.PARAMETER OrganizationName
    Organization name
.PARAMETER RepositoryName
    Repository name
.PARAMETER RepositoryOwner
    Repository owner
.PARAMETER Verbose
    Detailed output
#>

param(
    [Parameter(Mandatory=$true)]
    [int]$ProjectNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$OrganizationName,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryName,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryOwner,
    
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$logFile = "logs/monitoring-setup_$timestamp.log"
$reportFile = "logs/monitoring-report_$timestamp.json"

if (-not (Test-Path 'logs')) { New-Item -ItemType Directory -Path 'logs' -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'HH:mm:ss'
    $entry = "[$ts] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Verbose -or $Level -eq 'ERROR' -or $Level -eq 'SUCCESS') { Write-Host $entry }
}

# Monitoring configuration
$monitoringConfig = @{
    metrics = @{
        name = 'Performance Metrics'
        items = @(
            @{ name = 'Workflow Duration'; threshold = 300; unit = 'seconds' }
            @{ name = 'Deployment Success Rate'; threshold = 95; unit = 'percent' }
            @{ name = 'Build Cache Hit Rate'; threshold = 70; unit = 'percent' }
            @{ name = 'Average Response Time'; threshold = 200; unit = 'ms' }
            @{ name = 'Error Rate'; threshold = 1; unit = 'percent' }
            @{ name = 'Resource Utilization'; threshold = 80; unit = 'percent' }
        )
    }
    healthChecks = @{
        name = 'Integration Health Checks'
        items = @(
            @{ name = 'GitHub API Availability'; interval = 60; timeout = 10 }
            @{ name = 'Project Board Sync'; interval = 300; timeout = 30 }
            @{ name = 'Workflow Engine'; interval = 120; timeout = 15 }
            @{ name = 'Deployment Pipeline'; interval = 180; timeout = 20 }
            @{ name = 'Documentation Site'; interval = 600; timeout = 10 }
        )
    }
    alerts = @{
        name = 'Alert Routing'
        rules = @(
            @{
                condition = 'Workflow Duration > Threshold'
                severity = 'Warning'
                notification = @('slack', 'email')
                escalation = 'After 2 occurrences'
            },
            @{
                condition = 'Deployment Success Rate < Threshold'
                severity = 'Critical'
                notification = @('slack', 'email', 'pagerduty')
                escalation = 'Immediate'
            },
            @{
                condition = 'Health Check Failed'
                severity = 'High'
                notification = @('slack', 'teams')
                escalation = 'After 5 minutes'
            },
            @{
                condition = 'Error Rate > Threshold'
                severity = 'High'
                notification = @('slack', 'email')
                escalation = 'After 3 occurrences'
            },
            @{
                condition = 'Resource Utilization > Threshold'
                severity = 'Medium'
                notification = @('email')
                escalation = 'Daily summary'
            }
        )
    }
}

function Setup-PerformanceMetrics {
    Write-Log 'Setting up performance metrics...'
    
    $metricsConfig = @{
        metrics = $monitoringConfig.metrics.items
        collectionInterval = 60
        retention = 30
        aggregation = 'hourly'
        createdAt = Get-Date -Format 'o'
    }
    
    if (-not (Test-Path '.monitoring')) {
        New-Item -ItemType Directory -Path '.monitoring' -Force | Out-Null
    }
    
    try {
        $metricsConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.monitoring/metrics-config.json'
        Write-Log "  Performance metrics configured: $($monitoringConfig.metrics.items.Count) metrics" 'SUCCESS'
        return @{ name = 'Metrics'; status = 'configured'; count = $monitoringConfig.metrics.items.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'Metrics'; status = 'failed'; error = $_ }
    }
}

function Setup-HealthChecks {
    Write-Log 'Setting up health checks...'
    
    $healthConfig = @{
        checks = $monitoringConfig.healthChecks.items
        endpoint = "https://api.github.com"
        timeout = 30
        retries = 3
        createdAt = Get-Date -Format 'o'
    }
    
    try {
        $healthConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.monitoring/health-checks-config.json'
        Write-Log "  Health checks configured: $($monitoringConfig.healthChecks.items.Count) checks" 'SUCCESS'
        return @{ name = 'HealthChecks'; status = 'configured'; count = $monitoringConfig.healthChecks.items.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'HealthChecks'; status = 'failed'; error = $_ }
    }
}

function Setup-Alerts {
    Write-Log 'Setting up alert routing...'
    
    $alertConfig = @{
        rules = $monitoringConfig.alerts.rules
        notificationChannels = @('slack', 'email', 'teams', 'pagerduty')
        escalationPolicy = @{
            severity = @{
                'Critical' = @{ initial = 0; escalate = 0 }
                'High' = @{ initial = 300; escalate = 600 }
                'Medium' = @{ initial = 600; escalate = 1800 }
                'Warning' = @{ initial = 1800; escalate = 3600 }
            }
        }
        createdAt = Get-Date -Format 'o'
    }
    
    try {
        $alertConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.monitoring/alerts-config.json'
        Write-Log "  Alert routing configured: $($monitoringConfig.alerts.rules.Count) rules" 'SUCCESS'
        return @{ name = 'Alerts'; status = 'configured'; count = $monitoringConfig.alerts.rules.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'Alerts'; status = 'failed'; error = $_ }
    }
}

function Setup-Dashboard {
    Write-Log 'Setting up monitoring dashboard...'
    
    $dashboardConfig = @{
        title = 'HELIOS Platform - Monitoring Dashboard'
        projectNumber = $ProjectNumber
        organization = $OrganizationName
        repository = "$RepositoryOwner/$RepositoryName"
        widgets = @(
            @{
                type = 'metric'
                title = 'Workflow Duration'
                metric = 'workflow_duration'
                threshold = 300
            },
            @{
                type = 'metric'
                title = 'Deployment Success Rate'
                metric = 'deployment_success_rate'
                threshold = 95
            },
            @{
                type = 'timeseries'
                title = 'Build Times Over Time'
                metric = 'build_duration'
            },
            @{
                type = 'health'
                title = 'Integration Health'
                checks = @('github_api', 'project_board', 'workflow_engine')
            },
            @{
                type = 'alerts'
                title = 'Recent Alerts'
                severity = @('Critical', 'High')
            },
            @{
                type = 'resource'
                title = 'Resource Utilization'
                resources = @('cpu', 'memory', 'disk')
            }
        )
        refreshInterval = 300
        createdAt = Get-Date -Format 'o'
    }
    
    try {
        $dashboardConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.monitoring/dashboard-config.json'
        Write-Log "  Dashboard configured: $($dashboardConfig.widgets.Count) widgets" 'SUCCESS'
        return @{ name = 'Dashboard'; status = 'configured'; widgets = $dashboardConfig.widgets.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'Dashboard'; status = 'failed'; error = $_ }
    }
}

function Setup-Reporting {
    Write-Log 'Setting up reporting...'
    
    $reportConfig = @{
        schedules = @(
            @{
                name = 'Daily Standup'
                frequency = 'Daily'
                time = '09:00 UTC'
                recipients = @('team@company.com')
                metrics = @('workflow_duration', 'deployment_success_rate', 'error_rate')
            },
            @{
                name = 'Weekly Performance Review'
                frequency = 'Weekly'
                day = 'Friday'
                time = '17:00 UTC'
                recipients = @('management@company.com')
                metrics = @('all')
            },
            @{
                name = 'Monthly Summary'
                frequency = 'Monthly'
                day = 'Last Friday'
                time = '17:00 UTC'
                recipients = @('executive@company.com')
                metrics = @('all')
            }
        )
        templates = @{
            dailyTemplate = 'summary'
            weeklyTemplate = 'detailed'
            monthlyTemplate = 'comprehensive'
        }
        createdAt = Get-Date -Format 'o'
    }
    
    try {
        $reportConfig | ConvertTo-Json -Depth 10 | Set-Content -Path '.monitoring/reporting-config.json'
        Write-Log "  Reporting configured: $($reportConfig.schedules.Count) schedules" 'SUCCESS'
        return @{ name = 'Reporting'; status = 'configured'; schedules = $reportConfig.schedules.Count }
    }
    catch {
        Write-Log "  Failed: $_" 'ERROR'
        return @{ name = 'Reporting'; status = 'failed'; error = $_ }
    }
}

function Generate-MonitoringGuide {
    $doc = @"
# Monitoring & Alerting Setup

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Overview

Monitoring is configured for the HELIOS Platform project board and GitHub ecosystem integrations.

### Project Details
- Project: #$ProjectNumber
- Organization: $OrganizationName
- Repository: $RepositoryOwner/$RepositoryName

## Performance Metrics

The following metrics are continuously monitored:

| Metric | Threshold | Unit |
|--------|-----------|------|
$(foreach ($metric in $monitoringConfig.metrics.items) {
"| $($metric.name) | $($metric.threshold) | $($metric.unit) |`n"
})

## Health Checks

Automated health checks verify system status:

$(foreach ($check in $monitoringConfig.healthChecks.items) {
"- **$($check.name)**: Every $($check.interval)s (timeout: $($check.timeout)s)`n"
})

## Alert Rules

Alerts are triggered based on these conditions:

$(foreach ($rule in $monitoringConfig.alerts.rules) {
@"
### $($rule.condition)
- **Severity**: $($rule.severity)
- **Notification**: $(($rule.notification) -join ', ')
- **Escalation**: $($rule.escalation)

"@
})

## Dashboard Access

The monitoring dashboard is available at:
- URL: https://github.com/orgs/$OrganizationName/projects/$ProjectNumber/views
- Refresh: Every 5 minutes
- Widgets: 6 monitoring views

## Reporting Schedule

| Report | Frequency | Time | Recipients |
|--------|-----------|------|------------|
| Daily Standup | Daily | 09:00 UTC | team@company.com |
| Weekly Review | Weekly (Friday) | 17:00 UTC | management@company.com |
| Monthly Summary | Monthly | Last Friday 17:00 UTC | executive@company.com |

## Alert Escalation

**Critical Alerts**: Immediate escalation
**High Alerts**: 5-minute escalation
**Medium Alerts**: 30-minute escalation
**Warning Alerts**: 1-hour escalation

## Troubleshooting

### No Alerts Received
1. Check notification channels are configured
2. Verify alert rules in `.monitoring/alerts-config.json`
3. Check logs for errors

### Dashboard Not Updating
1. Verify dashboard refresh interval
2. Check metric collection status
3. Restart monitoring service

### Health Checks Failing
1. Verify network connectivity
2. Check endpoint availability
3. Review health check timeouts

"@
    
    $doc | Set-Content -Path 'logs/monitoring-setup-guide_$timestamp.md'
    Write-Log "Monitoring guide generated" 'SUCCESS'
}

try {
    Write-Log '=== Starting Monitoring & Alerting Setup ===' 'INFO'
    Write-Log "Project: #$ProjectNumber, Org: $OrganizationName"
    Write-Log "Repository: $RepositoryOwner/$RepositoryName"
    
    $results = @()
    
    $results += Setup-PerformanceMetrics
    $results += Setup-HealthChecks
    $results += Setup-Alerts
    $results += Setup-Dashboard
    $results += Setup-Reporting
    
    Generate-MonitoringGuide
    
    $report = @{
        timestamp = $timestamp
        projectNumber = $ProjectNumber
        organization = $OrganizationName
        repository = "$RepositoryOwner/$RepositoryName"
        components = $results
        summary = @{
            configured = ($results | Where-Object { $_.status -eq 'configured' }).Count
            failed = ($results | Where-Object { $_.status -eq 'failed' }).Count
        }
    }
    
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportFile
    
    Write-Log '=== Monitoring Setup Complete ===' 'SUCCESS'
    Write-Log "Configured: $($report.summary.configured), Failed: $($report.summary.failed)" 'INFO'
    
    $report | ConvertTo-Json -Depth 10
}
catch {
    Write-Log "Script failed: $_" 'ERROR'
    exit 1
}
