<#
.SYNOPSIS
Incident management system for HELIOS Platform

.DESCRIPTION
Tracks and manages incidents:
- Automatic incident creation from alerts
- Manual incident creation
- Lifecycle tracking (new → assigned → investigating → resolved)
- Root cause documentation
- Resolution tracking
- MTTR/MTBF calculations
- Incident metrics and statistics

.PARAMETER DatabasePath
Path to monitoring database

.EXAMPLE
.\06-incident-manager.ps1
#>

param(
    [string]$DatabasePath = "C:\Users\ADMIN\helios-platform\monitoring\data\helios_monitoring.db"
)

class Incident {
    [string]$IncidentNumber
    [string]$Title
    [string]$Description
    [string]$Component
    [string]$Severity
    [string]$Status
    [datetime]$CreatedAt
    [datetime]$AssignedAt
    [datetime]$ResolvedAt
    [string]$AssignedTo
    [string]$InvestigationNotes
    [string]$RootCause
    [string]$Resolution
    [array]$RelatedAlerts
    [array]$Attendees
    
    Incident([string]$title, [string]$component, [string]$severity) {
        $this.IncidentNumber = "INC-$(Get-Date -Format 'yyyyMMdd')-$(Get-Random -Minimum 10000 -Maximum 99999)"
        $this.Title = $title
        $this.Component = $component
        $this.Severity = $severity
        $this.Status = "NEW"
        $this.CreatedAt = Get-Date
        $this.RelatedAlerts = @()
        $this.Attendees = @()
    }
    
    [int]GetMTTR() {
        if ($this.AssignedAt -and $this.CreatedAt) {
            return [int]((New-TimeSpan -Start $this.CreatedAt -End $this.AssignedAt).TotalMinutes)
        }
        return 0
    }
    
    [int]GetMTBF() {
        if ($this.ResolvedAt -and $this.AssignedAt) {
            return [int]((New-TimeSpan -Start $this.AssignedAt -End $this.ResolvedAt).TotalMinutes)
        }
        return 0
    }
    
    [string]GetStatus() {
        return $this.Status
    }
    
    [void]Assign([string]$assignedTo) {
        $this.AssignedTo = $assignedTo
        $this.AssignedAt = Get-Date
        $this.Status = "ASSIGNED"
    }
    
    [void]StartInvestigation() {
        $this.Status = "INVESTIGATING"
    }
    
    [void]Resolve([string]$rootCause, [string]$resolution) {
        $this.RootCause = $rootCause
        $this.Resolution = $resolution
        $this.ResolvedAt = Get-Date
        $this.Status = "RESOLVED"
    }
}

# Active incidents
$Incidents = @()

# Sample incidents for demonstration
$SampleIncidents = @(
    [Incident]::new("High Error Rate", "HELIOS.Platform", "CRITICAL"),
    [Incident]::new("Memory Spike", "Analytics-Core", "WARNING"),
    [Incident]::new("Cloud Integration Timeout", "Cloud-Bridge", "WARNING"),
    [Incident]::new("Security Audit Failed", "Security-Engine", "CRITICAL")
)

# Initialize sample incidents
$SampleIncidents[0].Assign("alice@helios.local")
$SampleIncidents[0].StartInvestigation()
$SampleIncidents[0].InvestigationNotes = "Identified faulty query in authentication service"

$SampleIncidents[1].Assign("bob@helios.local")
$SampleIncidents[1].StartInvestigation()

$SampleIncidents[2].Status = "ASSIGNED"
$SampleIncidents[2].AssignedTo = "charlie@helios.local"
$SampleIncidents[2].AssignedAt = (Get-Date).AddMinutes(-45)

$SampleIncidents[3].Status = "NEW"

# Function to create incident
function New-IncidentFromAlert {
    param(
        [string]$AlertName,
        [string]$ComponentName,
        [string]$Severity,
        [string]$Description
    )
    
    $Incident = [Incident]::new($AlertName, $ComponentName, $Severity)
    $Incident.Description = $Description
    $Incident.InvestigationNotes = "Auto-created from alert: $AlertName"
    
    return $Incident
}

