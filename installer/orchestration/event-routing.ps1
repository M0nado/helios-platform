<#
.SYNOPSIS
Event Routing System for HELIOS Platform - Routes events between components with filtering and history.

.DESCRIPTION
Provides:
- Event routing based on subscriptions
- Event filtering (relevance-based)
- Event history and replay capability
- Dead letter queue for failed events
- Event correlation tracking
- Pub/Sub pattern implementation

.EXAMPLE
PS> .\event-routing.ps1 -Action Publish -Source 'ai-hub' -EventType 'training_complete' -Data @{ model='gpt4' }
PS> .\event-routing.ps1 -Action Subscribe -Component 'dev-hub' -EventFilter 'training_complete'
PS> .\event-routing.ps1 -Action GetHistory -Limit 100

.NOTES
Components publish events, routing engine filters and distributes to subscribers.
All events are logged and can be replayed for debugging or recovery.
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Publish', 'Subscribe', 'Unsubscribe', 'GetHistory', 'Replay', 'GetStatus')]
    [string]$Action = 'GetStatus',
    
    [Parameter(Mandatory=$false)]
    [string]$Source = '',
    
    [Parameter(Mandatory=$false)]
    [string]$EventType = '',
    
    [Parameter(Mandatory=$false)]
    [hashtable]$Data = @{},
    
    [Parameter(Mandatory=$false)]
    [string]$Component = '',
    
    [Parameter(Mandatory=$false)]
    [string]$EventFilter = '*',
    
    [Parameter(Mandatory=$false)]
    [int]$Limit = 100,
    
    [Parameter(Mandatory=$false)]
    [string]$StateFile = 'C:\HELIOS\orchestration\config\event-routing-state.json'
)

$ErrorActionPreference = 'Stop'

# ===========================
# CONFIGURATION
# ===========================

$eventTypes = @(
    'component_started', 'component_stopped', 'component_failed',
    'deployment_started', 'deployment_completed', 'deployment_failed',
    'training_complete', 'model_ready',
    'build_triggered', 'build_completed', 'build_failed',
    'security_alert', 'policy_updated',
    'performance_degradation', 'resource_warning'
)

$eventHistory = @()
$subscriptions = @()
$deadLetterQueue = @()

# ===========================
# HELPER FUNCTIONS
# ===========================

function Write-EventLog {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Success', 'Warning', 'Error')][string]$Level = 'Info'
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $color = @{
        'Info' = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error' = 'Red'
    }[$Level]
    Write-Host "[$timestamp] [EVENT] [$Level] $Message" -ForegroundColor $color
}

function Load-State {
    if (-not (Test-Path $StateFile)) {
        return @{
            events = @()
            subscriptions = @()
            dead_letter = @()
        }
    }
    return Get-Content $StateFile | ConvertFrom-Json
}

function Save-State {
    $state = @{
        events = $eventHistory | Select-Object -Last 1000
        subscriptions = $subscriptions
        dead_letter = $deadLetterQueue | Select-Object -Last 100
    }
    $state | ConvertTo-Json -Depth 10 | Set-Content $StateFile
}

function New-Event {
    param(
        [string]$Source,
        [string]$EventType,
        [hashtable]$Data
    )
    
    return @{
        event_id = "evt-$(Get-Random -Minimum 100000 -Maximum 999999)"
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
        source = $Source
        event_type = $EventType
        data = $Data
        correlation_id = "corr-$(Get-Random -Minimum 100000 -Maximum 999999)"
        status = 'published'
        routed_to = @()
    }
}

function Test-EventMatch {
    param(
        [string]$Filter,
        [string]$EventType
    )
    
    if ($Filter -eq '*') { return $true }
    if ($Filter -eq $EventType) { return $true }
    
    # Support wildcards: "training*" matches "training_complete"
    if ($Filter.EndsWith('*')) {
        $prefix = $Filter -replace '\*$', ''
        return $EventType.StartsWith($prefix)
    }
    
    return $false
}

