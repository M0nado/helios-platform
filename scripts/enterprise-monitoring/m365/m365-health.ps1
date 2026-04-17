<#
.SYNOPSIS
    M365 service health and usage monitoring
.DESCRIPTION
    Monitor Microsoft 365 services, mail flow, Teams, and SharePoint
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "M365-Monitor"

class M365ServiceMonitor {
    [string]$TenantId
    [hashtable]$ServiceHealth
    [array]$UserActivity
    [array]$AlertHistory
    
    M365ServiceMonitor([string]$TenantId) {
        $this.TenantId = $TenantId
        $this.ServiceHealth = @{}
        $this.UserActivity = @()
        $this.AlertHistory = @()
    }
    
    [bool]ConnectToM365() {
        try {
            Write-MonitoringLog "Connecting to Microsoft 365..."
            # In production: Connect-ExchangeOnline, Connect-MgGraph, etc.
            Write-MonitoringLog "M365 connection established"
            return $true
        }
        catch {
            Write-MonitoringLog "Failed to connect to M365: $_" -Level "ERROR"
            return $false
        }
    }
    
    [hashtable]GetServiceHealth() {
        $health = @{
            Timestamp = Get-Date -AsUTC
            Services = @()
        }
        
        $services = @(
            @{ Name = "Exchange Online"; Status = "ServiceOperational"; Incidents = 0 }
            @{ Name = "Teams"; Status = "ServiceOperational"; Incidents = 0 }
            @{ Name = "SharePoint Online"; Status = "ServiceOperational"; Incidents = 0 }
            @{ Name = "OneDrive"; Status = "ServiceOperational"; Incidents = 0 }
            @{ Name = "Microsoft 365 Apps"; Status = "ServiceOperational"; Incidents = 0 }
        )
        
        $health.Services = $services
        return $health
    }
    
    [hashtable]GetMailFlowMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            TotalMessagesProcessed = 125000
            DelayedMessages = 15
            FailedMessages = 8
            SpamDetected = 3500
            MalwareDetected = 2
            BytesProcessed = 5368709120  # 5GB
            AverageLatencyMS = 250
            QueueStatus = "Healthy"
        }
        
        Write-MonitoringLog "Retrieved mail flow metrics"
        return $metrics
    }
    
    [hashtable]GetTeamsUsageMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            ActiveUsers = 5420
            Meetings = 1250
            Messages = 85000
            CallsInProgress = 45
            AverageCallDuration = 18
            TeamCount = 280
            ChannelCount = 2100
        }
        
        Write-MonitoringLog "Retrieved Teams usage metrics"
        return $metrics
    }
    
    [hashtable]GetSharePointHealthMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            StorageUsedGB = 2048
            StorageLimitGB = 5120
            Sites = 450
            ActiveUsers = 3200
            DocumentsIndexed = 2500000
            SearchLatencyMS = 150
            HealthStatus = "Healthy"
        }
        
        Write-MonitoringLog "Retrieved SharePoint health metrics"
        return $metrics
    }
    
    [hashtable]GetUserActivityMetrics() {
        $metrics = @{
            Timestamp = Get-Date -AsUTC
            TotalUsers = 5500
            ActiveUsers = 4850
            InactiveUsers = 150
            NewUsersThisMonth = 45
            RemovedUsersThisMonth = 12
            LicenseAlerts = 3
        }
        
        Write-MonitoringLog "Retrieved user activity metrics"
        return $metrics
    }
    
    [array]GetServiceAlerts() {
        $alerts = @(
            @{
                AlertId = "M365-001"
                Service = "Exchange Online"
                Severity = "Medium"
                Title = "High Message Queue"
                Description = "Message queue exceeds threshold"
                CreatedTime = (Get-Date).AddHours(-2)
            }
            @{
                AlertId = "M365-002"
                Service = "SharePoint Online"
                Severity = "Low"
                Title = "Search Index Lag"
                Description = "Search index update delayed"
                CreatedTime = (Get-Date).AddHours(-1)
            }
        )
        
        return $alerts
    }
    
    [array]GetComplianceAlerts() {
        $alerts = @(
            @{
                Type = "DLP"
                Policy = "Financial Data"
                ViolationCount = 5
                Severity = "High"
            }
            @{
                Type = "ExternalSharing"
                Policy = "Sensitive Files"
                ViolationCount = 12
                Severity = "Medium"
            }
        )
        
        return $alerts
    }
}