# Function to display incident board
function Show-IncidentBoard {
    param(
        [array]$Incidents
    )
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "INCIDENT MANAGEMENT BOARD - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    $StatusGroups = $Incidents | Group-Object -Property Status
    
    foreach ($StatusGroup in $StatusGroups) {
        Write-Host "`n▼ $($StatusGroup.Name) [$($StatusGroup.Count)]" -ForegroundColor Yellow
        
        foreach ($Incident in $StatusGroup.Group) {
            $SeverityColor = switch ($Incident.Severity) {
                "CRITICAL" { "Red" }
                "WARNING" { "Yellow" }
                default { "White" }
            }
            
            $StatusIcon = switch ($Incident.Status) {
                "NEW" { "📋" }
                "ASSIGNED" { "👤" }
                "INVESTIGATING" { "🔍" }
                "RESOLVED" { "✓" }
                default { "•" }
            }
            
            Write-Host "`n  $StatusIcon $($Incident.IncidentNumber)" -ForegroundColor $SeverityColor
            Write-Host "     Title: $($Incident.Title)"
            Write-Host "     Component: $($Incident.Component)"
            Write-Host "     Severity: $($Incident.Severity)"
            Write-Host "     Created: $(Get-Date -Date $Incident.CreatedAt -Format 'HH:mm:ss')"
            
            if ($Incident.AssignedTo) {
                Write-Host "     Assigned To: $($Incident.AssignedTo)"
            }
            
            if ($Incident.Status -eq "INVESTIGATING") {
                Write-Host "     Investigation: $($Incident.InvestigationNotes)"
            }
            
            if ($Incident.Status -eq "RESOLVED") {
                $MTBF = $Incident.GetMTBF()
                Write-Host "     Resolution Time: $MTBF minutes"
                Write-Host "     Root Cause: $($Incident.RootCause)"
            }
        }
    }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# Function to show incident statistics
function Show-IncidentStatistics {
    param(
        [array]$Incidents
    )
    
    $TotalIncidents = $Incidents.Count
    $NewCount = ($Incidents | Where-Object { $_.Status -eq "NEW" }).Count
    $AssignedCount = ($Incidents | Where-Object { $_.Status -eq "ASSIGNED" }).Count
    $InvestigatingCount = ($Incidents | Where-Object { $_.Status -eq "INVESTIGATING" }).Count
    $ResolvedCount = ($Incidents | Where-Object { $_.Status -eq "RESOLVED" }).Count
    
    $CriticalCount = ($Incidents | Where-Object { $_.Severity -eq "CRITICAL" }).Count
    $WarningCount = ($Incidents | Where-Object { $_.Severity -eq "WARNING" }).Count
    
    # Calculate average MTTR
    $MTTRValues = @()
    foreach ($Incident in $Incidents) {
        $MTTR = $Incident.GetMTTR()
        if ($MTTR -gt 0) {
            $MTTRValues += $MTTR
        }
    }
    $AvgMTTR = if ($MTTRValues.Count -gt 0) { [math]::Round(($MTTRValues | Measure-Object -Average).Average, 2) } else { 0 }
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "INCIDENT STATISTICS" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    Write-Host "`nTotal Incidents: $TotalIncidents"
    Write-Host "`nStatus Distribution:"
    Write-Host "  ├─ New: $NewCount"
    Write-Host "  ├─ Assigned: $AssignedCount"
    Write-Host "  ├─ Investigating: $InvestigatingCount"
    Write-Host "  └─ Resolved: $ResolvedCount"
    
    Write-Host "`nSeverity Distribution:"
    Write-Host "  ├─ Critical: $CriticalCount"
    Write-Host "  └─ Warning: $WarningCount"
    
    Write-Host "`nPerformance Metrics:"
    Write-Host "  ├─ Average MTTR: $AvgMTTR minutes"
    Write-Host "  ├─ Resolution Rate: $([math]::Round(($ResolvedCount / $TotalIncidents * 100), 2))%"
    Write-Host "  └─ Average Time Assigned: ~25 minutes"
    
    Write-Host "`n════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# Function to escalate incident
function Escalate-Incident {
    param(
        [Incident]$Incident,
        [string]$Reason
    )
    
    $Incident.Severity = if ($Incident.Severity -eq "WARNING") { "CRITICAL" } else { "CRITICAL" }
    $Incident.InvestigationNotes += "`n[ESCALATED] Reason: $Reason"
    
    Write-Host "🔺 Incident $($Incident.IncidentNumber) escalated to $($Incident.Severity)" -ForegroundColor Red
}

# Main incident management loop
Write-Host "Starting incident management system..."
Write-Host "Current incidents: $($SampleIncidents.Count)"

$Incidents = $SampleIncidents
$Iteration = 0

while ($true) {
    $Iteration++
    
    Write-Host "`n[Cycle $Iteration - $(Get-Date -Format 'HH:mm:ss')]"
    
    # Show incident board
    Show-IncidentBoard -Incidents $Incidents
    
    # Show statistics
    Show-IncidentStatistics -Incidents $Incidents
    
    # Export incident status
    $IncidentExportFile = Join-Path (Split-Path $DatabasePath -Parent) "incidents_status.json"
    $Incidents | Select-Object IncidentNumber, Title, Component, Severity, Status, CreatedAt, AssignedTo, @{
        Name       = "MTTR"
        Expression = { $_.GetMTTR() }
    }, @{
        Name       = "MTBF"
        Expression = { $_.GetMTBF() }
    } | ConvertTo-Json | Out-File $IncidentExportFile -Force
    
    # Simulate incident lifecycle transitions
    $UnresolvedIncidents = $Incidents | Where-Object { $_.Status -ne "RESOLVED" }
    if ($UnresolvedIncidents.Count -gt 0 -and (Get-Random -Minimum 1 -Maximum 100) -gt 70) {
        $IncidentToProgress = $UnresolvedIncidents[(Get-Random -Minimum 0 -Maximum $UnresolvedIncidents.Count - 1)]
        
        switch ($IncidentToProgress.Status) {
            "NEW" {
                $IncidentToProgress.Assign("team@helios.local")
                Write-Host "`n✓ Incident assigned: $($IncidentToProgress.IncidentNumber)"
            }
            "ASSIGNED" {
                $IncidentToProgress.StartInvestigation()
                Write-Host "`n✓ Investigation started: $($IncidentToProgress.IncidentNumber)"
            }
            "INVESTIGATING" {
                if ((Get-Random -Minimum 1 -Maximum 100) -gt 50) {
                    $IncidentToProgress.Resolve("System error in service X", "Restarted service and applied patch")
                    Write-Host "`n✓ Incident resolved: $($IncidentToProgress.IncidentNumber)"
                }
            }
        }
    }
    
    # Wait before next cycle
    Write-Host "`nNext cycle in 60 seconds..."
    Start-Sleep -Seconds 60
}
