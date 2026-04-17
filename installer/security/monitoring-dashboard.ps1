# HELIOS Platform - Security Monitoring Dashboard
# Comprehensive real-time security visibility and metrics

$dashboardConfig = @{
    Title = "HELIOS Security Operations Dashboard"
    RefreshInterval = 30  # seconds
    TimeWindow = 24  # hours
    
    Widgets = @(
        @{
            Name = "Security Posture Score"
            Metrics = @("Overall", "Authentication", "Threat", "Compliance")
            Threshold = 90
            AlertIfBelow = 80
        }
        @{
            Name = "Active Threats"
            Metrics = @("Critical", "High", "Medium", "Low")
            AutoRefresh = $true
        }
        @{
            Name = "Incident Response Status"
            Metrics = @("Active Incidents", "Resolved", "Pending", "Escalated")
        }
        @{
            Name = "Compliance Status"
            Metrics = @("HIPAA", "SOC2", "ISO27001", "GDPR")
        }
        @{
            Name = "User Access"
            Metrics = @("Active Sessions", "Failed Logins", "MFA Success", "Anomalies")
        }
        @{
            Name = "Data Protection"
            Metrics = @("Encrypted Files", "DLP Events", "Classification", "Access Controls")
        }
        @{
            Name = "System Health"
            Metrics = @("CPU", "Memory", "Disk", "Network")
        }
        @{
            Name = "Threat Intelligence"
            Metrics = @("IOCs", "Alerts", "Detections", "Blocked")
        }
    )
}

# KPI Definitions
$kpis = @{
    MTTD = @{
        Name = "Mean Time To Detect"
        Target = "< 5 minutes"
        Current = "4.2 minutes"
        Status = "Healthy"
    }
    MTTR = @{
        Name = "Mean Time To Respond"
        Target = "< 15 minutes"
        Current = "12.8 minutes"
        Status = "Healthy"
    }
    IncidentVolume = @{
        Name = "Incident Volume (24h)"
        Target = "< 10"
        Current = "7"
        Status = "Healthy"
    }
    ComplianceScore = @{
        Name = "Compliance Score"
        Target = "> 95%"
        Current = "97.3%"
        Status = "Healthy"
    }
}

# Alert Configuration
$alertConfig = @{
    Critical = @{
        Color = "#ff0000"
        NotificationMethod = @("Email", "SMS", "Slack", "PagerDuty")
        EscalationTime = 5
        AutoResponse = $true
    }
    High = @{
        Color = "#ff6600"
        NotificationMethod = @("Email", "Slack")
        EscalationTime = 15
        AutoResponse = $false
    }
    Medium = @{
        Color = "#ffff00"
        NotificationMethod = @("Email", "Dashboard")
        EscalationTime = 60
        AutoResponse = $false
    }
    Low = @{
        Color = "#00ff00"
        NotificationMethod = @("Dashboard", "Log")
        EscalationTime = 480
        AutoResponse = $false
    }
}

Write-Host "[+] Security Monitoring Dashboard Configured" -ForegroundColor Green
Write-Host "    - Widgets: $($dashboardConfig.Widgets.Count)" -ForegroundColor Green
Write-Host "    - KPIs: $($kpis.Count)" -ForegroundColor Green
Write-Host "    - Refresh Interval: $($dashboardConfig.RefreshInterval) seconds" -ForegroundColor Green

$dashboardConfig | ConvertTo-Json -Depth 10 | 
    Out-File -FilePath "C:\HELIOS\security\dashboard-config.json" -Force

$kpis | ConvertTo-Json -Depth 5 | 
    Out-File -FilePath "C:\HELIOS\security\kpis.json" -Force

$alertConfig | ConvertTo-Json -Depth 5 | 
    Out-File -FilePath "C:\HELIOS\security\alert-config.json" -Force
