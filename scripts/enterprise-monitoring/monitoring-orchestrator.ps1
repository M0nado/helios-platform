<#
.SYNOPSIS
    Enterprise Monitoring System - Main Orchestrator
.DESCRIPTION
    Central orchestration and management of all monitoring components
.VERSION
    1.0.0
#>

param(
    [ValidateSet("Dashboard", "OnPrem", "Azure", "M365", "Compliance", "Analytics", "Reporting", "All")]
    [string]$StartModule = "Dashboard",
    [switch]$Help
)

$MonitoringRoot = Split-Path -Parent $PSScriptRoot

if ($Help) {
    Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║         ENTERPRISE MONITORING SYSTEM - HELP                   ║
╚════════════════════════════════════════════════════════════════╝

Usage: .\monitoring-orchestrator.ps1 -StartModule <ModuleName>

Available Modules:
  • Dashboard        - Real-time monitoring dashboard
  • OnPrem          - On-premises system monitoring
  • Azure           - Azure cloud monitoring
  • M365            - Microsoft 365 monitoring
  • Compliance      - Compliance and governance monitoring
  • Analytics       - AI-driven analytics and anomaly detection
  • Reporting       - Report generation and scheduling
  • All             - Start all monitoring components

Examples:
  .\monitoring-orchestrator.ps1 -StartModule Dashboard
  .\monitoring-orchestrator.ps1 -StartModule All

Components:
  ├─ lib/
  │  └─ monitoring-library.ps1       Core shared functions
  ├─ dashboard/
  │  ├─ dashboard-core.ps1           Main dashboard
  │  ├─ real-time-metrics.ps1        Live metrics
  │  ├─ health-overview.ps1          System health
  │  ├─ alert-management.ps1         Alert center
  │  └─ dashboard-themes.ps1         UI themes
  ├─ on-prem/
  │  ├─ system-health.ps1            Windows monitoring
  │  └─ resource-tracking.ps1        Performance tracking
  ├─ azure/
  │  └─ azure-monitor-integration.ps1 Cloud monitoring
  ├─ m365/
  │  └─ m365-health.ps1              M365 services
  ├─ compliance/
  │  └─ purview-alerts.ps1           Compliance monitoring
  ├─ analytics/
  │  └─ anomaly-detection.ps1        ML analytics
  ├─ reporting/
  │  └─ executive-reports.ps1        Report generation
  └─ config/
     ├─ monitoring-config.json       Main config
     ├─ alert-policies.json          Alert rules
     └─ report-templates.json        Report templates

Configuration:
  Edit monitoring-config.json to customize settings:
  • Alert thresholds
  • Monitoring intervals
  • Integration endpoints
  • Notification channels

Logs:
  All activity logged to logs/ directory with daily rotation
  Logs retained for 30 days by default

Support:
  For issues or questions, refer to documentation in each module

"@
    exit 0
}

function Initialize-Monitoring {
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║         ENTERPRISE MONITORING SYSTEM - Initializing           ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Initializing shared library..." -ForegroundColor Yellow
    . "$MonitoringRoot\lib\monitoring-library.ps1"
    
    Write-Host "✓ Library loaded" -ForegroundColor Green
    Write-Host "✓ Logging initialized" -ForegroundColor Green
    Write-Host "✓ Configuration loaded" -ForegroundColor Green
    Write-Host ""
}

function Start-DashboardModule {
    Write-Host "Starting Dashboard Module..." -ForegroundColor Yellow
    . "$MonitoringRoot\dashboard\dashboard-core.ps1"
    Start-Dashboard
}

function Start-OnPremModule {
    Write-Host "Starting On-Premises Module..." -ForegroundColor Yellow
    . "$MonitoringRoot\on-prem\system-health.ps1"
    Start-OnPremMonitoring
}

function Start-AzureModule {
    Write-Host "Starting Azure Module..." -ForegroundColor Yellow
    
    $config = Get-MonitoringConfig
    $tenantId = $config.monitoring.m365.tenantId
    $subscriptionIds = $config.monitoring.azure.subscriptionIds
    
    if (-not $tenantId -or -not $subscriptionIds) {
        Write-Warning "Azure configuration incomplete. Please set TenantId and SubscriptionIds in monitoring-config.json"
        return
    }
    
    . "$MonitoringRoot\azure\azure-monitor-integration.ps1"
    Start-AzureMonitoring -TenantId $tenantId -SubscriptionIds $subscriptionIds
}

function Start-M365Module {
    Write-Host "Starting Microsoft 365 Module..." -ForegroundColor Yellow
    
    $config = Get-MonitoringConfig
    $tenantId = $config.monitoring.m365.tenantId
    
    if (-not $tenantId) {
        Write-Warning "M365 configuration incomplete. Please set TenantId in monitoring-config.json"
        return
    }
    
    . "$MonitoringRoot\m365\m365-health.ps1"
    Start-M365Monitoring -TenantId $tenantId
}

function Start-ComplianceModule {
    Write-Host "Starting Compliance Module..." -ForegroundColor Yellow
    . "$MonitoringRoot\compliance\purview-alerts.ps1"
    Start-ComplianceMonitoring
}

function Start-AnalyticsModule {
    Write-Host "Starting Analytics Module..." -ForegroundColor Yellow
    . "$MonitoringRoot\analytics\anomaly-detection.ps1"
    Start-AnomalyDetection -MetricsHistory @()
}

function Start-ReportingModule {
    Write-Host "Starting Reporting Module..." -ForegroundColor Yellow
    . "$MonitoringRoot\reporting\executive-reports.ps1"
    Start-ReportScheduler
}

function Start-AllModules {
    Write-Host "Starting all monitoring modules in parallel..." -ForegroundColor Yellow
    Write-Host "Note: Run each module in separate PowerShell windows for parallel execution" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Commands to run in separate windows:" -ForegroundColor Yellow
    Write-Host "  1. Dashboard    : .\monitoring-orchestrator.ps1 -StartModule Dashboard" -ForegroundColor Gray
    Write-Host "  2. On-Premises  : .\monitoring-orchestrator.ps1 -StartModule OnPrem" -ForegroundColor Gray
    Write-Host "  3. Azure        : .\monitoring-orchestrator.ps1 -StartModule Azure" -ForegroundColor Gray
    Write-Host "  4. Microsoft365 : .\monitoring-orchestrator.ps1 -StartModule M365" -ForegroundColor Gray
    Write-Host "  5. Compliance   : .\monitoring-orchestrator.ps1 -StartModule Compliance" -ForegroundColor Gray
    Write-Host "  6. Analytics    : .\monitoring-orchestrator.ps1 -StartModule Analytics" -ForegroundColor Gray
    Write-Host "  7. Reporting    : .\monitoring-orchestrator.ps1 -StartModule Reporting" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
Initialize-Monitoring

Write-MonitoringLog "Enterprise Monitoring System started with module: $StartModule"

switch ($StartModule) {
    "Dashboard" { Start-DashboardModule }
    "OnPrem" { Start-OnPremModule }
    "Azure" { Start-AzureModule }
    "M365" { Start-M365Module }
    "Compliance" { Start-ComplianceModule }
    "Analytics" { Start-AnalyticsModule }
    "Reporting" { Start-ReportingModule }
    "All" { Start-AllModules }
    default { Write-Error "Unknown module: $StartModule" }
}