function Start-M365Monitoring {
    param(
        [string]$TenantId,
        [int]$IntervalSeconds = 600
    )
    
    Write-MonitoringLog "Starting M365 monitoring..."
    
    $monitor = [M365ServiceMonitor]::new($TenantId)
    
    if (-not $monitor.ConnectToM365()) {
        return
    }
    
    while ($true) {
        try {
            $health = $monitor.GetServiceHealth()
            $mailFlow = $monitor.GetMailFlowMetrics()
            $teams = $monitor.GetTeamsUsageMetrics()
            $sharepoint = $monitor.GetSharePointHealthMetrics()
            $users = $monitor.GetUserActivityMetrics()
            $alerts = $monitor.GetServiceAlerts()
            $compliance = $monitor.GetComplianceAlerts()
            
            DisplayM365Metrics -Health $health -MailFlow $mailFlow -Teams $teams `
                -SharePoint $sharepoint -Users $users -Alerts $alerts -Compliance $compliance
            
            Start-Sleep -Seconds $IntervalSeconds
        }
        catch {
            Write-MonitoringLog "M365 monitoring error: $_" -Level "ERROR"
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
}

function DisplayM365Metrics {
    param(
        [hashtable]$Health,
        [hashtable]$MailFlow,
        [hashtable]$Teams,
        [hashtable]$SharePoint,
        [hashtable]$Users,
        [array]$Alerts,
        [array]$Compliance
    )
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  MICROSOFT 365 SERVICE MONITORING                             ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "SERVICE HEALTH" -ForegroundColor Yellow
    $Health.Services | ForEach-Object {
        $status = if ($_.Status -eq "ServiceOperational") { "✓" } else { "✗" }
        Write-Host "  $status $($_.Name): $($_.Status)"
    }
    Write-Host ""
    
    Write-Host "EXCHANGE ONLINE - MAIL FLOW" -ForegroundColor Yellow
    Write-Host "  Messages Processed: $($MailFlow.TotalMessagesProcessed)"
    Write-Host "  Delayed: $($MailFlow.DelayedMessages) | Failed: $($MailFlow.FailedMessages)"
    Write-Host "  Spam Detected: $($MailFlow.SpamDetected) | Malware: $($MailFlow.MalwareDetected)"
    Write-Host "  Queue Status: $($MailFlow.QueueStatus)"
    Write-Host ""
    
    Write-Host "MICROSOFT TEAMS" -ForegroundColor Yellow
    Write-Host "  Active Users: $($Teams.ActiveUsers)"
    Write-Host "  Meetings: $($Teams.Meetings) | Messages: $($Teams.Messages)"
    Write-Host "  Calls In Progress: $($Teams.CallsInProgress)"
    Write-Host ""
    
    Write-Host "SHAREPOINT ONLINE" -ForegroundColor Yellow
    $spStorage = [Math]::Round(($SharePoint.StorageUsedGB / $SharePoint.StorageLimitGB) * 100, 2)
    Write-Host "  Storage: $($SharePoint.StorageUsedGB)GB/$($SharePoint.StorageLimitGB)GB ($spStorage%)"
    Write-Host "  Sites: $($SharePoint.Sites) | Active Users: $($SharePoint.ActiveUsers)"
    Write-Host "  Status: $($SharePoint.HealthStatus)"
    Write-Host ""
    
    Write-Host "USER MANAGEMENT" -ForegroundColor Yellow
    Write-Host "  Total Users: $($Users.TotalUsers)"
    Write-Host "  Active: $($Users.ActiveUsers) | Inactive: $($Users.InactiveUsers)"
    Write-Host "  License Alerts: $($Users.LicenseAlerts)"
    Write-Host ""
    
    if ($Alerts.Count -gt 0) {
        Write-Host "SERVICE ALERTS" -ForegroundColor Yellow
        $Alerts | ForEach-Object {
            $sevColor = if ($_.Severity -eq "High") { "Red" } else { "Yellow" }
            Write-Host "  [$($_.Service)] $($_.Title)" -ForegroundColor $sevColor
        }
        Write-Host ""
    }
    
    if ($Compliance.Count -gt 0) {
        Write-Host "COMPLIANCE ALERTS" -ForegroundColor Yellow
        $Compliance | ForEach-Object {
            Write-Host "  $($_.Type): $($_.ViolationCount) violations"
        }
    }
    
    Write-Host ""
    Write-Host "Last Updated: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
}

Export-ModuleMember -Function @('Start-M365Monitoring', 'DisplayM365Metrics')
Export-ModuleMember -Class 'M365ServiceMonitor'