function Find-SubscribedComponents {
    param([object]$Event)
    
    $interested = @()
    
    foreach ($sub in $subscriptions) {
        if (Test-EventMatch $sub.event_filter $Event.event_type) {
            if ($sub.component -ne $Event.source) {  # Don't send back to source
                $interested += $sub.component
            }
        }
    }
    
    return $interested | Select-Object -Unique
}

function Route-Event {
    param([object]$Event)
    
    Write-EventLog "Routing event: $($Event.event_type) from $($Event.source)" -Level Info
    
    try {
        $subscribers = Find-SubscribedComponents $Event
        
        if ($subscribers.Count -eq 0) {
            Write-EventLog "  No subscribers for $($Event.event_type)" -Level Warning
            return $Event
        }
        
        foreach ($subscriber in $subscribers) {
            Write-EventLog "  → Delivering to $subscriber" -Level Info
            $Event.routed_to += $subscriber
            
            # In production, would actually deliver event to subscriber
            # For now, just simulate successful delivery
        }
        
        $Event.status = 'routed'
        return $Event
    }
    catch {
        Write-EventLog "ERROR routing event: $_" -Level Error
        $Event.status = 'failed'
        $deadLetterQueue += $Event
        return $Event
    }
}

function Invoke-Subscribe {
    param(
        [string]$Component,
        [string]$EventFilter
    )
    
    Write-EventLog "Subscribing $Component to events: $EventFilter" -Level Info
    
    $subscription = @{
        id = "sub-$(Get-Random -Minimum 100000 -Maximum 999999)"
        component = $Component
        event_filter = $EventFilter
        subscribed_at = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
    }
    
    $subscriptions += $subscription
    Write-EventLog "✓ Subscription created: $($subscription.id)" -Level Success
    
    return $subscription
}

function Invoke-Unsubscribe {
    param(
        [string]$Component,
        [string]$EventFilter
    )
    
    $removed = $subscriptions | Where-Object {
        $_.component -eq $Component -and $_.event_filter -eq $EventFilter
    }
    
    if ($removed) {
        $subscriptions = $subscriptions | Where-Object {
            -not ($_.component -eq $Component -and $_.event_filter -eq $EventFilter)
        }
        Write-EventLog "✓ Unsubscribed $Component from $EventFilter" -Level Success
        return $true
    }
    
    Write-EventLog "⚠ Subscription not found" -Level Warning
    return $false
}

function Invoke-Publish {
    param(
        [string]$Source,
        [string]$EventType,
        [hashtable]$Data
    )
    
    if ($EventType -notin $eventTypes) {
        Write-EventLog "⚠ Unknown event type: $EventType" -Level Warning
    }
    
    $event = New-Event -Source $Source -EventType $EventType -Data $Data
    Write-EventLog "Published: $($event.event_id) - $EventType from $Source" -Level Success
    
    # Route the event
    $routedEvent = Route-Event $event
    
    # Add to history
    $eventHistory += $routedEvent
    
    # Keep history bounded
    if ($eventHistory.Count -gt 1000) {
        $eventHistory = $eventHistory[-1000..-1]
    }
    
    return $routedEvent
}

function Get-EventHistory {
    param([int]$Limit = 100)
    
    $recentEvents = $eventHistory | Select-Object -Last $Limit
    
    Write-EventLog "Retrieved $($recentEvents.Count) events from history" -Level Info
    Write-Host "`nEvent History (last $Limit):" -ForegroundColor Cyan
    Write-Host "$('='*80)" -ForegroundColor Cyan
    
    foreach ($event in $recentEvents) {
        $color = @{
            'published' = 'Green'
            'routed' = 'Cyan'
            'failed' = 'Red'
        }[$event.status]
        
        Write-Host "$($event.timestamp) | $($event.event_type) ($($event.status))" -ForegroundColor $color
        Write-Host "  From: $($event.source) → To: $($event.routed_to -join ', ')" -ForegroundColor Gray
    }
    Write-Host "$('='*80)" -ForegroundColor Cyan
    
    return $recentEvents
}

function Invoke-Replay {
    param(
        [int]$LastNEvents = 10
    )
    
    Write-EventLog "Replaying last $LastNEvents events..." -Level Info
    
    $eventsToReplay = $eventHistory | Select-Object -Last $LastNEvents
    $replayedCount = 0
    
    foreach ($event in $eventsToReplay) {
        Write-EventLog "Replaying: $($event.event_type) (Event ID: $($event.event_id))" -Level Info
        
        try {
            Route-Event $event | Out-Null
            $replayedCount++
        }
        catch {
            Write-EventLog "ERROR replaying event: $_" -Level Error
        }
    }
    
    Write-EventLog "✓ Replayed $replayedCount events" -Level Success
    return $replayedCount
}

function Show-Status {
    Write-Host "`n$('='*80)" -ForegroundColor Cyan
    Write-Host "EVENT ROUTING SYSTEM STATUS" -ForegroundColor Cyan
    Write-Host "$('='*80)" -ForegroundColor Cyan
    
    Write-Host "`nStatistics:" -ForegroundColor Cyan
    Write-Host "  Total Events: $($eventHistory.Count)" -ForegroundColor Cyan
    Write-Host "  Active Subscriptions: $($subscriptions.Count)" -ForegroundColor Cyan
    Write-Host "  Dead Letter Queue: $($deadLetterQueue.Count)" -ForegroundColor Cyan
    
    Write-Host "`nSubscriptions:" -ForegroundColor Cyan
    foreach ($sub in $subscriptions) {
        Write-Host "  ├─ $($sub.component) → $($sub.event_filter)" -ForegroundColor Green
    }
    
    Write-Host "`nRecent Events:" -ForegroundColor Cyan
    foreach ($event in $eventHistory | Select-Object -Last 5) {
        $color = @{
            'published' = 'Green'
            'routed' = 'Cyan'
            'failed' = 'Red'
        }[$event.status]
        Write-Host "  ├─ $($event.event_type) ($($event.status))" -ForegroundColor $color
    }
    
    Write-Host ""
}

# ===========================
# MAIN EXECUTION
# ===========================

try {
    Write-Host "`n"
    Write-EventLog "HELIOS Event Routing System v1.0" -Level Info
    Write-EventLog "Action: $Action" -Level Info
    
    # Load existing state
    $state = Load-State
    $eventHistory = $state.events
    $subscriptions = $state.subscriptions
    $deadLetterQueue = $state.dead_letter
    
    switch ($Action) {
        'Publish' {
            if ([string]::IsNullOrEmpty($Source) -or [string]::IsNullOrEmpty($EventType)) {
                Write-EventLog "ERROR: Source and EventType are required for Publish action" -Level Error
                exit 1
            }
            Invoke-Publish -Source $Source -EventType $EventType -Data $Data
        }
        
        'Subscribe' {
            if ([string]::IsNullOrEmpty($Component)) {
                Write-EventLog "ERROR: Component is required for Subscribe action" -Level Error
                exit 1
            }
            Invoke-Subscribe -Component $Component -EventFilter $EventFilter
        }
        
        'Unsubscribe' {
            if ([string]::IsNullOrEmpty($Component)) {
                Write-EventLog "ERROR: Component is required for Unsubscribe action" -Level Error
                exit 1
            }
            Invoke-Unsubscribe -Component $Component -EventFilter $EventFilter
        }
        
        'GetHistory' {
            Get-EventHistory -Limit $Limit
        }
        
        'Replay' {
            Invoke-Replay -LastNEvents $Limit
        }
        
        'GetStatus' {
            Show-Status
        }
    }
    
    # Save state
    Save-State
    
    Write-EventLog "Operation completed successfully" -Level Success
}
catch {
    Write-EventLog "FATAL ERROR: $_" -Level Error
    exit 1
}
